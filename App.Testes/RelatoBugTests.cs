using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;

namespace App.Testes
{
    public class RelatoBugTests
    {
        [Fact]
        public void MontarIssue_UsaAPrimeiraLinhaComoTituloEColocaRelatoELogNoCorpo()
        {
            var descricao = "O app fecha ao exportar " + new string('x', 100) + "\nsegunda linha";

            var (titulo, corpo) = RelatoBug.MontarIssue(descricao, "LOG-AQUI");

            Assert.StartsWith("[Relato] O app fecha ao exportar", titulo);
            Assert.EndsWith("…", titulo);
            Assert.DoesNotContain("segunda linha", titulo);
            Assert.Contains("segunda linha", corpo);
            Assert.Contains("````text\nLOG-AQUI\n````", corpo.Replace("\r\n", "\n"));
        }

        [AvaloniaFact]
        public async Task Enviar_SemDescricao_MostraErroENaoFecha()
        {
            var janela = new JanelaRelatarBug();
            janela.Show();

            janela.Encontrar<Button>("BtnEnviar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.Equal(Idioma.Texto("BugReport.Required"), janela.Encontrar<TextBlock>("LblErro").Text);
            Assert.True(janela.IsVisible);
        }

        [AvaloniaFact]
        public async Task Enviar_SemTokenNoBuild_AvisaQueNaoEstaDisponivel()
        {
            if (RelatoBug.Disponivel)
                return; // build com COFRE_TOKEN_RELATO: não abrir issue de verdade num teste

            var janela = new JanelaRelatarBug();
            janela.Show();
            janela.Encontrar<TextBox>("TxtDescricao").Text = "algo deu errado";

            janela.Encontrar<Button>("BtnEnviar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var erro = janela.Encontrar<TextBlock>("LblErro");
            await TesteUtil.AguardarAsync(() => !string.IsNullOrEmpty(erro.Text));

            Assert.Equal(Idioma.Formatar("BugReport.Error", Idioma.Texto("BugReport.Unavailable")), erro.Text);
        }

        [AvaloniaFact]
        public void Abrir_MostraORelatorioQueSeraEnviado()
        {
            var janela = new JanelaRelatarBug();
            janela.Show();

            Assert.Contains("== Ambiente ==", janela.Encontrar<TextBox>("TxtRelatorio").Text);
        }
    }
}
