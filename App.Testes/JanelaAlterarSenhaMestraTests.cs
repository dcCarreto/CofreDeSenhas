using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;

namespace App.Testes
{
    public class JanelaAlterarSenhaMestraTests
    {
        [AvaloniaFact]
        public async Task Construtor_RegistraAnunciadorParaLeitorDeTela()
        {
            var janela = new JanelaAlterarSenhaMestra();
            janela.Show();

            Acessibilidade.Anunciar(janela, "mensagem-de-teste-leitor-de-tela", forcar: true);

            var anunciador = janela.Encontrar<TextBlock>("LblAnuncioLeitorTela");
            await TesteUtil.AguardarAsync(() => anunciador.Text == "mensagem-de-teste-leitor-de-tela");

            Assert.Equal("mensagem-de-teste-leitor-de-tela", anunciador.Text);
        }
    }
}
