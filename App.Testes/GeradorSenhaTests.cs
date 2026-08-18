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
