using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;

namespace App.Testes
{
    public class JanelaConfirmarSenhaMestraTests
    {
        [AvaloniaFact]
        public void Construtor_SemTituloExplicito_UsaTituloTraduzidoEmVezDeFicarPresoEmPortugues()
        {
            var janela = new JanelaConfirmarSenhaMestra();

            Assert.Equal(Idioma.Texto("Qr.RegenerateTitle"), janela.Title);
        }

        [AvaloniaFact]
        public void Construtor_ComTituloExplicito_UsaOTituloRecebido()
        {
            var janela = new JanelaConfirmarSenhaMestra(titulo: "Título Customizado");

            Assert.Equal("Título Customizado", janela.Title);
        }

        [AvaloniaFact]
        public void Confirmar_ComValidadorAprovando_DefineSenhaConfirmadaEFechaComSucesso()
        {
            var janela = new JanelaConfirmarSenhaMestra(validador: senha => senha == "SenhaCerta@123");
            janela.Show();

            var fechouComSucesso = false;
            janela.Closed += (s, e) => fechouComSucesso = true;

            janela.Encontrar<TextBox>("TxtSenha").Text = "SenhaCerta@123";
            janela.Encontrar<Button>("BtnConfirmar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal("SenhaCerta@123", janela.SenhaConfirmada);
            Assert.True(fechouComSucesso);
        }

        [AvaloniaFact]
        public void Confirmar_ComValidadorReprovando_MostraErroENaoFecha()
        {
            var janela = new JanelaConfirmarSenhaMestra(validador: senha => senha == "SenhaCerta@123");
            janela.Show();

            var fechou = false;
            janela.Closed += (s, e) => fechou = true;

            janela.Encontrar<TextBox>("TxtSenha").Text = "SenhaErrada";
            janela.Encontrar<Button>("BtnConfirmar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var lblErro = janela.Encontrar<TextBlock>("LblErro");
            Assert.Equal(Idioma.Texto("Qr.ErrorMasterIncorrect"), lblErro.Text);
            Assert.Equal("", janela.SenhaConfirmada);
            Assert.False(fechou);
        }

        [AvaloniaFact]
        public void Confirmar_ComCampoVazio_MostraErroDeCampoObrigatorioSemChamarOValidador()
        {
            var validadorChamado = false;
            var janela = new JanelaConfirmarSenhaMestra(validador: senha => { validadorChamado = true; return true; });
            janela.Show();

            janela.Encontrar<Button>("BtnConfirmar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var lblErro = janela.Encontrar<TextBlock>("LblErro");
            Assert.Equal(Idioma.Texto("Qr.ErrorMasterRequired"), lblErro.Text);
            Assert.False(validadorChamado);
        }
    }
}
