using System.Diagnostics;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace CofreDeSenhas
{
    // Como o app está rodando no Windows: instalado pelo Setup (direto ou via winget,
    // que usa o mesmo Setup) ou como executável solto — o antigo "portátil",
    // descontinuado. O executável é o mesmo nos dois casos (arquivo único); o que
    // diferencia é o desinstalador que o Inno Setup deixa ao lado dele.
    //
    // Quem ainda roda o executável solto passa para a versão instalada na próxima
    // atualização (ServicoAtualizacao baixa o Setup e instala só para o usuário atual).
    // O arquivo solto continua onde estava — talvez com um atalho ou fixado na barra de
    // tarefas —, e a partir daí só abre a versão instalada (RedirecionarParaInstalado).
    internal static class InstalacaoWindows
    {
        // AppId do cofre-de-senhas.iss; o Inno grava a desinstalação em "<AppId>_is1".
        private const string ChaveDesinstalacao =
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{B4E1F5A0-6C3D-4E8A-9F2B-7D3C1A5E8B90}_is1";

        private const string NomeExecutavel = "CofreDeSenhas.exe";

        public static bool InstaladoViaSetup =>
            File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));

        // Executável solto numa máquina que já tem o app instalado: abre o instalado,
        // repassando os argumentos, e devolve true para quem chamou sair sem abrir nada.
        // Fora do Windows, na Loja, no app instalado e em execuções isoladas (Debug,
        // COFRE_BASE) não faz nada.
        public static bool RedirecionarParaInstalado(string[] args)
        {
            if (!OperatingSystem.IsWindows() || GerenciadorDeSenhas.AmbienteCofre.Isolado ||
                PacoteLoja.Empacotado || InstaladoViaSetup)
                return false;

            try
            {
                var instalado = ExecutavelInstalado();
                if (instalado == null || MesmoArquivo(instalado, Environment.ProcessPath))
                    return false;

                var inicio = new ProcessStartInfo(instalado) { UseShellExecute = false };
                foreach (var arg in args)
                    inicio.ArgumentList.Add(arg);
                Process.Start(inicio);
                return true;
            }
            catch
            {
                // Registro ilegível, arquivo sumiu entre a checagem e o início etc.:
                // melhor abrir o executável solto do que não abrir nada.
                return false;
            }
        }

        // Caminho do CofreDeSenhas.exe instalado pelo Setup, se houver: primeiro a
        // instalação só para o usuário atual, depois a para todos os usuários.
        private static string? ExecutavelInstalado()
        {
#if WINDOWS
            foreach (var (hive, visao) in new[]
            {
                (RegistryHive.CurrentUser, RegistryView.Default),
                (RegistryHive.LocalMachine, RegistryView.Registry64),
            })
            {
                using var raiz = RegistryKey.OpenBaseKey(hive, visao);
                using var chave = raiz.OpenSubKey(ChaveDesinstalacao);
                if (chave?.GetValue("InstallLocation") is string pasta && !string.IsNullOrWhiteSpace(pasta))
                {
                    var exe = Path.Combine(pasta, NomeExecutavel);
                    if (File.Exists(exe))
                        return exe;
                }
            }
#endif
            return null;
        }

        private static bool MesmoArquivo(string a, string? b) =>
            b != null && string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    }
}
