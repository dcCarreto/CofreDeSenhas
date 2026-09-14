using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace CofreDeSenhas.Cli
{
    internal sealed class SemClipboardException : Exception
    {
        public SemClipboardException(string mensagem) : base(mensagem) { }
    }

    // Copia e limpa a área de transferência. No Windows usa a API do clipboard
    // (CF_UNICODETEXT), para preservar o texto exatamente — uma senha com acento
    // ou caractere fora da página de código não pode chegar mutilada. No Linux e
    // no macOS delega ao utilitário do sistema (wl-copy / xclip / xsel / pbcopy).
    internal static class AreaTransferencia
    {
        public static void Copiar(string texto) => Definir(texto);

        public static void Limpar() => Definir(string.Empty);

        private static void Definir(string conteudo)
        {
            if (OperatingSystem.IsWindows())
                DefinirWindows(conteudo);
            else
                DefinirViaProcesso(conteudo);
        }

        // ---------- Windows ----------

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        [DllImport("user32.dll", SetLastError = true)] private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseClipboard();
        [DllImport("user32.dll", SetLastError = true)] private static extern bool EmptyClipboard();
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalFree(IntPtr hMem);

        [SupportedOSPlatform("windows")]
        private static void DefinirWindows(string conteudo)
        {
            var aberto = false;
            for (var tentativa = 0; tentativa < 10 && !aberto; tentativa++)
            {
                aberto = OpenClipboard(IntPtr.Zero);
                if (!aberto) Thread.Sleep(50);
            }
            if (!aberto)
                throw new SemClipboardException("não foi possível abrir a área de transferência do Windows.");

            try
            {
                if (!EmptyClipboard())
                    throw new SemClipboardException("não foi possível limpar a área de transferência.");

                if (conteudo.Length == 0)
                    return;

                var hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)((conteudo.Length + 1) * 2));
                if (hMem == IntPtr.Zero)
                    throw new SemClipboardException("sem memória para a área de transferência.");

                var ponteiro = GlobalLock(hMem);
                if (ponteiro == IntPtr.Zero)
                {
                    GlobalFree(hMem);
                    throw new SemClipboardException("não foi possível bloquear a memória da área de transferência.");
                }

                try
                {
                    var chars = conteudo.ToCharArray();
                    Marshal.Copy(chars, 0, ponteiro, chars.Length);
                    Marshal.WriteInt16(ponteiro, chars.Length * 2, 0);
                }
                finally
                {
                    GlobalUnlock(hMem);
                }

                if (SetClipboardData(CF_UNICODETEXT, hMem) == IntPtr.Zero)
                {
                    GlobalFree(hMem);
                    throw new SemClipboardException("SetClipboardData falhou.");
                }
                // Deu certo: o sistema passa a ser dono de hMem — não liberar.
            }
            finally
            {
                CloseClipboard();
            }
        }

        // ---------- Linux / macOS ----------

        private static void DefinirViaProcesso(string conteudo)
        {
            var (arquivo, argumentos) = ComandoDaPlataforma();
            var psi = new ProcessStartInfo(arquivo)
            {
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            };
            foreach (var a in argumentos)
                psi.ArgumentList.Add(a);

            Process processo;
            try
            {
                processo = Process.Start(psi) ?? throw new InvalidOperationException("processo nulo");
            }
            catch (Exception ex)
            {
                throw new SemClipboardException($"não foi possível acionar \"{arquivo}\" ({ex.Message}).");
            }

            using (processo)
            {
                processo.StandardInput.Write(conteudo);
                processo.StandardInput.Close();

                if (!processo.WaitForExit(3000))
                {
                    try { processo.Kill(entireProcessTree: true); } catch { }
                    throw new SemClipboardException($"\"{arquivo}\" não respondeu.");
                }
                if (processo.ExitCode != 0)
                    throw new SemClipboardException(
                        $"\"{arquivo}\" falhou (código {processo.ExitCode}). Há um servidor gráfico (Wayland/X11) ativo?");
            }
        }

        private static (string Arquivo, string[] Argumentos) ComandoDaPlataforma()
        {
            if (OperatingSystem.IsMacOS())
                return ("pbcopy", Array.Empty<string>());

            foreach (var (nome, args) in new[]
            {
                ("wl-copy", Array.Empty<string>()),
                ("xclip", new[] { "-selection", "clipboard" }),
                ("xsel", new[] { "--clipboard", "--input" }),
            })
            {
                if (ExisteNoPath(nome))
                    return (nome, args);
            }

            throw new SemClipboardException(
                "nenhum utilitário de área de transferência encontrado. Instale wl-clipboard (Wayland) ou xclip / xsel (X11).");
        }

        private static bool ExisteNoPath(string executavel)
        {
            var caminho = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(caminho))
                return false;

            foreach (var dir in caminho.Split(Path.PathSeparator))
            {
                if (!string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir, executavel)))
                    return true;
            }
            return false;
        }
    }
}
