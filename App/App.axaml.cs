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
        private NativeMenuItem? _itemAbrirBandeja;
        private NativeMenuItem? _itemSairBandeja;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            Preferencias.Carregar();
            Idioma.Definir(Preferencias.Idioma);
            Acessibilidade.Hidratar(
                ResolverDaltonismo(Preferencias.Daltonismo),
                Preferencias.AltoContraste,
                Preferencias.EscalaInterface,
                Preferencias.ReduzirAnimacoes,
                Preferencias.LeitorTela);
            AplicarTema(Preferencias.ModoEscuro);
            Idioma.Alterado += (s, e) => AtualizarTextosBandeja();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new JanelaLogin(new AutenticacaoMestra(),
                    (chave, senhaPlano) => AbrirCofre(desktop, chave, senhaPlano));
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static void AplicarTema(bool escuro)
        {
            Tema.DefinirModo(escuro);
            if (Current != null)
                Current.RequestedThemeVariant = escuro ? ThemeVariant.Dark : ThemeVariant.Light;
            Acessibilidade.Aplicar();
        }

        private static TipoDaltonismo ResolverDaltonismo(string? valor) =>
            Enum.TryParse<TipoDaltonismo>(valor, out var tipo) ? tipo : TipoDaltonismo.Nenhum;

        private async void AbrirCofre(IClassicDesktopStyleApplicationLifetime desktop, byte[] chave, string? senhaMestraPlano)
        {
            var criptografia = new ServicoCriptografia(chave);
            var persistencia = new PersistenciaLocal(criptografia);
            var repositorio = new RepositorioSenha(persistencia, chave);
            var servicoSenha = new ServicoSenha(repositorio, criptografia);

            var servicoSincronizacao = await PrepararSincronizacaoAsync(senhaMestraPlano);

            var principal = new JanelaPrincipal(servicoSenha, chave, criptografia, repositorio,
                () => Bloquear(desktop), servicoSincronizacao);
            var login = desktop.MainWindow;
            desktop.MainWindow = principal;
            principal.Show();
            login?.Close();

            ConfigurarBandeja(desktop);
        }

        private static async Task<ServicoSincronizacao?> PrepararSincronizacaoAsync(string? senhaMestraPlano)
        {
            var perfil = Preferencias.Sincronizacao;
            if (senhaMestraPlano == null || perfil == null || string.IsNullOrWhiteSpace(perfil.Pasta))
                return null;

            try
            {
                var caminho = Path.Combine(perfil.Pasta, ServicoSincronizacao.NomeArquivo);
                var cabecalho = await ServicoSincronizacao.LerCabecalhoAsync(caminho);
                var salt = cabecalho?.Salt ?? Convert.FromBase64String(perfil.Salt);
                var iteracoes = cabecalho?.Iteracoes ?? (perfil.Iteracoes > 0 ? perfil.Iteracoes : ServicoSincronizacao.Iteracoes);

                var chaveSincronizacao = ServicoSincronizacao.DerivarChave(senhaMestraPlano, salt, iteracoes);
                return new ServicoSincronizacao(new ServicoCriptografia(chaveSincronizacao));
            }
            catch
            {
                return null;
            }
        }

        private async void Bloquear(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is not JanelaPrincipal cofre)
                return;

            var clipboard = TopLevel.GetTopLevel(cofre)?.Clipboard;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(string.Empty); } catch { }
            }

            foreach (var janela in desktop.Windows.ToArray())
                if (janela != cofre)
                    janela.Close();

            var login = new JanelaLogin(new AutenticacaoMestra(),
                (chave, senhaPlano) => AbrirCofre(desktop, chave, senhaPlano));
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

                _itemAbrirBandeja = new NativeMenuItem();
                _itemAbrirBandeja.Click += (s, e) => Restaurar();
                _itemSairBandeja = new NativeMenuItem();
                _itemSairBandeja.Click += (s, e) => desktop.Shutdown();

                var menu = new NativeMenu();
                menu.Add(_itemAbrirBandeja);
                menu.Add(new NativeMenuItemSeparator());
                menu.Add(_itemSairBandeja);

                _bandeja = new TrayIcon
                {
                    Icon = Recursos.IconeApp(),
                    Menu = menu
                };
                _bandeja.Clicked += (s, e) => Restaurar();
                AtualizarTextosBandeja();
                _bandeja.IsVisible = true;
            }
            catch
            {
                _bandeja = null;
            }
        }

        private void AtualizarTextosBandeja()
        {
            if (_bandeja != null)
                _bandeja.ToolTipText = Idioma.Texto("App.Title");
            if (_itemAbrirBandeja != null)
                _itemAbrirBandeja.Header = Idioma.Texto("Vault.Header");
            if (_itemSairBandeja != null)
                _itemSairBandeja.Header = Idioma.Texto("Common.Exit");
        }
    }
}
