using System;
using System.IO;
using System.Security.Cryptography;

namespace GerenciadorDeSenhas.Servicos
{
    public class AutenticacaoMestra
    {
        private readonly string _caminhoAuth;

        public AutenticacaoMestra(string? pastaApp = null)
        {
            var pasta = pastaApp ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GerenciadorSenhas");

            if (!Directory.Exists(pasta))
                Directory.CreateDirectory(pasta);

            _caminhoAuth = Path.Combine(pasta, "auth.dat");
        }

        public bool ExisteSenhaMestra() => File.Exists(_caminhoAuth);

        public byte[] CriarSenhaMestra(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
                throw new ArgumentException("A senha mestra não pode ser vazia.");
            if (senha.Length < 8)
                throw new ArgumentException("A senha mestra deve ter pelo menos 8 caracteres.");

            var salt = RandomNumberGenerator.GetBytes(EspecificacaoCriptografica.TamanhoSalt);
            var chave = DerivarChave(senha, salt);
            var verificador = SHA256.HashData(chave);

            var dados = new byte[EspecificacaoCriptografica.TamanhoSalt + verificador.Length];
            Buffer.BlockCopy(salt, 0, dados, 0, EspecificacaoCriptografica.TamanhoSalt);
            Buffer.BlockCopy(verificador, 0, dados, EspecificacaoCriptografica.TamanhoSalt, verificador.Length);

            File.WriteAllText(_caminhoAuth, Convert.ToBase64String(dados));
            return chave;
        }

        public byte[]? Autenticar(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha)) return null;
            if (!File.Exists(_caminhoAuth)) return null;

            try
            {
                var dados = Convert.FromBase64String(File.ReadAllText(_caminhoAuth));
                if (dados.Length < EspecificacaoCriptografica.TamanhoMinimoAuth) return null;

                var salt = new byte[EspecificacaoCriptografica.TamanhoSalt];
                var verificadorArmazenado = new byte[EspecificacaoCriptografica.TamanhoVerificador];
                Buffer.BlockCopy(dados, 0, salt, 0, EspecificacaoCriptografica.TamanhoSalt);
                Buffer.BlockCopy(dados, EspecificacaoCriptografica.TamanhoSalt, verificadorArmazenado, 0, EspecificacaoCriptografica.TamanhoVerificador);

                var chave = DerivarChave(senha, salt);
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

        private static byte[] DerivarChave(string senha, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(
                senha,
                salt,
                EspecificacaoCriptografica.IteracoesPbkdf2,
                HashAlgorithmName.SHA256,
                EspecificacaoCriptografica.TamanhoChave);
    }
}
