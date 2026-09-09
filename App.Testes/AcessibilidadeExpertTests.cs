using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using CofreDeSenhas;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class AcessibilidadeExpertTests
    {
        public AcessibilidadeExpertTests() => Restaurar();

        private static void Restaurar()
        {
            Acessibilidade.Hidratar(TipoDaltonismo.Nenhum, ModoTema.Escuro, CorDestaque.Ambar, Densidade.Confortavel,
                LayoutDetalhe.Lateral, false, Acessibilidade.EscalaNormal, false, false);
            Acessibilidade.HidratarExpert(NivelContraste.Padrao, NivelMovimento.Completo, FonteLeitura.Padrao,
                EspacamentoTexto.Normal, VerbosidadeLeitor.Normal, false, false, false, false, false);
        }

        [AvaloniaFact]
        public void NormalizarEscala_SnapaParaOsSeisPassos()
        {
            Acessibilidade.DefinirEscala(1.62);
            Assert.Equal(1.5, Acessibilidade.Escala);

            Acessibilidade.DefinirEscala(5);
            Assert.Equal(2.0, Acessibilidade.Escala);

            Acessibilidade.DefinirEscala(0.5);
            Assert.Equal(1.0, Acessibilidade.Escala);
            Restaurar();
        }

        [AvaloniaFact]
        public void Contraste_Medio_ReforcaTextoMasNaoBordas()
        {
            try
            {
                Acessibilidade.DefinirContraste(NivelContraste.Medio);

                Assert.True(Acessibilidade.ContrasteMedio);
                Assert.False(Acessibilidade.AltoContraste);
                Assert.Equal(Acessibilidade.TextoPrincipal(), Acessibilidade.Cor(CorVisual.TextPrimary));
                Assert.NotEqual(Acessibilidade.Borda(), Acessibilidade.Cor(CorVisual.CardBorder));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void Contraste_Alto_ReforcaTextoEBordas()
        {
            try
            {
                Acessibilidade.DefinirContraste(NivelContraste.Alto);

                Assert.True(Acessibilidade.AltoContraste);
                Assert.Equal(Acessibilidade.TextoPrincipal(), Acessibilidade.Cor(CorVisual.TextPrimary));
                Assert.Equal(Acessibilidade.Borda(), Acessibilidade.Cor(CorVisual.CardBorder));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void Movimento_MapeiaParaReduzirESemAnimacao()
        {
            Acessibilidade.DefinirMovimento(NivelMovimento.Reduzido);
            Assert.True(Acessibilidade.ReduzirAnimacoes);
            Assert.False(Acessibilidade.SemAnimacao);

            Acessibilidade.DefinirMovimento(NivelMovimento.SemAnimacao);
            Assert.True(Acessibilidade.ReduzirAnimacoes);
            Assert.True(Acessibilidade.SemAnimacao);

            Acessibilidade.DefinirMovimento(NivelMovimento.Completo);
            Assert.False(Acessibilidade.ReduzirAnimacoes);
            Restaurar();
        }

        [AvaloniaFact]
        public void Aplicar_TrocaFonteEspacamentoEFoco()
        {
            try
            {
                Acessibilidade.DefinirFonte(FonteLeitura.Legivel);
                Acessibilidade.DefinirEspacamento(EspacamentoTexto.Amplo);
                Acessibilidade.DefinirFocoReforcado(true);
                Acessibilidade.Aplicar();

                var fonte = Assert.IsType<FontFamily>(Application.Current!.Resources["FontePadrao"]);
                Assert.Contains("Atkinson", fonte.Name);
                Assert.Equal(1.1, Application.Current!.Resources["EspacamentoTexto"]);
                Assert.Equal(new Thickness(3), Application.Current!.Resources["EspessuraFoco"]);
            }
            finally
            {
                Restaurar();
                Acessibilidade.Aplicar();
            }
        }

        [AvaloniaFact]
        public async Task Anunciar_ComAnunciarAcoes_MostraOToastEnaoLanca()
        {
            try
            {
                var toast = new Border();
                var texto = new TextBlock();
                var janela = new Window { Content = new Panel { Children = { toast, texto } } };
                janela.Show();
                Acessibilidade.RegistrarToast(janela, toast, texto);

                Acessibilidade.DefinirAnunciarAcoes(true);
                Acessibilidade.Anunciar(janela, "senha copiada");
                await TesteUtil.AguardarAsync(() => toast.IsVisible, tentativas: 10);

                Assert.True(toast.IsVisible);
                Assert.Equal("senha copiada", texto.Text);
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public async Task Anunciar_SemLeitorNemAnunciarAcoes_NaoMostraToast()
        {
            try
            {
                var toast = new Border();
                var texto = new TextBlock();
                var janela = new Window { Content = new Panel { Children = { toast, texto } } };
                janela.Show();
                Acessibilidade.RegistrarToast(janela, toast, texto);

                Acessibilidade.Anunciar(janela, "nao deve aparecer");
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                Assert.False(toast.IsVisible);
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void SelecionarFonte_SalvaNaPreferencia()
        {
            var original = Preferencias.FonteLeitura;
            try
            {
                Acessibilidade.SelecionarFonte("Serifada");
                Assert.Equal(FonteLeitura.Serifada, Acessibilidade.Fonte);
                Assert.Equal("Serifada", Preferencias.FonteLeitura);
            }
            finally
            {
                Restaurar();
                Preferencias.FonteLeitura = original;
                Preferencias.Salvar();
            }
        }

        [Fact]
        public void Preferencias_AcessibilidadeExpert_SobreviveAoRoundTrip()
        {
            var snapshot = (Preferencias.NivelContraste, Preferencias.NivelMovimento, Preferencias.FonteLeitura,
                Preferencias.EspacamentoTexto, Preferencias.VerbosidadeLeitor, Preferencias.EscalaAutomatica,
                Preferencias.SublinharLinks, Preferencias.FocoReforcado, Preferencias.AnunciarAcoes,
                Preferencias.AvisarAntesBloqueio);
            try
            {
                Preferencias.NivelContraste = "Medio";
                Preferencias.NivelMovimento = "SemAnimacao";
                Preferencias.FonteLeitura = "Legivel";
                Preferencias.EspacamentoTexto = "Amplo";
                Preferencias.VerbosidadeLeitor = "Detalhada";
                Preferencias.EscalaAutomatica = true;
                Preferencias.SublinharLinks = true;
                Preferencias.FocoReforcado = true;
                Preferencias.AnunciarAcoes = true;
                Preferencias.AvisarAntesBloqueio = true;
                Preferencias.Salvar();

                Preferencias.NivelContraste = Preferencias.NivelMovimento = Preferencias.FonteLeitura =
                    Preferencias.EspacamentoTexto = Preferencias.VerbosidadeLeitor = null;
                Preferencias.EscalaAutomatica = Preferencias.SublinharLinks = Preferencias.FocoReforcado =
                    Preferencias.AnunciarAcoes = Preferencias.AvisarAntesBloqueio = false;
                Preferencias.Carregar();

                Assert.Equal("Medio", Preferencias.NivelContraste);
                Assert.Equal("SemAnimacao", Preferencias.NivelMovimento);
                Assert.Equal("Legivel", Preferencias.FonteLeitura);
                Assert.Equal("Amplo", Preferencias.EspacamentoTexto);
                Assert.Equal("Detalhada", Preferencias.VerbosidadeLeitor);
                Assert.True(Preferencias.EscalaAutomatica);
                Assert.True(Preferencias.SublinharLinks);
                Assert.True(Preferencias.FocoReforcado);
                Assert.True(Preferencias.AnunciarAcoes);
                Assert.True(Preferencias.AvisarAntesBloqueio);
            }
            finally
            {
                (Preferencias.NivelContraste, Preferencias.NivelMovimento, Preferencias.FonteLeitura,
                    Preferencias.EspacamentoTexto, Preferencias.VerbosidadeLeitor, Preferencias.EscalaAutomatica,
                    Preferencias.SublinharLinks, Preferencias.FocoReforcado, Preferencias.AnunciarAcoes,
                    Preferencias.AvisarAntesBloqueio) = snapshot;
                Preferencias.Salvar();
            }
        }
    }
}
