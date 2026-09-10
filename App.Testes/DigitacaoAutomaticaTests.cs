using System.Linq;
using CofreDeSenhas;
using Xunit;

namespace App.Testes
{
    public class DigitacaoAutomaticaTests
    {
        private static char[] Caracteres(System.Collections.Generic.IEnumerable<DigitacaoAutomatica.EntradaDigitacao> seq) =>
            seq.Where(e => e.Tipo == DigitacaoAutomatica.TipoEntrada.Caractere).Select(e => e.Caractere).ToArray();

        [Fact]
        public void MontarSequencia_UsuarioESenha_DigitaUsuarioTabSenhaNessaOrdem()
        {
            var seq = DigitacaoAutomatica.MontarSequencia("ana", "s3nh4").ToList();

            Assert.Equal(9, seq.Count);
            Assert.Equal(new[] { 'a', 'n', 'a' }, Caracteres(seq.Take(3)));
            Assert.Equal(DigitacaoAutomatica.TipoEntrada.Tab, seq[3].Tipo);
            Assert.Equal(new[] { 's', '3', 'n', 'h', '4' }, Caracteres(seq.Skip(4)));
        }

        [Fact]
        public void MontarSequencia_UsaExatamenteUmTabComoSeparador()
        {
            var seq = DigitacaoAutomatica.MontarSequencia("usuario", "senha");

            Assert.Single(seq, e => e.Tipo == DigitacaoAutomatica.TipoEntrada.Tab);
        }

        [Fact]
        public void MontarSequencia_NuncaEnviaEnter()
        {
            var seq = DigitacaoAutomatica.MontarSequencia("linha1\r\nlinha2", "a\nb\rc");

            Assert.DoesNotContain(Caracteres(seq), c => c == '\r' || c == '\n');
            Assert.Equal(new[] { 'l', 'i', 'n', 'h', 'a', '1', 'l', 'i', 'n', 'h', 'a', '2' }, Caracteres(seq.Take(12)));
            Assert.Equal(new[] { 'a', 'b', 'c' }, Caracteres(seq.Skip(13)));
        }

        [Fact]
        public void MontarSequencia_TabDentroDoValor_NaoViraSeparadorExtra()
        {
            var seq = DigitacaoAutomatica.MontarSequencia("a\tb", "x\ty");

            Assert.Single(seq, e => e.Tipo == DigitacaoAutomatica.TipoEntrada.Tab);
            Assert.Equal(new[] { 'a', 'b', 'x', 'y' }, Caracteres(seq));
        }

        [Fact]
        public void MontarSequencia_SemUsuario_DigitaSoASenhaSemTab()
        {
            var seq = DigitacaoAutomatica.MontarSequencia("", "apenas-senha").ToList();

            Assert.DoesNotContain(seq, e => e.Tipo == DigitacaoAutomatica.TipoEntrada.Tab);
            Assert.Equal("apenas-senha".ToCharArray(), Caracteres(seq));
        }

        [Fact]
        public void MontarSequencia_TudoVazio_RetornaVazio()
        {
            Assert.Empty(DigitacaoAutomatica.MontarSequencia("", ""));
        }

        [Fact]
        public void Suportado_SegueOSistemaOperacional()
        {
            Assert.Equal(System.OperatingSystem.IsWindows(), DigitacaoAutomatica.Suportado);
        }

        [Fact]
        public void AlvoAtual_SemObservador_RetornaNulo()
        {
            DigitacaoAutomatica.PararDeObservar();
            Assert.Null(DigitacaoAutomatica.AlvoAtual());
        }
    }
}
