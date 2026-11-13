#if WINDOWS
using System.Runtime.InteropServices;
using Windows.Services.Store;
#endif

namespace CofreDeSenhas
{
    // Atualização pela Microsoft Store, só na versão empacotada. A Loja já atualiza os
    // apps sozinha, mas no ritmo dela e de preferência com o app fechado — e um cofre
    // costuma ficar aberto na bandeja o dia todo. Aqui o app baixa a atualização assim
    // que a Loja a oferece e pede a instalação no próximo bloqueio do cofre.
    //
    // Respeita a escolha do usuário: se ele desligou "Atualizações de aplicativos" na
    // Loja, ou está numa rede limitada, CanSilentlyDownloadStorePackageUpdates é false
    // e nada acontece por aqui.
    internal static class AtualizacaoLoja
    {
#if WINDOWS
        private static StoreContext? _contexto;
        private static IReadOnlyList<StorePackageUpdate>? _pendentes;
#endif

        // true = há atualização baixada, pronta para InstalarAsync.
        public static async Task<bool> BaixarAsync()
        {
#if WINDOWS
            _contexto ??= StoreContext.GetDefault();
            var atualizacoes = await _contexto.GetAppAndOptionalStorePackageUpdatesAsync();
            if (atualizacoes.Count == 0 || !_contexto.CanSilentlyDownloadStorePackageUpdates)
                return false;

            var resultado = await _contexto.TrySilentDownloadStorePackageUpdatesAsync(atualizacoes);
            if (resultado.OverallState != StorePackageUpdateState.Completed)
                return false;

            _pendentes = atualizacoes;
            return true;
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        // O Windows fecha o app para instalar; com o registro de reinício, ele o reabre
        // ao terminar. Se este método retornar, a instalação não aconteceu (ou ainda
        // não chegou a fechar o app).
        public static async Task<bool> InstalarAsync()
        {
#if WINDOWS
            if (_contexto == null || _pendentes == null)
                return false;

            RegisterApplicationRestart(null, 0);
            var resultado = await _contexto.TrySilentDownloadAndInstallStorePackageUpdatesAsync(_pendentes);
            _pendentes = null;
            return resultado.OverallState == StorePackageUpdateState.Completed;
#else
            await Task.CompletedTask;
            return false;
#endif
        }

#if WINDOWS
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegisterApplicationRestart(string? pwzCommandline, int dwFlags);
#endif
    }
}
