using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace CofreDeSenhas
{
    internal static class AreaTransferenciaSegura
    {
        // Copia texto sensível do cofre marcando-o para ficar fora do Histórico da Área
        // de Transferência (Win+V) e do Cloud Clipboard do Windows. A limpeza automática
        // do cofre só apaga o conteúdo atual da área de transferência — o que já foi
        // parar no histórico ou na nuvem continua lá.
        public static async Task CopiarAsync(IClipboard clipboard, string texto)
        {
#if WINDOWS
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
                && CopiarSemHistoricoWindows(texto))
                return;
#endif
            await clipboard.SetTextAsync(texto);
        }

#if WINDOWS
        private static bool CopiarSemHistoricoWindows(string texto)
        {
            try
            {
                var pacote = new Windows.ApplicationModel.DataTransfer.DataPackage();
                pacote.SetText(texto);

                var opcoes = new Windows.ApplicationModel.DataTransfer.ClipboardContentOptions
                {
                    IsAllowedInHistory = false,
                    IsRoamable = false
                };

                return Windows.ApplicationModel.DataTransfer.Clipboard.SetContentWithOptions(pacote, opcoes);
            }
            catch
            {
                return false;
            }
        }
#endif
    }
}
