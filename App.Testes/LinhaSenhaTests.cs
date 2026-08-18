using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CofreDeSenhas;
using CofreDeSenhas.Controles;
using GerenciadorDeSenhas.Modelos;

namespace App.Testes
{
    public class LinhaSenhaTests
    {
        private static (Window janela, LinhaSenha linha) CriarLinhaEmJanela(Senha senha, string senhaPlain,
            Func<Senha, TipoCampoCopiado, Task>? onRegistrarCopia = null,
            Func<Senha, Task>? onFavoritar = null, Func<Senha, Task>? onFixar = null)
        {
            var linha = new LinhaSenha(senha,
                s => senhaPlain,
                s => null,
                onFavoritar ?? (s => Task.CompletedTask),
                onFixar ?? (s => Task.CompletedTask),
                s => { },
                s => Task.CompletedTask,
                (s, nome) => Task.CompletedTask,
                onRegistrarCopia);

            var janela = new Window { Content = linha, Width = 800, Height = 100 };
            janela.Show();
            return (janela, linha);
        }

        [AvaloniaFact]
        public async Task CopiarAsync_ColocaSenhaNaAreaDeTransferencia()
        {
            var senha = new Senha { Id = Guid.NewGuid(), NomeServico = "Servico", Usuario = "usuario", SenhaHash = "irrelevante-para-o-teste" };
            var (janela, linha) = CriarLinhaEmJanela(senha, "SenhaSecreta123!");

            var botaoCopiar = linha.BotaoPorNomeAutomacao(Idioma.Texto("Row.CopyPassword"));
            botaoCopiar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            string? texto = null;
            await TesteUtil.AguardarAsync(() =>
            {
                texto = janela.Clipboard?.TryGetTextAsync().GetAwaiter().GetResult();
                return !string.IsNullOrEmpty(texto);
            });

            Assert.Equal("SenhaSecreta123!", texto);
        }

        [AvaloniaFact]
        public async Task CopiarUsuarioAsync_ColocaUsuarioNaAreaDeTransferencia()
        {
            var senha = new Senha { Id = Guid.NewGuid(), NomeServico = "Servico", Usuario = "usuario.linha", SenhaHash = "irrelevante-para-o-teste" };
            var (janela, linha) = CriarLinhaEmJanela(senha, "SenhaSecreta123!");

            var rotuloUsuario = linha.TextoPorConteudo("usuario.linha");
            rotuloUsuario.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

            string? texto = null;
            await TesteUtil.AguardarAsync(() =>
            {
                texto = janela.Clipboard?.TryGetTextAsync().GetAwaiter().GetResult();
                return !string.IsNullOrEmpty(texto);
            });

            Assert.Equal("usuario.linha", texto);
        }

        [AvaloniaFact]
        public async Task RevelarSenha_ClicarNoTextoRevelado_CopiaASenhaNaoOUsuario()
        {
            var senha = new Senha { Id = Guid.NewGuid(), NomeServico = "Servico", Usuario = "usuario.linha", SenhaHash = "irrelevante-para-o-teste" };
            var (janela, linha) = CriarLinhaEmJanela(senha, "SenhaSecreta123!");

            var botaoRevelar = linha.BotaoPorNomeAutomacao(Idioma.Texto("Row.RevealPassword"));
            botaoRevelar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var textoRevelado = linha.TextoPorConteudo("SenhaSecreta123!");
            Assert.Contains(Idioma.Texto("Row.CopyPassword"), AutomationProperties.GetName(textoRevelado));
            textoRevelado.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

            string? texto = null;
            await TesteUtil.AguardarAsync(() =>
            {
                texto = janela.Clipboard?.TryGetTextAsync().GetAwaiter().GetResult();
                return !string.IsNullOrEmpty(texto);
            });

            Assert.Equal("SenhaSecreta123!", texto);
        }

        [AvaloniaFact]
        public async Task Favoritar_FicaDesabilitadoEnquantoAChamadaEstaPendenteEReabilitaDepois()
        {
            // Botão desabilitado enquanto a chamada está em voo é o que impede o
            // Avalonia de entregar um segundo clique real (ponteiro/teclado) nessa
            // janela — RaiseEvent direto no Click não passa pela checagem de IsEnabled
            // que um clique de verdade passaria, então o que dá pra provar aqui é a
            // transição de estado em si.
            var senha = new Senha { Id = Guid.NewGuid(), NomeServico = "Servico", Usuario = "usuario", SenhaHash = "irrelevante-para-o-teste" };
            var chamadas = 0;
            var liberar = new TaskCompletionSource();
            var (janela, linha) = CriarLinhaEmJanela(senha, "SenhaSecreta123!",
                onFavoritar: async s => { chamadas++; await liberar.Task; });

            var botaoFavoritar = linha.BotaoPorNomeAutomacao(Idioma.Texto("Row.FavoriteAdd"));
            botaoFavoritar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => !botaoFavoritar.IsEnabled, tentativas: 5);

            Assert.Equal(1, chamadas);
            Assert.False(botaoFavoritar.IsEnabled);

            liberar.SetResult();
            await TesteUtil.AguardarAsync(() => botaoFavoritar.IsEnabled);

            Assert.Equal(1, chamadas);
        }

        [AvaloniaFact]
        public void DefinirModoPrivacidade_QuandoEdicaoDeServicoEstaAberta_CancelaAEdicao()
        {
            var senha = new Senha { Id = Guid.NewGuid(), NomeServico = "Servico Original", Usuario = "usuario", SenhaHash = "irrelevante-para-o-teste" };
            var (janela, linha) = CriarLinhaEmJanela(senha, "SenhaSecreta123!");

            var rotuloServico = linha.TextoPorConteudo("Servico Original");
            rotuloServico.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

            var txtServico = linha.GetVisualDescendants().OfType<TextBox>().Single();
            Assert.True(txtServico.IsVisible);

            linha.DefinirModoPrivacidade(true);

            Assert.False(txtServico.IsVisible);
        }

        [AvaloniaFact]
        public void DefinirModoPrivacidade_MascaraServicoEUsuarioNaLista()
        {
            var senha = new Senha { Id = Guid.NewGuid(), NomeServico = "Servico Privado", Usuario = "usuario.privado", SenhaHash = "irrelevante-para-o-teste" };
            var (janela, linha) = CriarLinhaEmJanela(senha, "SenhaSecreta123!");

            Assert.NotNull(janela.TextoPorConteudo("Servico Privado"));
            Assert.NotNull(janela.TextoPorConteudo("usuario.privado"));

            linha.DefinirModoPrivacidade(true);

            Assert.DoesNotContain(janela.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Servico Privado");
            Assert.DoesNotContain(janela.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "usuario.privado");
            Assert.True(janela.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "••••••••") >= 2);

            linha.DefinirModoPrivacidade(false);

            Assert.NotNull(janela.TextoPorConteudo("Servico Privado"));
            Assert.NotNull(janela.TextoPorConteudo("usuario.privado"));
        }

        [AvaloniaFact]
        public async Task CopiarAsync_DisparaCallbackDeRegistrarCopiaComCampoSenha()
        {
            var senha = new Senha { Id = Guid.NewGuid(), NomeServico = "Servico", Usuario = "usuario", SenhaHash = "irrelevante-para-o-teste" };
            TipoCampoCopiado? campoRegistrado = null;
            var (janela, linha) = CriarLinhaEmJanela(senha, "SenhaSecreta123!",
                (s, campo) => { campoRegistrado = campo; return Task.CompletedTask; });

            var botaoCopiar = linha.BotaoPorNomeAutomacao(Idioma.Texto("Row.CopyPassword"));
            botaoCopiar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() => campoRegistrado != null);

            Assert.Equal(TipoCampoCopiado.Senha, campoRegistrado);
        }
    }
}
