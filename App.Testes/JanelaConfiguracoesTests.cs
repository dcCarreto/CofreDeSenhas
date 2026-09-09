using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class JanelaConfiguracoesTests
    {
        private static AcoesConfiguracoes Acoes(Action? alterarSenhaMestra = null) => new()
        {
            AlterarSenhaMestra = alterarSenhaMestra ?? (() => { }),
            RegerarQr = () => { },
            AlternarWindowsHello = () => { },
            BloquearAgora = () => { },
            Backup = () => { },
            Sincronizacao = () => { },
            ImportarCsv = () => { },
            ConectarBanco = () => { },
            DesconectarBanco = () => { },
            AtalhosTeclado = () => { },
            AbrirManual = () => { },
            LimparCofre = () => { },
            ExcluirCofre = () => { },
            DefinirBloqueioAutomatico = _ => { },
            DefinirVerificarAtualizacoes = _ => { },
            DefinirIconesOnline = _ => { },
            WindowsHelloSuportado = true,
            WindowsHelloAtivo = false,
            BancoConectado = false
        };

        [AvaloniaFact]
        public async Task Abrir_MostraSeteAbas_ComUmPainelVisivel()
        {
            var janela = new JanelaConfiguracoes(Acoes());
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var trilha = janela.Encontrar<StackPanel>("TrilhaAbas");
            var area = janela.Encontrar<Panel>("AreaConteudo");

            Assert.Equal(7, trilha.Children.OfType<Button>().Count());
            Assert.Equal(7, area.Children.Count);
            Assert.Single(area.Children, c => c.IsVisible);
        }

        [AvaloniaFact]
        public async Task ClicarAba_AlternaOPainelVisivel()
        {
            var janela = new JanelaConfiguracoes(Acoes());
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var trilha = janela.Encontrar<StackPanel>("TrilhaAbas");
            var area = janela.Encontrar<Panel>("AreaConteudo");

            trilha.Children.OfType<Button>().ElementAt(3)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.True(area.Children[3].IsVisible);
            Assert.DoesNotContain(area.Children.Where((_, i) => i != 3), c => c.IsVisible);
        }

        [AvaloniaFact]
        public async Task BotaoDeAcao_RegistraAcaoPendenteEFecha()
        {
            var chamou = false;
            var janela = new JanelaConfiguracoes(Acoes(() => chamou = true));
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var area = janela.Encontrar<Panel>("AreaConteudo");
            janela.Encontrar<StackPanel>("TrilhaAbas").Children.OfType<Button>().ElementAt(2)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var acao = area.Children[2].GetVisualDescendants().OfType<Button>()
                .First(b => b.Classes.Contains("cartao"));
            acao.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.NotNull(janela.AcaoPendente);
            janela.AcaoPendente!();
            Assert.True(chamou);
        }

        [AvaloniaFact]
        public async Task AbaAparencia_TrocarTema_AplicaESalva()
        {
            var modoOriginal = Acessibilidade.Modo;
            var prefOriginal = Preferencias.ModoTema;
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Escuro);

                var janela = new JanelaConfiguracoes(Acoes());
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                var temaCombo = janela.Encontrar<Panel>("AreaConteudo").Children[0]
                    .GetVisualDescendants().OfType<ComboBox>().ElementAt(1);
                temaCombo.SelectedIndex = 1;
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                Assert.Equal(ModoTema.Claro, Acessibilidade.Modo);
                Assert.Equal("Claro", Preferencias.ModoTema);
            }
            finally
            {
                Acessibilidade.DefinirModoTema(modoOriginal);
                Preferencias.ModoTema = prefOriginal;
                Preferencias.Salvar();
            }
        }

        [AvaloniaFact]
        public async Task AjustarLimpezaDoClipboard_SalvaNaPreferencia()
        {
            var original = Preferencias.SegundosLimpezaClipboard;
            try
            {
                Preferencias.SegundosLimpezaClipboard = 0;

                var janela = new JanelaConfiguracoes(Acoes());
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                janela.Encontrar<StackPanel>("TrilhaAbas").Children.OfType<Button>().ElementAt(1)
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                var combos = janela.Encontrar<Panel>("AreaConteudo").Children[1]
                    .GetVisualDescendants().OfType<ComboBox>().ToList();
                combos[1].SelectedIndex = 2;
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                Assert.Equal(30, Preferencias.SegundosLimpezaClipboard);
            }
            finally
            {
                Preferencias.SegundosLimpezaClipboard = original;
            }
        }
    }
}
