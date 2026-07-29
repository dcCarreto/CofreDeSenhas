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
    public class JanelaBackupTests
    {
        [AvaloniaFact]
        public async Task BackupAgora_CriaUmBackupEAtualizaALista()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var chave = new AutenticacaoMestra(pasta).CriarSenhaMestra("SenhaDeTeste123!");
            var criptografia = new ServicoCriptografia(chave);
            var persistencia = new PersistenciaLocal(criptografia, pasta);
            var repositorio = new RepositorioSenha(persistencia, chave);
            var servico = new ServicoSenha(repositorio, criptografia);
            await servico.CriarSenhaAsync("Servico", "usuario", "SenhaForte123!", Categoria.Personal);
            await servico.PersistirAsync();

            Assert.Empty(persistencia.ListarBackups());

            var janela = new JanelaBackup(persistencia, () => servico.ListarTodosAsync(), chave, permiteRestaurar: true);
            janela.Show();

            var botao = janela.BotaoPorNomeAutomacao(Idioma.Texto("Backup.Now"));
            botao.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() => persistencia.ListarBackups().Count > 0);

            Assert.Single(persistencia.ListarBackups());
        }
    }
}
