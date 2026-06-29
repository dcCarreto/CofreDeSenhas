using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public interface IPersistenciaLocal
    {
        Task SalvarSenhasAsync(List<Senha> senhas, byte[] chave);
        Task<List<Senha>> CarregarSenhasAsync(byte[] chave);
        Task BackupAutomaticoAsync(List<Senha> senhas, byte[] chave);
        bool ValidarIntegridade();
    }
}
