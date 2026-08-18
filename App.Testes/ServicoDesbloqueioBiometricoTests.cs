using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CofreDeSenhas;

namespace App.Testes
{
    public class ServicoDesbloqueioBiometricoTests
    {
        [Fact]
        public void EnvelopeChaveEAbrirEnvelope_ComMesmaAssinatura_RoundTripPreservaAChave()
        {
            var chaveMestra = RandomNumberGenerator.GetBytes(32);
            var desafio = RandomNumberGenerator.GetBytes(32);
            var assinatura = RandomNumberGenerator.GetBytes(64);
            var copiaAssinatura = (byte[])assinatura.Clone();

            var registro = ServicoDesbloqueioBiometrico.EnvelopeChave(chaveMestra, desafio, assinatura);
            var chaveRecuperada = ServicoDesbloqueioBiometrico.AbrirEnvelope(registro, copiaAssinatura);

            Assert.Equal(chaveMestra, chaveRecuperada);
        }

        [Fact]
        public void AbrirEnvelope_ComAssinaturaErrada_RetornaNulo()
        {
            var chaveMestra = RandomNumberGenerator.GetBytes(32);
            var desafio = RandomNumberGenerator.GetBytes(32);
            var assinatura = RandomNumberGenerator.GetBytes(64);
            var assinaturaErrada = RandomNumberGenerator.GetBytes(64);

            var registro = ServicoDesbloqueioBiometrico.EnvelopeChave(chaveMestra, desafio, assinatura);
            var chaveRecuperada = ServicoDesbloqueioBiometrico.AbrirEnvelope(registro, assinaturaErrada);

            Assert.Null(chaveRecuperada);
        }

        [Fact]
        public void EnvelopeChave_ZeraAAssinaturaOriginalAposUso()
        {
            var chaveMestra = RandomNumberGenerator.GetBytes(32);
            var desafio = RandomNumberGenerator.GetBytes(32);
            var assinatura = RandomNumberGenerator.GetBytes(64);

            ServicoDesbloqueioBiometrico.EnvelopeChave(chaveMestra, desafio, assinatura);

            Assert.All(assinatura, b => Assert.Equal(0, b));
        }

        [Fact]
        public void EnvelopeChave_ProduzRegistroValido()
        {
            var chaveMestra = RandomNumberGenerator.GetBytes(32);
            var desafio = RandomNumberGenerator.GetBytes(32);
            var assinatura = RandomNumberGenerator.GetBytes(64);

            var registro = ServicoDesbloqueioBiometrico.EnvelopeChave(chaveMestra, desafio, assinatura);

            Assert.True(ServicoDesbloqueioBiometrico.RegistroValido(registro));
        }

        [Fact]
        public void RegistroValido_ComNulo_RetornaFalso() =>
            Assert.False(ServicoDesbloqueioBiometrico.RegistroValido(null));

#if WINDOWS
        [Fact]
        public void SistemaSuportado_NaTfmWindows_UsaVerificacaoRealDePlataforma()
        {
            var servico = new ServicoDesbloqueioBiometrico(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            Assert.Equal(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240),
                servico.SistemaSuportado);
        }
#else
        [Fact]
        public void SistemaSuportado_ForaDaTfmWindows_SempreFalso()
        {
            var servico = new ServicoDesbloqueioBiometrico(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            Assert.False(servico.SistemaSuportado);
        }
#endif

        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        public void RegistroValido_ComVersaoDiferenteDaAtual_RetornaFalso(int versao)
        {
            var registro = new ServicoDesbloqueioBiometrico.RegistroBiometrico
            {
                Versao = versao,
                Credencial = "x",
                Desafio = "x",
                Nonce = "x",
                Tag = "x",
                ChaveCifrada = "x"
            };

            Assert.False(ServicoDesbloqueioBiometrico.RegistroValido(registro));
        }

        [Theory]
        [InlineData(null, "x", "x", "x", "x")]
        [InlineData("x", null, "x", "x", "x")]
        [InlineData("x", "x", null, "x", "x")]
        [InlineData("x", "x", "x", null, "x")]
        [InlineData("x", "x", "x", "x", null)]
        public void RegistroValido_ComCampoObrigatorioFaltando_RetornaFalso(
            string? credencial, string? desafio, string? nonce, string? tag, string? chaveCifrada)
        {
            var registro = new ServicoDesbloqueioBiometrico.RegistroBiometrico
            {
                Versao = ServicoDesbloqueioBiometrico.VersaoAtual,
                Credencial = credencial,
                Desafio = desafio,
                Nonce = nonce,
                Tag = tag,
                ChaveCifrada = chaveCifrada
            };

            Assert.False(ServicoDesbloqueioBiometrico.RegistroValido(registro));
        }
    }
}
