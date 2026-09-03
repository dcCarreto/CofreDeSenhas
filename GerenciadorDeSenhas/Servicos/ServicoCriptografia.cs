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
        // AesGcm reusado em vez de um por chamada. Seguro porque o app é single-thread
        // (sem Task.Run); descartado em ZerarChave.
        private readonly AesGcm _aes;
        private byte[]? _chaveHmac;
        private bool _zerada;

        public ServicoCriptografia(byte[] chave)
        {
            if (chave.Length != 32)
                throw new ArgumentException("Chave deve ter 256 bits (32 bytes)");
            _chave = chave;
            _aes = new AesGcm(chave, TamanhoTag);
        }

        public string Criptografar(string plaintext) =>
            Convert.ToBase64String(CriptografarBytes(Encoding.UTF8.GetBytes(plaintext)));

        public string Descriptografar(string ciphertextBase64) =>
            Encoding.UTF8.GetString(DescriptografarBytes(Convert.FromBase64String(ciphertextBase64)));

        public byte[] CriptografarBytes(byte[] plaintext)
        {
            VerificarNaoZerada();

            var resultado = new byte[TamanhoNonce + plaintext.Length + TamanhoTag];
            var nonce = resultado.AsSpan(0, TamanhoNonce);
            var ciphertext = resultado.AsSpan(TamanhoNonce, plaintext.Length);
            var tag = resultado.AsSpan(TamanhoNonce + plaintext.Length, TamanhoTag);

            RandomNumberGenerator.Fill(nonce);
            _aes.Encrypt(nonce, plaintext, ciphertext, tag);

            return resultado;
        }

        public byte[] DescriptografarBytes(byte[] data)
        {
            VerificarNaoZerada();

            if (data.Length < TamanhoNonce + TamanhoTag)
                throw new CryptographicException("Dados cifrados corrompidos ou incompletos.");

            int tamanhoCipher = data.Length - TamanhoNonce - TamanhoTag;
            var nonce = data.AsSpan(0, TamanhoNonce);
            var ciphertext = data.AsSpan(TamanhoNonce, tamanhoCipher);
            var tag = data.AsSpan(TamanhoNonce + tamanhoCipher, TamanhoTag);

            var plaintext = new byte[tamanhoCipher];
            _aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }

        public string CalcularHmacIntegridade(string dados)
        {
            VerificarNaoZerada();

            var hash = HMACSHA256.HashData(ChaveHmac(), Encoding.UTF8.GetBytes(dados));
            return Convert.ToBase64String(hash);
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

            var calculado = HMACSHA256.HashData(ChaveHmac(), Encoding.UTF8.GetBytes(dados));
            return CryptographicOperations.FixedTimeEquals(calculado, recebido);
        }

        private byte[] ChaveHmac() =>
            _chaveHmac ??= HKDF.DeriveKey(HashAlgorithmName.SHA256, _chave, 32, info: InfoChaveHmac);

        public void ZerarChave()
        {
            CryptographicOperations.ZeroMemory(_chave);
            if (_chaveHmac != null)
                CryptographicOperations.ZeroMemory(_chaveHmac);
            _aes.Dispose();
            _zerada = true;
        }

        private void VerificarNaoZerada()
        {
            if (_zerada)
                throw new ObjectDisposedException(nameof(ServicoCriptografia), "A chave já foi zerada e não pode mais ser usada.");
        }
    }
}
