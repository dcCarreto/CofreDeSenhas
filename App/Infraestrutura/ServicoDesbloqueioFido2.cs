using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia.Controls;
using GerenciadorDeSenhas.Servicos;

#if WINDOWS
using DSInternals.Win32.WebAuthn;
#endif

namespace CofreDeSenhas
{
    internal sealed class ResultadoFido2
    {
        private ResultadoFido2(bool sucesso, bool cancelado, byte[]? chave, string? mensagem)
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

        public static ResultadoFido2 Ok(byte[]? chave = null) => new(true, false, chave, null);
        public static ResultadoFido2 Falha(string mensagem) => new(false, false, null, mensagem);
        public static ResultadoFido2 Cancelada() => new(false, true, null, null);
    }

    internal sealed class ServicoDesbloqueioFido2
    {
        private const int VersaoAtual = 1;
        private const string RelyingPartyId = "cofredesenhas.local";
        private const string RelyingPartyNome = "Cofre de Senhas";
        private const int TamanhoDesafio = 32;
        private const int TamanhoSalt = 32;
        private const int TamanhoNonce = 12;
        private const int TamanhoTag = 16;
        private const uint TimeoutCerimoniaMs = 60_000;

        private readonly string _caminhoRegistro;

        public ServicoDesbloqueioFido2(string? pastaApp = null)
        {
            var pasta = pastaApp ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                CaminhosApp.PastaDados);

            _caminhoRegistro = Path.Combine(pasta, "fido2.dat");
        }

        public bool SistemaSuportado =>
#if WINDOWS
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            WebAuthnApi.IsAvailable &&
            WebAuthnApi.IsPsuedoRandomFunctionSupported;
#else
            false;
#endif

        public bool EstaHabilitado => File.Exists(_caminhoRegistro);

        public Task DesabilitarAsync()
        {
            ExcluirRegistro();
            return Task.CompletedTask;
        }

        public Task<bool> PodeConfigurarAsync() => Task.FromResult(SistemaSuportado);

        public async Task<ResultadoFido2> HabilitarAsync(Window janela, byte[] chaveMestra)
        {
            if (chaveMestra.Length != 32)
                return ResultadoFido2.Falha(Idioma.Texto("Fido2.InvalidVaultKey"));

            if (!SistemaSuportado)
                return ResultadoFido2.Falha(Idioma.Texto("Fido2.UnsupportedPlatform"));

#if WINDOWS
            janela.Activate();
            var wh = new WindowHandle(janela.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);

            try
            {
                var api = new WebAuthnApi();
                var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);

                var rp = new RelyingPartyInformation { Id = RelyingPartyId, Name = RelyingPartyNome };
                var usuario = new UserInformation
                {
                    Id = RandomNumberGenerator.GetBytes(16),
                    Name = "cofre-local",
                    DisplayName = RelyingPartyNome
                };

                var criacao = await api.AuthenticatorMakeCredentialAsync(
                    rp, usuario, RandomNumberGenerator.GetBytes(TamanhoDesafio),
                    UserVerificationRequirement.Required,
                    AuthenticatorAttachment.CrossPlatform,
                    ResidentKeyRequirement.Discouraged,
                    attestationConveyancePreference: AttestationConveyancePreference.None,
                    timeoutMilliseconds: TimeoutCerimoniaMs,
                    extensions: new AuthenticationExtensionsClientAttestationInputs
                    {
                        Prf = new PRFAttestationInputs { Eval = new PRFValues { First = salt } }
                    },
                    windowHandle: wh);

                if (criacao.ClientExtensionResults?.Prf?.Enabled != true)
                    return ResultadoFido2.Falha(Idioma.Texto("Fido2.PrfNotSupported"));

                var segredo = criacao.ClientExtensionResults.Prf.Results?.First;
                if (segredo == null)
                {
                    var confirmacao = await api.AuthenticatorGetAssertionAsync(
                        RelyingPartyId, RandomNumberGenerator.GetBytes(TamanhoDesafio),
                        UserVerificationRequirement.Required,
                        AuthenticatorAttachment.CrossPlatform,
                        timeoutMilliseconds: TimeoutCerimoniaMs,
                        allowCredentials: new[] { new PublicKeyCredentialDescriptor(criacao.Id) },
                        extensions: new AuthenticationExtensionsClientAssertionInputs
                        {
                            Prf = new PRFAssertionInputs { Eval = new PRFValues { First = salt } }
                        },
                        windowHandle: wh);

                    segredo = confirmacao.ClientExtensionResults?.Prf?.Results?.First;
                }

                if (segredo == null)
                    return ResultadoFido2.Falha(Idioma.Texto("Fido2.PrfNotSupported"));

                var registro = EnvelopeChave(chaveMestra, criacao.Id, salt, segredo);

                var pasta = Path.GetDirectoryName(_caminhoRegistro)!;
                if (!Directory.Exists(pasta))
                    Directory.CreateDirectory(pasta);

                File.WriteAllText(_caminhoRegistro, JsonSerializer.Serialize(registro));
                return ResultadoFido2.Ok();
            }
            catch (OperationCanceledException)
            {
                return ResultadoFido2.Cancelada();
            }
            catch (Exception ex)
            {
                return ResultadoFido2.Falha(Idioma.Formatar("Fido2.EnableError", ex.Message));
            }
#else
            _ = janela;
            await Task.CompletedTask;
            return ResultadoFido2.Falha(Idioma.Texto("Fido2.UnsupportedPlatform"));
#endif
        }

        public async Task<ResultadoFido2> DesbloquearAsync(Window janela, AutenticacaoMestra autenticacao)
        {
            if (!EstaHabilitado)
                return ResultadoFido2.Falha(Idioma.Texto("Fido2.NotEnabled"));

            if (!SistemaSuportado)
                return ResultadoFido2.Falha(Idioma.Texto("Fido2.UnsupportedPlatform"));

#if WINDOWS
            RegistroFido2? registro;
            try
            {
                registro = JsonSerializer.Deserialize<RegistroFido2>(File.ReadAllText(_caminhoRegistro));
            }
            catch (Exception ex)
            {
                return ResultadoFido2.Falha(Idioma.Formatar("Fido2.UnlockError", ex.Message));
            }

            if (!RegistroValido(registro))
            {
                await DesabilitarAsync();
                return ResultadoFido2.Falha(Idioma.Texto("Fido2.InvalidRegistration"));
            }

            janela.Activate();
            var wh = new WindowHandle(janela.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);

            try
            {
                var api = new WebAuthnApi();
                var credencialId = Convert.FromBase64String(registro!.CredentialId!);
                var salt = Convert.FromBase64String(registro.Salt!);

                var assercao = await api.AuthenticatorGetAssertionAsync(
                    RelyingPartyId, RandomNumberGenerator.GetBytes(TamanhoDesafio),
                    UserVerificationRequirement.Required,
                    AuthenticatorAttachment.CrossPlatform,
                    timeoutMilliseconds: TimeoutCerimoniaMs,
                    allowCredentials: new[] { new PublicKeyCredentialDescriptor(credencialId) },
                    extensions: new AuthenticationExtensionsClientAssertionInputs
                    {
                        Prf = new PRFAssertionInputs { Eval = new PRFValues { First = salt } }
                    },
                    windowHandle: wh);

                var segredo = assercao.ClientExtensionResults?.Prf?.Results?.First;
                if (segredo == null)
                {
                    await DesabilitarAsync();
                    return ResultadoFido2.Falha(Idioma.Texto("Fido2.InvalidRegistration"));
                }

                var chave = AbrirEnvelope(registro, segredo);
                if (chave == null || !autenticacao.ValidarChave(chave))
                {
                    if (chave != null)
                        CryptographicOperations.ZeroMemory(chave);
                    await DesabilitarAsync();
                    return ResultadoFido2.Falha(Idioma.Texto("Fido2.InvalidRegistration"));
                }

                return ResultadoFido2.Ok(chave);
            }
            catch (OperationCanceledException)
            {
                return ResultadoFido2.Cancelada();
            }
            catch (Exception ex)
            {
                return ResultadoFido2.Falha(Idioma.Formatar("Fido2.UnlockError", ex.Message));
            }
#else
            _ = janela;
            _ = autenticacao;
            await Task.CompletedTask;
            return ResultadoFido2.Falha(Idioma.Texto("Fido2.UnsupportedPlatform"));
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

        private static bool RegistroValido(RegistroFido2? registro) =>
            registro is { Versao: VersaoAtual } &&
            !string.IsNullOrWhiteSpace(registro.CredentialId) &&
            !string.IsNullOrWhiteSpace(registro.Salt) &&
            !string.IsNullOrWhiteSpace(registro.Nonce) &&
            !string.IsNullOrWhiteSpace(registro.Tag) &&
            !string.IsNullOrWhiteSpace(registro.ChaveCifrada);

#if WINDOWS
        private static RegistroFido2 EnvelopeChave(byte[] chaveMestra, byte[] credencialId, byte[] salt, byte[] segredo)
        {
            var chaveEnvelope = DerivarChaveEnvelope(segredo);
            CryptographicOperations.ZeroMemory(segredo);

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

            return new RegistroFido2
            {
                Versao = VersaoAtual,
                CredentialId = Convert.ToBase64String(credencialId),
                Salt = Convert.ToBase64String(salt),
                CriadoEmUtc = DateTime.UtcNow,
                Nonce = Convert.ToBase64String(nonce),
                Tag = Convert.ToBase64String(tag),
                ChaveCifrada = Convert.ToBase64String(cifrada)
            };
        }

        private static byte[]? AbrirEnvelope(RegistroFido2 registro, byte[] segredo)
        {
            var chaveEnvelope = DerivarChaveEnvelope(segredo);
            CryptographicOperations.ZeroMemory(segredo);

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

        private static byte[] DerivarChaveEnvelope(byte[] segredo) => SHA256.HashData(segredo);
#endif

        private sealed class RegistroFido2
        {
            public int Versao { get; set; }
            public string? CredentialId { get; set; }
            public string? Salt { get; set; }
            public DateTime CriadoEmUtc { get; set; }
            public string? Nonce { get; set; }
            public string? Tag { get; set; }
            public string? ChaveCifrada { get; set; }
        }
    }
}
