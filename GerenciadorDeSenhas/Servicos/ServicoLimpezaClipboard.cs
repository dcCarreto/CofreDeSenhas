namespace GerenciadorDeSenhas.Servicos
{
    public interface IAreaTransferencia
    {
        Task DefinirTextoAsync(string texto);
        Task<string?> ObterTextoAsync();
    }

    public static class ServicoLimpezaClipboard
    {
        public static async Task ProgramarLimpezaAsync(IAreaTransferencia areaTransferencia, string textoCopiado,
            int segundos, Func<int, Task>? aguardar = null)
        {
            if (areaTransferencia == null)
                throw new ArgumentNullException(nameof(areaTransferencia));

            if (segundos <= 0 || string.IsNullOrEmpty(textoCopiado))
                return;

            aguardar ??= s => Task.Delay(TimeSpan.FromSeconds(s));
            await aguardar(segundos);

            var atual = await areaTransferencia.ObterTextoAsync();
            if (atual == textoCopiado)
                await areaTransferencia.DefinirTextoAsync(string.Empty);
        }
    }
}
