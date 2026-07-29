using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
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

        [AvaloniaFact]
        public async Task AplicarImportacaoAsync_ComCampoMuitoLongo_ContaComoInvalidaSemAbortarOResto()
        {
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var itens = new List<SenhaExportada>
            {
                new()
                {
                    NomeServico = new string('a', 200),
                    Usuario = "usuario.invalido",
                    Senha = "SenhaForte123!",
                    Categoria = Categoria.Personal
                },
                new()
                {
                    NomeServico = "Servico Valido",
                    Usuario = "usuario.valido",
                    Senha = "SenhaForte456!",
                    Categoria = Categoria.Personal
                }
            };

            var (adicionadas, invalidas, duplicadas) = await janela.AplicarImportacaoAsync(itens);

            Assert.Equal(1, adicionadas);
            Assert.Equal(1, invalidas);
            Assert.Equal(0, duplicadas);

            var todas = await servico.ListarTodosAsync();
            var unica = Assert.Single(todas);
            Assert.Equal("Servico Valido", unica.NomeServico);
        }

        [AvaloniaFact]
        public async Task NavegarParaLixeira_MostraItemExcluidoEPermiteRestaurar()
        {
            var (servico, chave) = CriarServico();
            var criada = await servico.CriarSenhaAsync("Servico Excluido", "usuario.excluido", "SenhaForte123!", Categoria.Personal);
            await servico.RemoverSenhaAsync(criada.Id);
            await servico.PersistirAsync();

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.Encontrar<Button>("BtnNavLixeira").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Excluido"));

            var botaoRestaurar = janela.BotaoPorTexto(Idioma.Texto("Trash.Restore"));
            botaoRestaurar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            List<Senha> ativos = new();
            await TesteUtil.AguardarAsync(() =>
            {
                ativos = servico.ListarTodosAsync().GetAwaiter().GetResult();
                return ativos.Any(s => s.Id == criada.Id);
            });

            Assert.Contains(ativos, s => s.Id == criada.Id);
        }

        [AvaloniaFact]
        public async Task CtrlClicarDuasLinhas_MostraPainelDeAcoesEmLoteEFavoritarAplicaNasDuas()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Servico Lote A", "usuario.a", "SenhaForte123!", Categoria.Personal);
            await servico.CriarSenhaAsync("Servico Lote B", "usuario.b", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<LinhaSenha>().Count() == 2);

            foreach (var linha in janela.GetVisualDescendants().OfType<LinhaSenha>().ToList())
            {
                var ponto = linha.TranslatePoint(new Point(2, 26), janela) ?? default;
                janela.MouseDown(ponto, MouseButton.Left, RawInputModifiers.Control);
                janela.MouseUp(ponto, MouseButton.Left, RawInputModifiers.Control);
            }

            await TesteUtil.AguardarAsync(() => janela.Encontrar<Border>("PainelAcoesLote").IsVisible);
            Assert.Equal(Idioma.Plural(2, "Batch.CountSingular", "Batch.CountPlural"),
                janela.Encontrar<TextBlock>("LblContagemSelecao").Text);

            janela.Encontrar<Button>("BtnLoteFavoritar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            List<Senha> todas = new();
            await TesteUtil.AguardarAsync(() =>
            {
                todas = servico.ListarTodosAsync().GetAwaiter().GetResult();
                return todas.Count(s => s.Favorito) == 2;
            });

            Assert.Equal(2, todas.Count(s => s.Favorito));
        }

        [AvaloniaFact]
        public async Task LoteAdicionarEtiqueta_AplicaAEtiquetaNosItensSelecionados()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("Servico Etiqueta A", "usuario.a", "SenhaForte123!", Categoria.Personal);
            await servico.CriarSenhaAsync("Servico Etiqueta B", "usuario.b", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<LinhaSenha>().Count() == 2);

            foreach (var linha in janela.GetVisualDescendants().OfType<LinhaSenha>().ToList())
            {
                var ponto = linha.TranslatePoint(new Point(2, 26), janela) ?? default;
                janela.MouseDown(ponto, MouseButton.Left, RawInputModifiers.Control);
                janela.MouseUp(ponto, MouseButton.Left, RawInputModifiers.Control);
            }

            await TesteUtil.AguardarAsync(() => janela.Encontrar<Border>("PainelAcoesLote").IsVisible);

            janela.Encontrar<Button>("BtnLoteEtiqueta").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => janela.Encontrar<StackPanel>("PainelAcoesLoteEtiqueta").IsVisible);

            janela.Encontrar<TextBox>("TxtLoteEtiqueta").Text = "urgente";
            janela.Encontrar<Button>("BtnLoteEtiquetaAplicar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            List<Senha> todas = new();
            await TesteUtil.AguardarAsync(() =>
            {
                todas = servico.ListarTodosAsync().GetAwaiter().GetResult();
                return todas.Count(s => s.Etiquetas.Contains("urgente")) == 2;
            });

            Assert.Equal(2, todas.Count(s => s.Etiquetas.Contains("urgente")));
        }
    }
}
