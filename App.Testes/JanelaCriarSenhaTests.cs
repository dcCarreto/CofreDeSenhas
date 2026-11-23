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
        private static (IServicoSenha servico, byte[] chave) CriarServico(Task? liberarGravacao = null)
        {
            var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
            var criptografia = new ServicoCriptografia(chave);
            IPersistenciaLocal persistencia = new PersistenciaLocal(criptografia, TesteUtil.CriarPastaTemporaria());
            if (liberarGravacao != null)
                persistencia = new PersistenciaSegurada(persistencia, liberarGravacao);
            var repositorio = new RepositorioSenha(persistencia, chave);
            return (new ServicoSenha(repositorio, criptografia), chave);
        }

        // Segura a gravação até o teste liberar. Sem isso, quando a escrita terminava de forma
        // síncrona (comum no Linux do CI), o salvamento inteiro (até o finally que reabilita o
        // botão) rodava dentro do RaiseEvent e o teste nunca via o botão desabilitado.
        private sealed class PersistenciaSegurada(IPersistenciaLocal real, Task liberar) : IPersistenciaLocal
        {
            public async Task SalvarSenhasAsync(List<Senha> senhas, byte[] chave)
            {
                await liberar;
                await real.SalvarSenhasAsync(senhas, chave);
            }

            public Task<List<Senha>> CarregarSenhasAsync(byte[] chave) => real.CarregarSenhasAsync(chave);
            public Task BackupAutomaticoAsync(List<Senha> senhas, byte[] chave, int quantidadeMaxima = PersistenciaLocal.QuantidadeMaximaBackupsPadrao)
                => real.BackupAutomaticoAsync(senhas, chave, quantidadeMaxima);
            public List<InfoBackup> ListarBackups() => real.ListarBackups();
            public Task<List<Senha>> CarregarBackupAsync(string caminhoArquivo) => real.CarregarBackupAsync(caminhoArquivo);
            public bool ValidarIntegridade() => real.ValidarIntegridade();
            public Task ApagarTudoAsync() => real.ApagarTudoAsync();
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
            var liberar = new TaskCompletionSource();
            var (servico, _) = CriarServico(liberar.Task);
            var janela = new JanelaCriarSenha(servico);
            janela.Show();

            janela.Encontrar<TextBox>("TxtNomeServico").Text = "Servico de Teste";
            janela.Encontrar<TextBox>("TxtUsuario").Text = "usuario.teste";
            janela.Encontrar<TextBox>("TxtSenha").Text = "SenhaCriada123!";

            var botao = janela.BotaoPorTexto(Idioma.Texto("Common.Save"));
            botao.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.False(botao.IsEnabled);

            liberar.SetResult();
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
