using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Modelos;

namespace App.Testes
{
    public class JanelaConexaoBancoTests
    {
        [AvaloniaFact]
        public async Task Construtor_RegistraAnunciadorParaLeitorDeTela()
        {
            var janela = new JanelaConexaoBanco(TipoBanco.SQLite);
            janela.Show();

            Acessibilidade.Anunciar(janela, "mensagem-de-teste-leitor-de-tela", forcar: true);

            var anunciador = janela.Encontrar<TextBlock>("LblAnuncioLeitorTela");
            await TesteUtil.AguardarAsync(() => anunciador.Text == "mensagem-de-teste-leitor-de-tela");

            Assert.Equal("mensagem-de-teste-leitor-de-tela", anunciador.Text);
        }
    }
}
