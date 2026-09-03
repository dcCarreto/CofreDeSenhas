using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
        private bool _bloqueando;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        // Chamada pela segunda instância (via Program): traz a janela existente pra
        // frente em vez de abrir um segundo processo sobre o mesmo cofre.
        internal static void TrazerJanelaParaFrente()
        {
            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } janela })
            {
                janela.Show();
                janela.WindowState = WindowState.Normal;
                janela.Activate();
            }
        }

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
            Acessibilidade.Aplicar();
            Idioma.Alterado += (s, e) => AtualizarTextosBandeja();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new JanelaLogin(new AutenticacaoMestra(),
                    (chave, senhaPlano) => AbrirCofre(desktop, chave, senhaPlano));
            }

            base.OnFrameworkInitializationCompleted();
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
                var kdf = cabecalho?.Kdf ?? perfil.Kdf;
                var iteracoes = cabecalho?.Iteracoes ?? (perfil.Iteracoes > 0 ? perfil.Iteracoes : ServicoSincronizacao.Iteracoes);
                var memoriaKb = cabecalho?.MemoriaKb ?? perfil.MemoriaKb;
                var paralelismo = cabecalho?.Paralelismo ?? perfil.Paralelismo;

                var chaveSincronizacao = ServicoSincronizacao.DerivarChave(senhaMestraPlano, salt, kdf, iteracoes, memoriaKb, paralelismo);
                return new ServicoSincronizacao(new ServicoCriptografia(chaveSincronizacao));
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "PrepararSincronizacao");
                return null;
            }
        }

        private async void Bloquear(IClassicDesktopStyleApplicationLifetime desktop)
        {
            // desktop.MainWindow só é reatribuído perto do fim (depois do await de
            // limpeza do clipboard) — sem essa trava, acionar bloquear duas vezes
            // rápido (atalho + botão, ou duplo clique) passa os dois pelo guard acima
            // enquanto MainWindow ainda é o cofre antigo, criando duas JanelaLogin
            // simultâneas.
            if (_bloqueando || desktop.MainWindow is not JanelaPrincipal cofre)
                return;

            _bloqueando = true;
            try
            {
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
            finally
            {
                _bloqueando = false;
            }
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
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "ConfigurarBandeja");
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
