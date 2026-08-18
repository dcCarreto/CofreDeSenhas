using System;
using System.Security.Cryptography;
using System.Text;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoCriptografia : IServicoCriptografia
    {
        private const int TamanhoNonce = 12;
        private const int TamanhoTag = 16;
        private static readonly byte[] InfoChaveHmac = Encoding.UTF8.GetBytes("CofreDeSenhas.IntegridadeBanco.v1");

        private readonly byte[] _chave;
        private byte[]? _chaveHmac;
        private bool _zerada;

        public ServicoCriptografia(byte[] chave)
        {
            if (chave.Length != 32)
                throw new ArgumentException("Chave deve ter 256 bits (32 bytes)");
            _chave = chave;
        }

        public string Criptografar(string plaintext) =>
            Convert.ToBase64String(CriptografarBytes(Encoding.UTF8.GetBytes(plaintext)));

        public string Descriptografar(string ciphertextBase64) =>
            Encoding.UTF8.GetString(DescriptografarBytes(Convert.FromBase64String(ciphertextBase64)));

        public byte[] CriptografarBytes(byte[] plaintext)
        {
            VerificarNaoZerada();

            var iv = new byte[TamanhoNonce];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(iv);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TamanhoTag];

            using (var aes = new AesGcm(_chave, TamanhoTag))
            {
                aes.Encrypt(iv, plaintext, ciphertext, tag, Array.Empty<byte>());
            }

            var result = new byte[iv.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, iv.Length + ciphertext.Length, tag.Length);

            return result;
        }

        public byte[] DescriptografarBytes(byte[] data)
        {
            VerificarNaoZerada();

            if (data.Length < TamanhoNonce + TamanhoTag)
                throw new CryptographicException("Dados cifrados corrompidos ou incompletos.");

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

            return plaintext;
        }

        public string CalcularHmacIntegridade(string dados)
        {
            VerificarNaoZerada();

            using var hmac = new HMACSHA256(ChaveHmac());
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dados)));
        }

        public bool VerificarHmacIntegridade(string dados, string? hmacBase64)
        {
            VerificarNaoZerada();

            if (string.IsNullOrEmpty(hmacBase64))
                return false;

            byte[] recebido;
            try
            {
                recebido = Convert.FromBase64String(hmacBase64);
            }
            catch (FormatException)
            {
                return false;
            }

            using var hmac = new HMACSHA256(ChaveHmac());
            var calculado = hmac.ComputeHash(Encoding.UTF8.GetBytes(dados));
            return CryptographicOperations.FixedTimeEquals(calculado, recebido);
        }

        private byte[] ChaveHmac() =>
            _chaveHmac ??= HKDF.DeriveKey(HashAlgorithmName.SHA256, _chave, 32, info: InfoChaveHmac);

        public void ZerarChave()
        {
            CryptographicOperations.ZeroMemory(_chave);
            if (_chaveHmac != null)
                CryptographicOperations.ZeroMemory(_chaveHmac);
            _zerada = true;
        }

        private void VerificarNaoZerada()
        {
            if (_zerada)
                throw new ObjectDisposedException(nameof(ServicoCriptografia), "A chave já foi zerada e não pode mais ser usada.");
        }
    }
}
