using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas;
using CofreDeSenhas.Controles;

namespace App.Testes
{
    // Coleção Preferencias: o gerador lê e grava Preferencias.Gerador.
    [Collection("Preferencias")]
    public class GeradorSenhaTests
    {
        [AvaloniaFact]
        public async Task OpcoesEscolhidas_SaoGravadasEVoltamNumGeradorNovo()
        {
            var original = Preferencias.Gerador;
            try
            {
                Preferencias.Gerador = null;
                var gerador = new GeradorSenha();
                var janela = new Window { Content = gerador, Width = 500, Height = 600 };
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 3);

                gerador.FindControl<CustomSlider>("SliderTamanho")!.Value = 40;
                gerador.FindControl<CustomSlider>("SliderQuantidade")!.Value = 4;
                gerador.FindControl<CustomToggle>("ToggleEspeciais")!.Checked = true;
                gerador.FindControl<CustomToggle>("ToggleNumeros")!.Checked = false;
                gerador.FindControl<ComboBox>("CmbModoGerador")!.SelectedIndex = 1;
                gerador.FindControl<CustomSlider>("SliderPalavras")!.Value = 6;
                gerador.FindControl<ComboBox>("CmbSeparadorFrase")!.SelectedIndex = 3;

                // Fechar a janela antes do debounce ainda grava (Detached).
                janela.Close();
                await TesteUtil.AguardarAsync(() => Preferencias.Gerador != null);

                var opcoes = Assert.IsType<OpcoesGerador>(Preferencias.Gerador);
                Assert.Equal((40, 4, 1, 6, 3), (opcoes.Tamanho, opcoes.Quantidade, opcoes.Modo, opcoes.Palavras, opcoes.Separador));
                Assert.True(opcoes.Especiais);
                Assert.False(opcoes.Numeros);

                var novo = new GeradorSenha();
                Assert.Equal(40, novo.FindControl<CustomSlider>("SliderTamanho")!.Value);
                Assert.Equal(4, novo.FindControl<CustomSlider>("SliderQuantidade")!.Value);
                Assert.Equal(6, novo.FindControl<CustomSlider>("SliderPalavras")!.Value);
                Assert.True(novo.FindControl<CustomToggle>("ToggleEspeciais")!.Checked);
                Assert.False(novo.FindControl<CustomToggle>("ToggleNumeros")!.Checked);
                Assert.Equal(1, novo.FindControl<ComboBox>("CmbModoGerador")!.SelectedIndex);
                Assert.Equal(3, novo.FindControl<ComboBox>("CmbSeparadorFrase")!.SelectedIndex);
                Assert.True(novo.FindControl<StackPanel>("PainelFraseSenha")!.IsVisible);
            }
            finally
            {
                Preferencias.Gerador = original;
                Preferencias.Salvar();
            }
        }

        [AvaloniaFact]
        public void OpcoesSalvasForaDoIntervalo_SaoLimitadasAoQueOControleAceita()
        {
            var original = Preferencias.Gerador;
            try
            {
                Preferencias.Gerador = new OpcoesGerador { Modo = 9, Separador = -1, Tamanho = 999, Quantidade = 0 };
                var gerador = new GeradorSenha();

                Assert.Equal(1, gerador.FindControl<ComboBox>("CmbModoGerador")!.SelectedIndex);
                Assert.Equal(0, gerador.FindControl<ComboBox>("CmbSeparadorFrase")!.SelectedIndex);
                Assert.Equal(64, gerador.FindControl<CustomSlider>("SliderTamanho")!.Value);
                Assert.Equal(1, gerador.FindControl<CustomSlider>("SliderQuantidade")!.Value);
            }
            finally
            {
                Preferencias.Gerador = original;
            }
        }

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
