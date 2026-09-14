using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GerenciadorDeSenhas.Servicos
{
    // Chave de recuperação: um segredo de 160 bits que o usuário anota. O arquivo
    // recuperacao.dat guarda a CHAVE MESTRA do cofre envelopada (AES-256-GCM) por
    // uma chave derivada desse segredo (HKDF-SHA256 — o segredo já tem entropia
    // alta, não precisa de KDF memória-dura). Só destranca; quem recupera é
    // obrigado a definir uma senha mestra nova em seguida.
    public sealed class ServicoRecuperacao
    {
        public const int TamanhoSegredoBytes = 20;   // 160 bits -> 32 caracteres base32
        private const int TamanhoChave = 32;
        private const int TamanhoSalt = 16;
        private const int TamanhoNonce = 12;
        private const int TamanhoTag = 16;
        private const int TamanhoArquivo = TamanhoSalt + TamanhoNonce + TamanhoChave + TamanhoTag;

        private static readonly byte[] InfoHkdf = Encoding.UTF8.GetBytes("CofreDeSenhas.Recuperacao.v1");

        private readonly string _caminho;

        public ServicoRecuperacao(string? pastaApp = null)
        {
            var pasta = pastaApp ?? AmbienteCofre.PastaDados;
            if (!Directory.Exists(pasta))
                Directory.CreateDirectory(pasta);
            _caminho = Path.Combine(pasta, "recuperacao.dat");
        }

        public bool EstaHabilitada() => File.Exists(_caminho);

        public void Desabilitar()
        {
            try { if (File.Exists(_caminho)) File.Delete(_caminho); }
            catch { /* melhor esforço */ }
        }

        // Gera um segredo novo, envelopa a chave mestra e grava recuperacao.dat.
        // Devolve o segredo já formatado em grupos de 4 para exibir.
        public string Habilitar(byte[] chaveMestra)
        {
            if (chaveMestra is not { Length: TamanhoChave })
                throw new ArgumentException("Chave mestra deve ter 32 bytes.", nameof(chaveMestra));

            var segredo = RandomNumberGenerator.GetBytes(TamanhoSegredoBytes);
            try
            {
                GravarEnvelope(segredo, chaveMestra);
                return Formatar(Base32.Codificar(segredo));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(segredo);
            }
        }

        // Devolve a chave mestra do cofre se o segredo estiver certo; null se o
        // segredo é inválido, não confere, ou não há recuperação habilitada.
        public byte[]? Recuperar(string segredoDigitado)
        {
            if (string.IsNullOrWhiteSpace(segredoDigitado) || !File.Exists(_caminho))
                return null;

            byte[]? segredo = null;
            try
            {
                segredo = Base32.Decodificar(RemoverSeparadores(segredoDigitado));
                if (segredo is not { Length: TamanhoSegredoBytes })
                    return null;
                return LerEnvelope(segredo);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (segredo != null)
                    CryptographicOperations.ZeroMemory(segredo);
            }
        }

        private void GravarEnvelope(byte[] segredo, byte[] chaveMestra)
        {
            var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
            var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
            var envoltorio = HKDF.DeriveKey(HashAlgorithmName.SHA256, segredo, TamanhoChave, salt, InfoHkdf);
            try
            {
                var buffer = new byte[TamanhoArquivo];
                salt.CopyTo(buffer, 0);
                nonce.CopyTo(buffer, TamanhoSalt);
                var cifrado = buffer.AsSpan(TamanhoSalt + TamanhoNonce, TamanhoChave);
                var tag = buffer.AsSpan(TamanhoSalt + TamanhoNonce + TamanhoChave, TamanhoTag);

                using (var aes = new AesGcm(envoltorio, TamanhoTag))
                    aes.Encrypt(nonce, chaveMestra, cifrado, tag);

                EscritaAtomica.EscreverTexto(_caminho, Convert.ToBase64String(buffer));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(envoltorio);
            }
        }

        private byte[]? LerEnvelope(byte[] segredo)
        {
            var buffer = Convert.FromBase64String(File.ReadAllText(_caminho));
            if (buffer.Length != TamanhoArquivo)
                return null;

            var salt = buffer.AsSpan(0, TamanhoSalt);
            var nonce = buffer.AsSpan(TamanhoSalt, TamanhoNonce);
            var cifrado = buffer.AsSpan(TamanhoSalt + TamanhoNonce, TamanhoChave);
            var tag = buffer.AsSpan(TamanhoSalt + TamanhoNonce + TamanhoChave, TamanhoTag);

            var envoltorio = HKDF.DeriveKey(HashAlgorithmName.SHA256, segredo, TamanhoChave, salt.ToArray(), InfoHkdf);
            try
            {
                var chave = new byte[TamanhoChave];
                using var aes = new AesGcm(envoltorio, TamanhoTag);
                aes.Decrypt(nonce, cifrado, tag, chave); // lança se o segredo estiver errado ou o arquivo foi adulterado
                return chave;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(envoltorio);
            }
        }

        private static string Formatar(string bruto)
        {
            var sb = new StringBuilder(bruto.Length + bruto.Length / 4);
            for (int i = 0; i < bruto.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    sb.Append('-');
                sb.Append(bruto[i]);
            }
            return sb.ToString();
        }

        private static string RemoverSeparadores(string texto)
        {
            var sb = new StringBuilder(texto.Length);
            foreach (var c in texto)
                if (!char.IsWhiteSpace(c) && c != '-')
                    sb.Append(c);
            return sb.ToString();
        }
    }
}
