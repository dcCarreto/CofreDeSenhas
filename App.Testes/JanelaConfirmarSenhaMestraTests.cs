using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    public class JanelaConfirmarSenhaMestraTests
    {
        private static AutenticacaoMestra CriarAuthIsolado() => new(TesteUtil.CriarPastaTemporaria());

        [AvaloniaFact]
        public void Construtor_SemTituloExplicito_UsaTituloTraduzidoEmVezDeFicarPresoEmPortugues()
        {
            var janela = new JanelaConfirmarSenhaMestra(auth: CriarAuthIsolado());

            Assert.Equal(Idioma.Texto("Qr.RegenerateTitle"), janela.Title);
        }

        [AvaloniaFact]
        public void Construtor_ComTituloExplicito_UsaOTituloRecebido()
        {
            var janela = new JanelaConfirmarSenhaMestra(titulo: "Título Customizado", auth: CriarAuthIsolado());

            Assert.Equal("Título Customizado", janela.Title);
        }

        [AvaloniaFact]
        public void Confirmar_ComValidadorAprovando_DefineSenhaConfirmadaEFechaComSucesso()
        {
            var janela = new JanelaConfirmarSenhaMestra(validador: senha => senha == "SenhaCerta@123", auth: CriarAuthIsolado());
            janela.Show();

            var fechouComSucesso = false;
            janela.Closed += (s, e) => fechouComSucesso = true;

            janela.Encontrar<TextBox>("TxtSenha").Text = "SenhaCerta@123";
            janela.Encontrar<Button>("BtnConfirmar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal("SenhaCerta@123", janela.SenhaConfirmada);
            Assert.True(fechouComSucesso);
        }

        [AvaloniaFact]
        public void Confirmar_ComValidadorReprovando_MostraErroComContagemDeTentativasENaoFecha()
        {
            var janela = new JanelaConfirmarSenhaMestra(validador: senha => senha == "SenhaCerta@123", auth: CriarAuthIsolado());
            janela.Show();

            var fechou = false;
            janela.Closed += (s, e) => fechou = true;

            janela.Encontrar<TextBox>("TxtSenha").Text = "SenhaErrada";
            janela.Encontrar<Button>("BtnConfirmar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var lblErro = janela.Encontrar<TextBlock>("LblErro");
            Assert.Equal(Idioma.Formatar("Login.Error.WrongPassword", 1), lblErro.Text);
            Assert.Equal("", janela.SenhaConfirmada);
            Assert.False(fechou);
        }

        [AvaloniaFact]
        public void Confirmar_ComCampoVazio_MostraErroDeCampoObrigatorioSemChamarOValidador()
        {
            var validadorChamado = false;
            var janela = new JanelaConfirmarSenhaMestra(validador: senha => { validadorChamado = true; return true; }, auth: CriarAuthIsolado());
            janela.Show();

            janela.Encontrar<Button>("BtnConfirmar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var lblErro = janela.Encontrar<TextBlock>("LblErro");
            Assert.Equal(Idioma.Texto("Qr.ErrorMasterRequired"), lblErro.Text);
            Assert.False(validadorChamado);
        }

        [AvaloniaFact]
        public async Task Confirmar_ComCincoSenhasErradas_BloqueiaEDesabilitaOBotao()
        {
            // Mesmo mecanismo de bloqueio da tela de login (ControleTentativasLogin) — sem
            // isto, este diálogo (reautenticação pra excluir/limpar cofre, regerar QR code,
            // ativar sincronização) deixava tentar a senha mestra sem limite nenhum.
            var janela = new JanelaConfirmarSenhaMestra(validador: senha => senha == "SenhaCerta@123", auth: CriarAuthIsolado());
            janela.Show();

            var btnConfirmar = janela.Encontrar<Button>("BtnConfirmar");
            var txtSenha = janela.Encontrar<TextBox>("TxtSenha");
            var lblErro = janela.Encontrar<TextBlock>("LblErro");

            for (var i = 0; i < ControleTentativasLogin.LimiteTentativas; i++)
            {
                await TesteUtil.AguardarAsync(() => btnConfirmar.IsEnabled);
                txtSenha.Text = "SenhaErrada";
                btnConfirmar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await TesteUtil.AguardarAsync(() => !string.IsNullOrEmpty(lblErro.Text));
            }

            Assert.False(btnConfirmar.IsEnabled);
            Assert.Equal(Idioma.Texto("Login.Error.TooManyAttempts"), lblErro.Text);
        }

        [AvaloniaFact]
        public async Task Construtor_RegistraAnunciadorParaLeitorDeTela()
        {
            // Sem RegistrarAnunciador, Acessibilidade.Anunciar (usado por
            // MostrarErroInline — ex.: "senha mestra incorreta", "muitas tentativas")
            // descartava a mensagem em silêncio: TryGetValue falhava e nada era lido
            // por um leitor de tela.
            var janela = new JanelaConfirmarSenhaMestra(auth: CriarAuthIsolado());
            janela.Show();

            Acessibilidade.Anunciar(janela, "mensagem-de-teste-leitor-de-tela", forcar: true);

            var anunciador = janela.Encontrar<TextBlock>("LblAnuncioLeitorTela");
            await TesteUtil.AguardarAsync(() => anunciador.Text == "mensagem-de-teste-leitor-de-tela");

            Assert.Equal("mensagem-de-teste-leitor-de-tela", anunciador.Text);
        }

        [AvaloniaFact]
        public async Task Confirmar_ComBloqueioDeUmaInstanciaAnterior_AbreJaBloqueado()
        {
            var auth = CriarAuthIsolado();
            var controle = new ControleTentativasLogin(auth.PastaApp);
            for (var i = 0; i < ControleTentativasLogin.LimiteTentativas; i++)
                controle.RegistrarFalha();

            var janela = new JanelaConfirmarSenhaMestra(auth: auth);
            janela.Show();

            var btnConfirmar = janela.Encontrar<Button>("BtnConfirmar");
            var lblErro = janela.Encontrar<TextBlock>("LblErro");
            await TesteUtil.AguardarAsync(() => !string.IsNullOrEmpty(lblErro.Text));

            Assert.False(btnConfirmar.IsEnabled);
            Assert.Equal(Idioma.Texto("Login.Error.TooManyAttempts"), lblErro.Text);
        }
    }
}
