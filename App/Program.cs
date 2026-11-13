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
                if (!primeiraInstancia && !EsperarInstanciaAnteriorSair(args))
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

        // A atualização do portátil/AppImage abre a versão nova enquanto a antiga ainda
        // está fechando. Sem esperar, a nova via o mutex ocupado, só "ativava" a antiga
        // e saía — e o app sumia quando a antiga terminasse de fechar.
        private static bool EsperarInstanciaAnteriorSair(string[] args)
        {
            if (!args.Contains(ServicoAtualizacao.ArgumentoAposAtualizacao))
                return false;

            try
            {
                return _mutexInstancia!.WaitOne(TimeSpan.FromSeconds(30));
            }
            catch (AbandonedMutexException)
            {
                // A anterior saiu sem liberar (encerrada à força): o mutex é nosso agora.
                return true;
            }
        }

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

            // Downloads de atualização que sobraram: baixada e nunca instalada (o app
            // fechou sem o cofre ser bloqueado — desligamento do PC, por exemplo) ou o
            // instalador que acabou de rodar. Cada um tem ~70 MB e o próximo uso baixa
            // de novo de qualquer forma. Um instalador ainda em execução falha ao ser
            // apagado e fica para a próxima abertura.
            try
            {
                foreach (var pasta in Directory.EnumerateDirectories(Path.GetTempPath(), "CofreDeSenhas-update-*"))
                {
                    try { Directory.Delete(pasta, recursive: true); }
                    catch { }
                }
            }
            catch { }
        }
    }
}
