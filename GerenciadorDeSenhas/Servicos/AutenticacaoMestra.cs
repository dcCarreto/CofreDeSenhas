using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GerenciadorDeSenhas.Excecoes;
using Konscious.Security.Cryptography;

namespace GerenciadorDeSenhas.Servicos
{
    public class AutenticacaoMestra
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int VerificadorSize = 32;
        public const int TamanhoMinimoSenha = 8;

        private const int IteracoesLegado = 100_000;

        private const byte KdfPbkdf2 = 0;
        private const byte KdfArgon2id = 1;

        private const int MemoriaKbAtual = 65536;
        private const int TempoCustoAtual = 3;
        private const int ParalelismoAtual = 1;

        private readonly string _pastaApp;
        private readonly string _caminhoAuth;

        public AutenticacaoMestra(string? pastaApp = null)
        {
            _pastaApp = pastaApp ?? AmbienteCofre.PastaDados;

            if (!Directory.Exists(_pastaApp))
                Directory.CreateDirectory(_pastaApp);

            _caminhoAuth = Path.Combine(_pastaApp, "auth.dat");
        }

        public string PastaApp => _pastaApp;

        public bool ExisteSenhaMestra() => File.Exists(_caminhoAuth);

        public byte[] CriarSenhaMestra(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
                throw new ErroLocalizavel("Auth.Error.PasswordRequired");
            if (senha.Length < TamanhoMinimoSenha)
                throw new ErroLocalizavel("Auth.Error.PasswordTooShort", TamanhoMinimoSenha);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var chave = DerivarChaveArgon2id(senha, salt, TempoCustoAtual, MemoriaKbAtual, ParalelismoAtual);
            var verificador = SHA256.HashData(chave);

            var dados = new byte[SaltSize + VerificadorSize + sizeof(int) + 1 + sizeof(int) + sizeof(int)];
            var offset = 0;
            Buffer.BlockCopy(salt, 0, dados, offset, SaltSize);
            offset += SaltSize;
            Buffer.BlockCopy(verificador, 0, dados, offset, VerificadorSize);
            offset += VerificadorSize;
            BitConverter.GetBytes(TempoCustoAtual).CopyTo(dados, offset);
            offset += sizeof(int);
            dados[offset] = KdfArgon2id;
            offset += 1;
            BitConverter.GetBytes(MemoriaKbAtual).CopyTo(dados, offset);
            offset += sizeof(int);
            BitConverter.GetBytes(ParalelismoAtual).CopyTo(dados, offset);

            EscritaAtomica.EscreverTexto(_caminhoAuth, Convert.ToBase64String(dados));
            return chave;
        }

        public void ExcluirSenhaMestra()
        {
            if (File.Exists(_caminhoAuth))
                File.Delete(_caminhoAuth);
        }

        public byte[]? Autenticar(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha)) return null;
            if (!File.Exists(_caminhoAuth)) return null;

            try
            {
                if (!LerDadosAutenticacao(out var salt, out var verificadorArmazenado, out var parametros))
                    return null;

                var chave = DerivarChave(senha, salt, parametros);
                var verificador = SHA256.HashData(chave);

                if (CryptographicOperations.FixedTimeEquals(verificador, verificadorArmazenado))
                    return chave;

                return null;
            }
            catch
            {
                return null;
            }
        }

        public bool ValidarChave(byte[]? chave)
        {
            if (chave == null || chave.Length != KeySize || !File.Exists(_caminhoAuth))
                return false;

            try
            {
                if (!LerDadosAutenticacao(out _, out var verificadorArmazenado, out _))
                    return false;

                var verificador = SHA256.HashData(chave);
                return CryptographicOperations.FixedTimeEquals(verificador, verificadorArmazenado);
            }
            catch
            {
                return false;
            }
        }

        public bool KdfDesatualizado()
        {
            if (!File.Exists(_caminhoAuth))
                return false;

            try
            {
                return LerDadosAutenticacao(out _, out _, out var parametros) && parametros.Kdf != KdfArgon2id;
            }
            catch
            {
                return false;
            }
        }

        public bool TentarLerParametros(out byte[] salt, out byte[] verificador, out byte kdf, out int custo, out int memoriaKb, out int paralelismo)
        {
            salt = Array.Empty<byte>();
            verificador = Array.Empty<byte>();
            kdf = KdfPbkdf2;
            custo = 0;
            memoriaKb = 0;
            paralelismo = 0;

            if (!File.Exists(_caminhoAuth))
                return false;

            try
            {
                if (!LerDadosAutenticacao(out salt, out verificador, out var parametros))
                    return false;

                kdf = parametros.Kdf;
                custo = parametros.CustoPrimario;
                memoriaKb = parametros.MemoriaKb;
                paralelismo = parametros.Paralelismo;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static byte[] DerivarChaveDeParametros(string senha, byte[] salt, byte kdf, int custo, int memoriaKb, int paralelismo) =>
            DerivarChave(senha, salt, new ParametrosKdf(kdf, custo, memoriaKb, paralelismo));

        public void GravarAutenticacaoRestaurada(byte[] salt, byte[] verificador, byte kdf, int custo, int memoriaKb, int paralelismo)
        {
            var dados = new byte[SaltSize + VerificadorSize + sizeof(int) + 1 + sizeof(int) + sizeof(int)];
            var offset = 0;
            Buffer.BlockCopy(salt, 0, dados, offset, SaltSize);
            offset += SaltSize;
            Buffer.BlockCopy(verificador, 0, dados, offset, VerificadorSize);
            offset += VerificadorSize;
            BitConverter.GetBytes(custo).CopyTo(dados, offset);
            offset += sizeof(int);
            dados[offset] = kdf;
            offset += 1;
            BitConverter.GetBytes(memoriaKb).CopyTo(dados, offset);
            offset += sizeof(int);
            BitConverter.GetBytes(paralelismo).CopyTo(dados, offset);

            EscritaAtomica.EscreverTexto(_caminhoAuth, Convert.ToBase64String(dados));
        }

        private bool LerDadosAutenticacao(out byte[] salt, out byte[] verificador, out ParametrosKdf parametros)
        {
            salt = Array.Empty<byte>();
            verificador = Array.Empty<byte>();
            parametros = new ParametrosKdf(KdfPbkdf2, IteracoesLegado, 0, 0);

            var dados = Convert.FromBase64String(File.ReadAllText(_caminhoAuth));
            if (dados.Length < SaltSize + VerificadorSize)
                return false;

            salt = new byte[SaltSize];
            verificador = new byte[VerificadorSize];
            Buffer.BlockCopy(dados, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(dados, SaltSize, verificador, 0, VerificadorSize);

            var offset = SaltSize + VerificadorSize;
            if (dados.Length < offset + sizeof(int))
                return true;

            var custoPrimario = BitConverter.ToInt32(dados, offset);
            offset += sizeof(int);

            if (dados.Length < offset + 1 + sizeof(int) + sizeof(int))
            {
                parametros = new ParametrosKdf(KdfPbkdf2, custoPrimario, 0, 0);
                return true;
            }

            var kdf = dados[offset];
            offset += 1;
            var memoriaKb = BitConverter.ToInt32(dados, offset);
            offset += sizeof(int);
            var paralelismo = BitConverter.ToInt32(dados, offset);

            parametros = kdf == KdfArgon2id
                ? new ParametrosKdf(KdfArgon2id, custoPrimario, memoriaKb, paralelismo)
                : new ParametrosKdf(KdfPbkdf2, custoPrimario, 0, 0);

            return true;
        }

        private static byte[] DerivarChave(string senha, byte[] salt, ParametrosKdf parametros) =>
            parametros.Kdf == KdfArgon2id
                ? DerivarChaveArgon2id(senha, salt, parametros.CustoPrimario, parametros.MemoriaKb, parametros.Paralelismo)
                : DerivarChavePbkdf2(senha, salt, parametros.CustoPrimario);

        private static byte[] DerivarChavePbkdf2(string senha, byte[] salt, int iteracoes) =>
            Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, KeySize);

        private static byte[] DerivarChaveArgon2id(string senha, byte[] salt, int tempoCusto, int memoriaKb, int paralelismo)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(senha))
            {
                Salt = salt,
                DegreeOfParallelism = paralelismo,
                Iterations = tempoCusto,
                MemorySize = memoriaKb
            };
            return argon2.GetBytes(KeySize);
        }

        private readonly record struct ParametrosKdf(byte Kdf, int CustoPrimario, int MemoriaKb, int Paralelismo);
    }
}
