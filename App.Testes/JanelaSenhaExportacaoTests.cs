using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;

namespace App.Testes
{
    public class JanelaSenhaExportacaoTests
    {
        [AvaloniaFact]
        public async Task Abrir_ComFiltroAtivo_MostraCheckboxDeExportacaoSeletiva()
        {
            var janela = new JanelaSenhaExportacao(modoExportar: true, totalGeral: 10, totalFiltrado: 3);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.True(janela.Encontrar<CheckBox>("ChkSomenteFiltrados").IsVisible);
        }

        [AvaloniaFact]
        public async Task Abrir_SemFiltroAtivo_EscondeCheckboxDeExportacaoSeletiva()
        {
            var janela = new JanelaSenhaExportacao(modoExportar: true, totalGeral: 10, totalFiltrado: 10);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.False(janela.Encontrar<CheckBox>("ChkSomenteFiltrados").IsVisible);
        }

        [AvaloniaFact]
        public async Task Confirmar_ComCheckboxMarcado_RetornaExportarSomenteFiltradosVerdadeiro()
        {
            var janela = new JanelaSenhaExportacao(modoExportar: true, totalGeral: 10, totalFiltrado: 3);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.Encontrar<CheckBox>("ChkSomenteFiltrados").IsChecked = true;
            janela.Encontrar<TextBox>("TxtSenha").Text = "SenhaForte123!";
            janela.Encontrar<TextBox>("TxtConfirmar").Text = "SenhaForte123!";
            janela.Encontrar<Button>("BtnPrincipal").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.True(janela.ExportarSomenteFiltrados);
            Assert.Equal("SenhaForte123!", janela.SenhaInformada);
        }
    }
}
