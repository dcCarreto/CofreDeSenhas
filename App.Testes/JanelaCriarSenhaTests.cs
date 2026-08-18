using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    public class JanelaCriarSenhaTests
    {
        private static (IServicoSenha servico, byte[] chave) CriarServico()
        {
            var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
            var criptografia = new ServicoCriptografia(chave);
            var persistencia = new PersistenciaLocal(criptografia, TesteUtil.CriarPastaTemporaria());
            var repositorio = new RepositorioSenha(persistencia, chave);
            return (new ServicoSenha(repositorio, criptografia), chave);
        }

        [AvaloniaFact]
        public async Task Salvar_PersisteNovaCredencial()
        {
            var (servico, _) = CriarServico();
            var janela = new JanelaCriarSenha(servico);
            janela.Show();

            janela.Encontrar<TextBox>("TxtNomeServico").Text = "Servico de Teste";
            janela.Encontrar<TextBox>("TxtUsuario").Text = "usuario.teste";
            janela.Encontrar<TextBox>("TxtSenha").Text = "SenhaCriada123!";

            janela.BotaoPorTexto(Idioma.Texto("Common.Save")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            List<Senha> lista = new();
            await TesteUtil.AguardarAsync(() =>
            {
                lista = servico.ListarTodosAsync().GetAwaiter().GetResult();
                return lista.Count > 0;
            });

            var criada = Assert.Single(lista);
            Assert.Equal("Servico de Teste", criada.NomeServico);
            Assert.Equal("usuario.teste", criada.Usuario);
        }

        [AvaloniaFact]
        public async Task Salvar_DesabilitaOBotaoEnquantoSalvaEReabilitaDepois_ImpedindoDuploClique()
        {
            var (servico, _) = CriarServico();
            var janela = new JanelaCriarSenha(servico);
            janela.Show();

            janela.Encontrar<TextBox>("TxtNomeServico").Text = "Servico de Teste";
            janela.Encontrar<TextBox>("TxtUsuario").Text = "usuario.teste";
            janela.Encontrar<TextBox>("TxtSenha").Text = "SenhaCriada123!";

            var botao = janela.BotaoPorTexto(Idioma.Texto("Common.Save"));
            botao.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.False(botao.IsEnabled);

            await TesteUtil.AguardarAsync(() => botao.IsEnabled);

            Assert.True(botao.IsEnabled);
            var lista = await servico.ListarTodosAsync();
            Assert.Single(lista);
        }

        [AvaloniaFact]
        public async Task Salvar_ComCamposObrigatoriosVazios_NaoPersisteNada()
        {
            var (servico, _) = CriarServico();
            var janela = new JanelaCriarSenha(servico);
            janela.Show();

            janela.BotaoPorTexto(Idioma.Texto("Common.Save")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var lista = await servico.ListarTodosAsync();
            Assert.Empty(lista);
        }
    }
}
