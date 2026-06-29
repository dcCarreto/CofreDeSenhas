using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace GerenciadorDeSenhas.Testes;

internal sealed class PersistenciaEmMemoria : IPersistenciaLocal
{
    private List<Senha> _dados = new();

    public Task SalvarSenhasAsync(List<Senha> senhas, byte[] chave)
    {
        _dados = senhas.ToList();
        return Task.CompletedTask;
    }

    public Task<List<Senha>> CarregarSenhasAsync(byte[] chave) => Task.FromResult(_dados.ToList());

    public Task BackupAutomaticoAsync(List<Senha> senhas, byte[] chave) => Task.CompletedTask;

    public bool ValidarIntegridade() => true;
}
