using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas
{
    public partial class App : Application
    {
        private TrayIcon? _bandeja;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            Preferencias.Carregar();
            AplicarTema(Preferencias.ModoEscuro);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new JanelaLogin(new AutenticacaoMestra(), chave => AbrirCofre(desktop, chave));
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static void AplicarTema(bool escuro)
        {
            Tema.DefinirModo(escuro);
            if (Current != null)
                Current.RequestedThemeVariant = escuro ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        private void AbrirCofre(IClassicDesktopStyleApplicationLifetime desktop, byte[] chave)
        {
            var criptografia = new ServicoCriptografia(chave);
            var persistencia = new PersistenciaLocal(criptografia);
            var repositorio = new RepositorioSenha(persistencia, chave);
            var servicoSenha = new ServicoSenha(repositorio, criptografia);

            var principal = new JanelaPrincipal(servicoSenha, criptografia, repositorio,
                () => Bloquear(desktop));
            var login = desktop.MainWindow;
            desktop.MainWindow = principal;
            principal.Show();
            login?.Close();

            ConfigurarBandeja(desktop);
        }

        private void Bloquear(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is not JanelaPrincipal cofre)
                return;

            foreach (var janela in desktop.Windows.ToArray())
                if (janela != cofre)
                    janela.Close();

            var login = new JanelaLogin(new AutenticacaoMestra(), chave => AbrirCofre(desktop, chave));
            desktop.MainWindow = login;
            login.Show();
            cofre.Close();
        }

        private void ConfigurarBandeja(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_bandeja != null)
                return;

            try
            {
                void Restaurar()
                {
                    if (desktop.MainWindow is not { } janela)
                        return;
                    janela.Show();
                    janela.WindowState = WindowState.Normal;
                    janela.Activate();
                }

                var itemAbrir = new NativeMenuItem("Abrir cofre");
                itemAbrir.Click += (s, e) => Restaurar();
                var itemSair = new NativeMenuItem("Sair");
                itemSair.Click += (s, e) => desktop.Shutdown();

                var menu = new NativeMenu();
                menu.Add(itemAbrir);
                menu.Add(new NativeMenuItemSeparator());
                menu.Add(itemSair);

                _bandeja = new TrayIcon
                {
                    Icon = Recursos.IconeApp(),
                    ToolTipText = "Cofre de Senhas",
                    Menu = menu
                };
                _bandeja.Clicked += (s, e) => Restaurar();
                _bandeja.IsVisible = true;
            }
            catch
            {
                _bandeja = null;
            }
        }
    }
}
