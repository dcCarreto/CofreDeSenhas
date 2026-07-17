using System;
using System.Security.Cryptography;
using System.Text;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoCriptografia : IServicoCriptografia
    {
        private const int TamanhoNonce = 12;
        private const int TamanhoTag = 16;

        private readonly byte[] _chave;

        public ServicoCriptografia(byte[] chave)
        {
            if (chave.Length != 32)
                throw new ArgumentException("Chave deve ter 256 bits (32 bytes)");
            _chave = chave;
        }

        public string Criptografar(string plaintext)
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var iv = new byte[TamanhoNonce];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(iv);

            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TamanhoTag];

            using (var aes = new AesGcm(_chave, TamanhoTag))
            {
                aes.Encrypt(iv, plaintextBytes, ciphertext, tag, Array.Empty<byte>());
            }

            var result = new byte[iv.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, iv.Length + ciphertext.Length, tag.Length);

            return Convert.ToBase64String(result);
        }

        public string Descriptografar(string ciphertextBase64)
        {
            var data = Convert.FromBase64String(ciphertextBase64);

            var iv = new byte[TamanhoNonce];
            var encrypted = new byte[data.Length - iv.Length - TamanhoTag];
            var tag = new byte[TamanhoTag];

            Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(data, iv.Length, encrypted, 0, encrypted.Length);
            Buffer.BlockCopy(data, iv.Length + encrypted.Length, tag, 0, tag.Length);

            var plaintext = new byte[encrypted.Length];
            using (var aes = new AesGcm(_chave, TamanhoTag))
            {
                aes.Decrypt(iv, encrypted, tag, plaintext, Array.Empty<byte>());
            }

            return Encoding.UTF8.GetString(plaintext);
        }

        public void ZerarChave() => CryptographicOperations.ZeroMemory(_chave);
    }
}
