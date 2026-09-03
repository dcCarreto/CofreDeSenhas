using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Threading;

[assembly: InternalsVisibleTo("App.Testes")]
[assembly: InternalsVisibleTo("GeradorDeSenhas")]

namespace CofreDeSenhas
{
    internal static class Program
    {
        // Mesmo nome do AppMutex em cofre-de-senhas.iss: permite que o instalador
        // detecte e feche esta instância durante uma atualização silenciosa.
        private const string NomeMutexApp = "CofreDeSenhasApp";
        private const string NomeEventoAtivacao = "CofreDeSenhasApp.Ativar";
        private static Mutex? _mutexInstancia;

        [STAThread]
        public static void Main(string[] args)
        {
            // Instância única só no app instalado; Debug/testes/verify (COFRE_BASE) rodam em paralelo.
            if (!GerenciadorDeSenhas.AmbienteCofre.Isolado)
            {
                _mutexInstancia = new Mutex(true, NomeMutexApp, out var primeiraInstancia);
                if (!primeiraInstancia)
                {
                    AtivarInstanciaExistente();
                    return;
                }
                EscutarPedidosDeAtivacao();
            }

            LimparAtualizacaoPendente();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont();

        private static void AtivarInstanciaExistente()
        {
            if (!OperatingSystem.IsWindows())
                return;

            try
            {
                if (EventWaitHandle.TryOpenExisting(NomeEventoAtivacao, out var evento))
                    using (evento)
                        evento.Set();
            }
            catch { }
        }

        private static void EscutarPedidosDeAtivacao()
        {
            if (!OperatingSystem.IsWindows())
                return;

            var evento = new EventWaitHandle(false, EventResetMode.AutoReset, NomeEventoAtivacao);
            new Thread(() =>
            {
                while (true)
                {
                    evento.WaitOne();
                    try { Dispatcher.UIThread.Post(App.TrazerJanelaParaFrente); } catch { }
                }
            })
            { IsBackground = true, Name = "AtivacaoInstancia" }.Start();
        }

        private static void LimparAtualizacaoPendente()
        {
            try
            {
                var exeAntigo = Environment.ProcessPath + ".old";
                if (File.Exists(exeAntigo))
                    File.Delete(exeAntigo);
            }
            catch { }
        }
    }
}
