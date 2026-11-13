#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace CofreDeSenhas
{
    // Diz se o app está rodando como pacote MSIX — na prática, a versão instalada pela
    // Microsoft Store. Nesse caso quem atualiza é a Loja: a política dela proíbe app
    // empacotado se atualizar por fora, e a pasta de instalação (WindowsApps) é somente
    // leitura, então o atualizador embutido nem conseguiria trocar o executável.
    internal static class PacoteLoja
    {
        public static bool Empacotado { get; } = Detectar();

        private static bool Detectar()
        {
#if WINDOWS
            try
            {
                // Sem pacote a chamada devolve APPMODEL_ERROR_NO_PACKAGE; com pacote,
                // ERROR_INSUFFICIENT_BUFFER (o buffer de tamanho zero é proposital).
                var tamanho = 0;
                return GetCurrentPackageFullName(ref tamanho, null) != AppModelErrorNoPackage;
            }
            catch
            {
                // Windows sem a API de pacotes (anterior ao 8) — não há como estar empacotado.
                return false;
            }
#else
            return false;
#endif
        }

#if WINDOWS
        private const int AppModelErrorNoPackage = 15700;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, char[]? packageFullName);
#endif
    }
}
