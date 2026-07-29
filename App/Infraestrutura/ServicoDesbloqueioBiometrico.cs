using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia.Controls;
using GerenciadorDeSenhas.Servicos;

#if WINDOWS
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
#endif

namespace CofreDeSenhas
{
    internal sealed class ResultadoBiometria
    {
        private ResultadoBiometria(bool sucesso, bool cancelado, byte[]? chave, string? mensagem)
        {
            Sucesso = sucesso;
            Cancelado = cancelado;
            Chave = chave;
            Mensagem = mensagem;
        }

        public bool Sucesso { get; }
        public bool Cancelado { get; }
        public byte[]? Chave { get; }
        public string? Mensagem { get; }

        public static ResultadoBiometria Ok(byte[]? chave = null) => new(true, false, chave, null);
        public static ResultadoBiometria Falha(string mensagem) => new(false, false, null, mensagem);
        public static ResultadoBiometria Cancelada() => new(false, true, null, null);
    }

    internal sealed class ServicoDesbloqueioBiometrico
    {
        internal const int VersaoAtual = 2;
        private const string NomeCredencial = "CofreDeSenhas.WindowsHello";
        private const int TamanhoDesafio = 32;
        private const int TamanhoNonce = 12;
        private const int TamanhoTag = 16;

        private readonly string _caminhoRegistro;

        public ServicoDesbloqueioBiometrico(string? pastaApp = null)
        {
            var pasta = pastaApp ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                CaminhosApp.PastaDados);

            _caminhoRegistro = Path.Combine(pasta, "biometria.dat");
        }

        public bool SistemaSuportado =>
#if WINDOWS
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240);
#else
            false;
#endif

        public bool EstaHabilitado => File.Exists(_caminhoRegistro);

        public async Task DesabilitarAsync()
        {
            await ExcluirCredencialAsync();
            ExcluirRegistro();
        }

        public async Task<bool> PodeConfigurarAsync()
        {
            if (!SistemaSuportado)
                return false;

#if WINDOWS
            try
            {
                return await KeyCredentialManager.IsSupportedAsync();
            }
            catch
            {
                return false;
            }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        public async Task<ResultadoBiometria> HabilitarAsync(Window janela, byte[] chaveMestra)
        {
            if (chaveMestra.Length != 32)
                return ResultadoBiometria.Falha(Idioma.Texto("Biometric.InvalidVaultKey"));

            var disponibilidade = await VerificarDisponibilidadeAsync();
            if (!disponibilidade.Sucesso)
                return disponibilidade;

#if WINDOWS
            janela.Activate();
            try
            {
                var criacao = await KeyCredentialManager.RequestCreateAsync(
                    NomeCredencial, KeyCredentialCreationOption.ReplaceExisting);
                if (criacao.Status != KeyCredentialStatus.Success)
                    return MapearStatus(criacao.Status);

                var desafio = RandomNumberGenerator.GetBytes(TamanhoDesafio);
                var (status, assinatura) = await AssinarAsync(criacao.Credential, desafio);
                if (status != KeyCredentialStatus.Success || assinatura == null)
                    return MapearStatus(status);

                var registro = EnvelopeChave(chaveMestra, desafio, assinatura);

                var pasta = Path.GetDirectoryName(_caminhoRegistro)!;
                if (!Directory.Exists(pasta))
                    Directory.CreateDirectory(pasta);

                File.WriteAllText(_caminhoRegistro, JsonSerializer.Serialize(registro));
                return ResultadoBiometria.Ok();
            }
            catch (Exception ex)
            {
                await ExcluirCredencialAsync();
                return ResultadoBiometria.Falha(Idioma.Formatar("Biometric.EnableError", ErrosUi.MensagemAmigavel(ex)));
            }
#else
            _ = janela;
            await Task.CompletedTask;
            return ResultadoBiometria.Falha(Idioma.Texto("Biometric.UnsupportedPlatform"));
#endif
        }

        public async Task<ResultadoBiometria> DesbloquearAsync(Window janela, AutenticacaoMestra autenticacao)
        {
            if (!EstaHabilitado)
                return ResultadoBiometria.Falha(Idioma.Texto("Biometric.NotEnabled"));

            var disponibilidade = await VerificarDisponibilidadeAsync();
            if (!disponibilidade.Sucesso)
                return disponibilidade;

#if WINDOWS
            RegistroBiometrico? registro;
            try
            {
                registro = JsonSerializer.Deserialize<RegistroBiometrico>(File.ReadAllText(_caminhoRegistro));
            }
            catch (Exception ex)
            {
                return ResultadoBiometria.Falha(Idioma.Formatar("Biometric.UnlockError", ErrosUi.MensagemAmigavel(ex)));
            }

            if (!RegistroValido(registro))
            {
                await DesabilitarAsync();
                return ResultadoBiometria.Falha(Idioma.Texto("Biometric.InvalidRegistration"));
            }

            janela.Activate();
            try
            {
                var abertura = await KeyCredentialManager.OpenAsync(registro!.Credencial);
                if (abertura.Status != KeyCredentialStatus.Success)
                {
                    if (abertura.Status == KeyCredentialStatus.NotFound)
                    {
                        await DesabilitarAsync();
                        return ResultadoBiometria.Falha(Idioma.Texto("Biometric.InvalidRegistration"));
                    }
                    return MapearStatus(abertura.Status);
                }

                var desafio = Convert.FromBase64String(registro.Desafio!);
                var (status, assinatura) = await AssinarAsync(abertura.Credential, desafio);
                if (status != KeyCredentialStatus.Success || assinatura == null)
                    return MapearStatus(status);

                var chave = AbrirEnvelope(registro, assinatura);
                if (chave == null || !autenticacao.ValidarChave(chave))
                {
                    if (chave != null)
                        CryptographicOperations.ZeroMemory(chave);
                    await DesabilitarAsync();
                    return ResultadoBiometria.Falha(Idioma.Texto("Biometric.InvalidRegistration"));
                }

                return ResultadoBiometria.Ok(chave);
            }
            catch (Exception ex)
            {
                return ResultadoBiometria.Falha(Idioma.Formatar("Biometric.UnlockError", ErrosUi.MensagemAmigavel(ex)));
            }
#else
            _ = janela;
            _ = autenticacao;
            await Task.CompletedTask;
            return ResultadoBiometria.Falha(Idioma.Texto("Biometric.UnsupportedPlatform"));
#endif
        }

        private static async Task<ResultadoBiometria> VerificarDisponibilidadeAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ResultadoBiometria.Falha(Idioma.Texto("Biometric.UnsupportedPlatform"));

#if WINDOWS
            try
            {
                return await KeyCredentialManager.IsSupportedAsync()
                    ? ResultadoBiometria.Ok()
                    : ResultadoBiometria.Falha(Idioma.Texto("Biometric.NotConfigured"));
            }
            catch (Exception ex)
            {
                return ResultadoBiometria.Falha(Idioma.Formatar("Biometric.AvailabilityError", ErrosUi.MensagemAmigavel(ex)));
            }
#else
            await Task.CompletedTask;
            return ResultadoBiometria.Falha(Idioma.Texto("Biometric.UnsupportedPlatform"));
#endif
        }

        private void ExcluirRegistro()
        {
            try
            {
                if (File.Exists(_caminhoRegistro))
                    File.Delete(_caminhoRegistro);
            }
            catch
            {
            }
        }

        internal static bool RegistroValido(RegistroBiometrico? registro) =>
            registro is { Versao: VersaoAtual } &&
            !string.IsNullOrWhiteSpace(registro.Credencial) &&
            !string.IsNullOrWhiteSpace(registro.Desafio) &&
            !string.IsNullOrWhiteSpace(registro.Nonce) &&
            !string.IsNullOrWhiteSpace(registro.Tag) &&
            !string.IsNullOrWhiteSpace(registro.ChaveCifrada);

        // Envelope/versionamento puros (sem dependência de WinRT/hardware): ficam fora do
        // #if WINDOWS de propósito, pra dar pra testar em qualquer SO sem chave de
        // segurança física — só quem assina o desafio (AssinarAsync, abaixo) depende
        // de verdade da API do Windows Hello.
        internal static RegistroBiometrico EnvelopeChave(byte[] chaveMestra, byte[] desafio, byte[] assinatura)
        {
            var chaveEnvelope = DerivarChaveEnvelope(assinatura);
            CryptographicOperations.ZeroMemory(assinatura);

            var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
            var cifrada = new byte[chaveMestra.Length];
            var tag = new byte[TamanhoTag];

            try
            {
                using var aes = new AesGcm(chaveEnvelope, TamanhoTag);
                aes.Encrypt(nonce, chaveMestra, cifrada, tag);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(chaveEnvelope);
            }

            return new RegistroBiometrico
            {
                Versao = VersaoAtual,
                Credencial = NomeCredencial,
                CriadoEmUtc = DateTime.UtcNow,
                Desafio = Convert.ToBase64String(desafio),
                Nonce = Convert.ToBase64String(nonce),
                Tag = Convert.ToBase64String(tag),
                ChaveCifrada = Convert.ToBase64String(cifrada)
            };
        }

        internal static byte[]? AbrirEnvelope(RegistroBiometrico registro, byte[] assinatura)
        {
            var chaveEnvelope = DerivarChaveEnvelope(assinatura);
            CryptographicOperations.ZeroMemory(assinatura);

            var nonce = Convert.FromBase64String(registro.Nonce!);
            var tag = Convert.FromBase64String(registro.Tag!);
            var cifrada = Convert.FromBase64String(registro.ChaveCifrada!);
            var chave = new byte[cifrada.Length];

            try
            {
                using var aes = new AesGcm(chaveEnvelope, TamanhoTag);
                aes.Decrypt(nonce, cifrada, tag, chave);
                return chave;
            }
            catch (CryptographicException)
            {
                CryptographicOperations.ZeroMemory(chave);
                return null;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(chaveEnvelope);
            }
        }

        private static byte[] DerivarChaveEnvelope(byte[] assinatura) => SHA256.HashData(assinatura);

#if WINDOWS
        private static async Task<(KeyCredentialStatus Status, byte[]? Assinatura)> AssinarAsync(
            KeyCredential credencial, byte[] desafio)
        {
            var buffer = CryptographicBuffer.CreateFromByteArray(desafio);
            var resultado = await credencial.RequestSignAsync(buffer);
            if (resultado.Status != KeyCredentialStatus.Success)
                return (resultado.Status, null);

            CryptographicBuffer.CopyToByteArray(resultado.Result, out var assinatura);
            return (KeyCredentialStatus.Success, assinatura);
        }

        private static ResultadoBiometria MapearStatus(KeyCredentialStatus status) => status switch
        {
            KeyCredentialStatus.UserCanceled => ResultadoBiometria.Cancelada(),
            KeyCredentialStatus.UserPrefersPassword => ResultadoBiometria.Cancelada(),
            KeyCredentialStatus.NotFound => ResultadoBiometria.Falha(Idioma.Texto("Biometric.NotConfigured")),
            KeyCredentialStatus.SecurityDeviceLocked => ResultadoBiometria.Falha(Idioma.Texto("Biometric.RetriesExhausted")),
            _ => ResultadoBiometria.Falha(Idioma.Texto("Biometric.Unavailable"))
        };

        private static async Task ExcluirCredencialAsync()
        {
            try
            {
                await KeyCredentialManager.DeleteAsync(NomeCredencial);
            }
            catch
            {
            }
        }
#else
        private static Task ExcluirCredencialAsync() => Task.CompletedTask;
#endif

        internal sealed class RegistroBiometrico
        {
            public int Versao { get; set; }
            public string? Credencial { get; set; }
            public DateTime CriadoEmUtc { get; set; }
            public string? Desafio { get; set; }
            public string? Nonce { get; set; }
            public string? Tag { get; set; }
            public string? ChaveCifrada { get; set; }
        }
    }
}
