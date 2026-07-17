using GerenciadorDeSenhas.Servicos;

namespace GerenciadorDeSenhas.Testes;

internal sealed class AreaTransferenciaEmMemoria : IAreaTransferencia
{
    public string? Texto { get; private set; }

    public Task DefinirTextoAsync(string texto)
    {
        Texto = texto;
        return Task.CompletedTask;
    }

    public Task<string?> ObterTextoAsync() => Task.FromResult(Texto);
}
