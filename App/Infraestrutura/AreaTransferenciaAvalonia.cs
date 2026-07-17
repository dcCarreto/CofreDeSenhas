using Avalonia.Input.Platform;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas
{
    internal sealed class AreaTransferenciaAvalonia : IAreaTransferencia
    {
        private readonly IClipboard _clipboard;

        public AreaTransferenciaAvalonia(IClipboard clipboard) => _clipboard = clipboard;

        public Task DefinirTextoAsync(string texto) => _clipboard.SetTextAsync(texto);

        public Task<string?> ObterTextoAsync() => _clipboard.TryGetTextAsync();
    }
}
