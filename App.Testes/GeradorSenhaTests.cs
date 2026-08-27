using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas;
using CofreDeSenhas.Controles;

namespace App.Testes
{
    public class GeradorSenhaTests
    {
        [AvaloniaFact]
        public async Task BotaoRevelarSenhaGerada_NomeETooltipAcompanhamAVisibilidade()
        {
            // A senha gerada nasce visível (_mostrarSenha = true), mas o botão do olho
            // anunciava "Revelar senha" fixo — o oposto da ação que ele dispara.
            var gerador = new GeradorSenha();
            var janela = new Window { Content = gerador, Width = 500, Height = 600 };
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var btnOlho = gerador.FindControl<Button>("BtnOlhoGerada")!;

            Assert.Equal(Idioma.Texto("Row.HidePassword"), AutomationProperties.GetName(btnOlho));
            Assert.Equal(Idioma.Texto("Row.HidePassword"), ToolTip.GetTip(btnOlho) as string);

            btnOlho.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 3);

            Assert.Equal(Idioma.Texto("Row.RevealPassword"), AutomationProperties.GetName(btnOlho));
            Assert.Equal(Idioma.Texto("Row.RevealPassword"), ToolTip.GetTip(btnOlho) as string);
        }

        [AvaloniaFact]
        public async Task TrocaDeIdioma_NaoApagaSenhaGeradaAindaNaoSalva()
        {
            var gerador = new GeradorSenha();
            var janela = new Window { Content = gerador, Width = 500, Height = 600 };
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var btnGerar = gerador.FindControl<Button>("BtnGerar")!;
            btnGerar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var txtSenha = gerador.FindControl<TextBox>("TxtSenhaGerada")!;
            var senhaGerada = txtSenha.Text;
            Assert.False(string.IsNullOrEmpty(senhaGerada));

            try
            {
                Idioma.Definir("en");

                Assert.Equal(senhaGerada, txtSenha.Text);
            }
            finally
            {
                Idioma.Definir("pt-BR");
            }
        }
    }
}
