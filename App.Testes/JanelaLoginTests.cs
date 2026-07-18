using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    public class JanelaLoginTests
    {
        [AvaloniaFact]
        public async Task Desbloqueio_ComSenhaCorreta_DisparaCallbackComAChave()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            var chaveOriginal = auth.CriarSenhaMestra("SenhaDeTeste123!");

            byte[]? chaveRecebida = null;
            var login = new JanelaLogin(auth, (chave, senha) => chaveRecebida = chave);
            login.Show();

            login.Encontrar<TextBox>("TxtSenha").Text = "SenhaDeTeste123!";
            login.Encontrar<Button>("BtnPrincipal").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() => chaveRecebida != null);

            Assert.NotNull(chaveRecebida);
            Assert.Equal(chaveOriginal, chaveRecebida);
        }

        [AvaloniaFact]
        public async Task Desbloqueio_ComSenhaErrada_MostraErroENaoDisparaCallback()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            auth.CriarSenhaMestra("SenhaDeTeste123!");

            byte[]? chaveRecebida = null;
            var login = new JanelaLogin(auth, (chave, senha) => chaveRecebida = chave);
            login.Show();

            login.Encontrar<TextBox>("TxtSenha").Text = "SenhaErrada!";
            login.Encontrar<Button>("BtnPrincipal").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var lblErro = login.Encontrar<TextBlock>("LblErro");
            await TesteUtil.AguardarAsync(() => !string.IsNullOrEmpty(lblErro.Text));

            Assert.Null(chaveRecebida);
            Assert.False(string.IsNullOrEmpty(lblErro.Text));
        }
    }
}
