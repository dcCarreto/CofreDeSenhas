using System;
using System.IO;
using System.Security.Cryptography;

namespace GerenciadorDeSenhas.Servicos
{
    public class AutenticacaoMestra
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int VerificadorSize = 32;
        public const int TamanhoMinimoSenha = 8;

        // OWASP recomenda 600 mil iterações para PBKDF2-HMAC-SHA256 (dez/2023).
        // Cofres criados com a contagem antiga (100 mil) são migrados de forma
        // transparente no próximo desbloqueio por senha (ver ServicoMudancaSenhaMestra).
        private const int IteracoesAtuais = 600_000;
        private const int IteracoesLegado = 100_000;

        private readonly string _pastaApp;
        private readonly string _caminhoAuth;

        public AutenticacaoMestra(string? pastaApp = null)
        {
            _pastaApp = pastaApp ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GerenciadorSenhas");

            if (!Directory.Exists(_pastaApp))
                Directory.CreateDirectory(_pastaApp);

            _caminhoAuth = Path.Combine(_pastaApp, "auth.dat");
        }

        public string PastaApp => _pastaApp;

        public bool ExisteSenhaMestra() => File.Exists(_caminhoAuth);

        public byte[] CriarSenhaMestra(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
                throw new ArgumentException("A senha mestra não pode ser vazia.");
            if (senha.Length < TamanhoMinimoSenha)
                throw new ArgumentException($"A senha mestra deve ter pelo menos {TamanhoMinimoSenha} caracteres.");

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var chave = DerivarChave(senha, salt, IteracoesAtuais);
            var verificador = SHA256.HashData(chave);

            var dados = new byte[SaltSize + verificador.Length + sizeof(int)];
            Buffer.BlockCopy(salt, 0, dados, 0, SaltSize);
            Buffer.BlockCopy(verificador, 0, dados, SaltSize, verificador.Length);
            BitConverter.GetBytes(IteracoesAtuais).CopyTo(dados, SaltSize + verificador.Length);

            File.WriteAllText(_caminhoAuth, Convert.ToBase64String(dados));
            return chave;
        }

        public byte[]? Autenticar(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha)) return null;
            if (!File.Exists(_caminhoAuth)) return null;

            try
            {
                if (!LerDadosAutenticacao(out var salt, out var verificadorArmazenado, out var iteracoes))
                    return null;

                var chave = DerivarChave(senha, salt, iteracoes);
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

        public bool IteracoesDesatualizadas()
        {
            if (!File.Exists(_caminhoAuth))
                return false;

            try
            {
                return LerDadosAutenticacao(out _, out _, out var iteracoes) && iteracoes < IteracoesAtuais;
            }
            catch
            {
                return false;
            }
        }

        private bool LerDadosAutenticacao(out byte[] salt, out byte[] verificador, out int iteracoes)
        {
            salt = Array.Empty<byte>();
            verificador = Array.Empty<byte>();
            iteracoes = IteracoesLegado;

            var dados = Convert.FromBase64String(File.ReadAllText(_caminhoAuth));
            if (dados.Length < SaltSize + VerificadorSize)
                return false;

            salt = new byte[SaltSize];
            verificador = new byte[VerificadorSize];
            Buffer.BlockCopy(dados, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(dados, SaltSize, verificador, 0, VerificadorSize);

            if (dados.Length >= SaltSize + VerificadorSize + sizeof(int))
                iteracoes = BitConverter.ToInt32(dados, SaltSize + VerificadorSize);

            return true;
        }

        private static byte[] DerivarChave(string senha, byte[] salt, int iteracoes) =>
            Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, KeySize);
    }
}
