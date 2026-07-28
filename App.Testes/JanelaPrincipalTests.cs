using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CofreDeSenhas;
using CofreDeSenhas.Controles;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    public class JanelaPrincipalTests
    {
        private static (IServicoSenha servico, byte[] chave) CriarServico()
        {
            var (servico, chave, _) = CriarServicoComCriptografia();
            return (servico, chave);
        }

        private static (IServicoSenha servico, byte[] chave, IServicoCriptografia criptografia) CriarServicoComCriptografia()
        {
            var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
            var criptografia = new ServicoCriptografia(chave);
            var persistencia = new PersistenciaLocal(criptografia, TesteUtil.CriarPastaTemporaria());
            var repositorio = new RepositorioSenha(persistencia, chave);
            return (new ServicoSenha(repositorio, criptografia), chave, criptografia);
        }

        [AvaloniaFact]
        public async Task AtalhoBloquearAgora_DisparaCallbackDeBloqueio()
        {
            var (servico, chave) = CriarServico();
            var bloqueado = false;
            var janela = new JanelaPrincipal(servico, chave, aoBloquear: () => bloqueado = true);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.L,
                KeyModifiers = KeyModifiers.Control
            });

            Assert.True(bloqueado);
        }

        [AvaloniaFact]
        public async Task TrocaDeIdioma_AtualizaTextoDaJanelaPrincipal()
        {
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            try
            {
                Idioma.Definir("en");

                Assert.Equal("Search by service, user, or category", janela.Encontrar<TextBox>("TxtBusca").Watermark);
            }
            finally
            {
                Idioma.Definir("pt-BR");
            }
        }

        [AvaloniaFact]
        public async Task AtalhoModoPrivacidade_MascaraListaDeCredenciais()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Servico Sensivel", "usuario.sensivel", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Sensivel"));

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.H,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "••••••••"));

            Assert.DoesNotContain(janela.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Servico Sensivel");
        }

        [AvaloniaFact]
        public async Task AtalhoModoPrivacidade_MascaraNomeAcessivelETooltipsDaLinha()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Servico Sensivel", "usuario.sensivel", "SenhaForte123!", Categoria.Work);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Sensivel"));

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.H,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "••••••••"));

            var linha = janela.GetVisualDescendants().OfType<LinhaSenha>().Single();

            var nomeLinha = AutomationProperties.GetName(linha);
            Assert.DoesNotContain("Servico Sensivel", nomeLinha ?? "");
            Assert.DoesNotContain("usuario.sensivel", nomeLinha ?? "");

            foreach (var descendente in linha.GetVisualDescendants().OfType<Control>())
            {
                var nome = AutomationProperties.GetName(descendente);
                Assert.DoesNotContain("Servico Sensivel", nome ?? "");
                Assert.DoesNotContain("usuario.sensivel", nome ?? "");

                var dica = ToolTip.GetTip(descendente) as string;
                Assert.DoesNotContain("Servico Sensivel", dica ?? "");
                Assert.DoesNotContain("usuario.sensivel", dica ?? "");
            }
        }

        [AvaloniaFact]
        public async Task AtalhoModoPrivacidade_MascaraTooltipDaEtiquetaNoChipDeCategoria()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Servico Etiquetado", "usuario.etiquetado", "SenhaForte123!",
                Categoria.Other, etiquetas: new[] { "EtiquetaSecreta" });

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Etiquetado"));

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.H,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "••••••••"));

            var linha = janela.GetVisualDescendants().OfType<LinhaSenha>().Single();

            foreach (var descendente in linha.GetVisualDescendants().OfType<Control>())
            {
                var nome = AutomationProperties.GetName(descendente);
                Assert.DoesNotContain("EtiquetaSecreta", nome ?? "");

                var dica = ToolTip.GetTip(descendente) as string;
                Assert.DoesNotContain("EtiquetaSecreta", dica ?? "");
            }
        }

        [AvaloniaFact]
        public async Task CopiarSenhaPelaLista_RegistraDataDeUltimaCopia()
        {
            var frequenciaOriginal = Preferencias.FrequenciaBackup;
            try
            {
                // "Manual" impede que JanelaPrincipal.IniciarAsync dispare um backup automático,
                // que tocaria o %APPDATA% real do desenvolvedor (fora do cofre descartável do teste).
                Preferencias.FrequenciaBackup = "Manual";

                var (servico, chave, criptografia) = CriarServicoComCriptografia();
                var criada = await servico.CriarSenhaAsync("Servico Copia", "usuario.copia", "SenhaForte123!", Categoria.Personal);

                var janela = new JanelaPrincipal(servico, chave, criptografia);
                janela.Show();
                await TesteUtil.AguardarAsync(() =>
                    janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Copia"));

                var botaoCopiar = janela.BotaoPorNomeAutomacao(Idioma.Texto("Row.CopyPassword"));
                botaoCopiar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Senha? atualizada = null;
                await TesteUtil.AguardarAsync(() =>
                {
                    atualizada = servico.ListarTodosAsync().GetAwaiter().GetResult().FirstOrDefault(s => s.Id == criada.Id);
                    return atualizada?.DataUltimaCopiaSenha != null;
                });

                Assert.NotNull(atualizada?.DataUltimaCopiaSenha);
            }
            finally
            {
                Preferencias.FrequenciaBackup = frequenciaOriginal;
            }
        }
    }
}
