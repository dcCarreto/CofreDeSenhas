using System.Runtime.InteropServices;

namespace GerenciadorDeSenhas.Servicos
{
    // Fixa na RAM física as páginas que guardam a chave mestra (VirtualLock no
    // Windows, mlock no Linux e no macOS), para o segredo não ser gravado no
    // arquivo de swap nem no de hibernação. É melhor esforço: se a chamada falhar
    // (cota de memória travável, permissão), o cofre segue funcionando sem essa
    // proteção — o modelo continua o descrito no THREAT_MODEL.md.
    public static class MemoriaTravada
    {
        public static bool Travar(IntPtr endereco, nuint tamanho)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                    return VirtualLock(endereco, tamanho);
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                    return mlock(endereco, tamanho) == 0;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            return false;
        }

        public static void Destravar(IntPtr endereco, nuint tamanho)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                    VirtualUnlock(endereco, tamanho);
                else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                    munlock(endereco, tamanho);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualLock(IntPtr lpAddress, nuint dwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualUnlock(IntPtr lpAddress, nuint dwSize);

        [DllImport("libc", SetLastError = true)]
        private static extern int mlock(IntPtr addr, nuint len);

        [DllImport("libc", SetLastError = true)]
        private static extern int munlock(IntPtr addr, nuint len);
    }
}
