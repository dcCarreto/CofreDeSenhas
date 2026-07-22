using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public sealed record InfoBackup(string Caminho, DateTime DataUtc);

    public interface IPersistenciaLocal
    {
        Task SalvarSenhasAsync(List<Senha> senhas, byte[] chave);
        Task<List<Senha>> CarregarSenhasAsync(byte[] chave);
        Task BackupAutomaticoAsync(List<Senha> senhas, byte[] chave, int quantidadeMaxima = PersistenciaLocal.QuantidadeMaximaBackupsPadrao);
        List<InfoBackup> ListarBackups();
        Task<List<Senha>> CarregarBackupAsync(string caminhoArquivo);
        bool ValidarIntegridade();
        Task ApagarTudoAsync();
    }
}
