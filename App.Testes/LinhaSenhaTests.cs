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
            Func<Senha, TipoCampoCopiado, Task>? onRegistrarCopia = null)
        {
            var linha = new LinhaSenha(senha,
                s => senhaPlain,
                s => null,
                s => { },
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
