using System;
using System.IO;
using System.Security.Cryptography;

namespace GerenciadorDeSenhas.Servicos
{
    public class AutenticacaoMestra
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

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

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var chave = DerivarChave(senha, salt);
            var verificador = SHA256.HashData(chave);

            var dados = new byte[SaltSize + verificador.Length];
            Buffer.BlockCopy(salt, 0, dados, 0, SaltSize);
            Buffer.BlockCopy(verificador, 0, dados, SaltSize, verificador.Length);

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
                if (dados.Length < SaltSize + 32) return null;

                var salt = new byte[SaltSize];
                var verificadorArmazenado = new byte[32];
                Buffer.BlockCopy(dados, 0, salt, 0, SaltSize);
                Buffer.BlockCopy(dados, SaltSize, verificadorArmazenado, 0, 32);

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
            Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
    }
}
