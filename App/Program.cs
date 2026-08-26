using System.Runtime.CompilerServices;
using Avalonia;

[assembly: InternalsVisibleTo("App.Testes")]
[assembly: InternalsVisibleTo("GeradorDeSenhas")]

namespace CofreDeSenhas
{
    internal static class Program
    {
        // Mesmo nome do AppMutex em cofre-de-senhas.iss: permite que o instalador
        // detecte e feche esta instância durante uma atualização silenciosa.
        private const string NomeMutexApp = "CofreDeSenhasApp";
        private static Mutex? _mutexInstancia;

        [STAThread]
        public static void Main(string[] args)
        {
            _mutexInstancia = new Mutex(false, NomeMutexApp);
            LimparAtualizacaoPendente();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont();

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
