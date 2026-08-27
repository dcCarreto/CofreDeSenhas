using Avalonia.Input;
using CofreDeSenhas;
using Xunit;

namespace App.Testes
{
    public class AtalhosTecladoTests
    {
        [Fact]
        public void Encontrar_ComCtrlSozinho_RetornaOAtalhoCorrespondente()
        {
            var atalho = AtalhosTeclado.Encontrar(Key.L, KeyModifiers.Control);

            Assert.NotNull(atalho);
            Assert.Equal(AtalhosTeclado.Acao.BloquearAgora, atalho!.Acao);
        }

        [Fact]
        public void Encontrar_ComCtrlEAlt_RetornaNulo()
        {
            // AltGr (comum em teclados PT, FR, DE, ES, IT) chega ao Windows como
            // Ctrl+Alt sintético — sem essa exclusão, digitar um símbolo com AltGr em
            // qualquer campo de texto da janela disparava um atalho por engano.
            var atalho = AtalhosTeclado.Encontrar(Key.L, KeyModifiers.Control | KeyModifiers.Alt);

            Assert.Null(atalho);
        }

        [Fact]
        public void Encontrar_ComCtrlAltEShift_RetornaNulo()
        {
            var atalho = AtalhosTeclado.Encontrar(Key.P, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift);

            Assert.Null(atalho);
        }

        [Fact]
        public void Encontrar_ComCtrlShiftParaAtalhoQueExigeShift_RetornaOAtalhoCorrespondente()
        {
            var atalho = AtalhosTeclado.Encontrar(Key.U, KeyModifiers.Control | KeyModifiers.Shift);

            Assert.NotNull(atalho);
            Assert.Equal(AtalhosTeclado.Acao.CopiarUsuario, atalho!.Acao);
        }

        [Fact]
        public void Encontrar_SemCtrl_RetornaNulo()
        {
            var atalho = AtalhosTeclado.Encontrar(Key.L, KeyModifiers.None);

            Assert.Null(atalho);
        }
    }
}
