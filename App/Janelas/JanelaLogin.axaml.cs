using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CofreDeSenhas.Controles;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaLogin : Window
    {
        private readonly AutenticacaoMestra _auth;
        private readonly Action<byte[]> _aoAutenticar;
        private readonly bool _primeiroAcesso;

        private int _tentativas;

        public JanelaLogin(AutenticacaoMestra auth, Action<byte[]> aoAutenticar)
        {
            _auth = auth;
            _aoAutenticar = aoAutenticar;
            _primeiroAcesso = !auth.ExisteSenhaMestra();

            InitializeComponent();
            Icon = Recursos.IconeApp();

            Gerador.PermiteSalvar = false;

            CmbIdioma.ItemsSource = Idioma.Idiomas;
            CmbIdioma.SelectedItem = Idioma.Atual;
            CmbIdioma.SelectionChanged += Idioma_Alterado;
            Idioma.Alterado += IdiomaGlobal_Alterado;

            AtualizarTextos();
            PainelConfirmar.IsVisible = _primeiroAcesso;
            Closed += (s, e) => Idioma.Alterado -= IdiomaGlobal_Alterado;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && BtnPrincipal.IsEnabled)
                    _ = ConfirmarAsync();
            };

            Opened += (s, e) => TxtSenha.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            if (e.Source is Visual origem && OrigemInterativa(origem))
                return;

            BeginMoveDrag(e);
        }

        private static bool OrigemInterativa(Visual origem) =>
            origem.FindAncestorOfType<Button>(true) != null ||
            origem.FindAncestorOfType<TextBox>(true) != null ||
            origem.FindAncestorOfType<ComboBox>(true) != null ||
            origem.FindAncestorOfType<CustomSlider>(true) != null ||
            origem.FindAncestorOfType<CustomToggle>(true) != null ||
            origem.FindAncestorOfType<ScrollViewer>(true) != null;

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();

        private async void Principal_Click(object? sender, RoutedEventArgs e) => await ConfirmarAsync();

        private void Idioma_Alterado(object? sender, SelectionChangedEventArgs e)
        {
            if (CmbIdioma.SelectedItem is not IdiomaDisponivel idioma ||
                string.Equals(idioma.Codigo, Idioma.Atual.Codigo, StringComparison.OrdinalIgnoreCase))
                return;

            Idioma.Definir(idioma.Codigo);
            Preferencias.Idioma = Idioma.Atual.Codigo;
            Preferencias.Salvar();
        }

        private void IdiomaGlobal_Alterado(object? sender, EventArgs e)
        {
            CmbIdioma.SelectedItem = Idioma.Atual;
            AtualizarTextos();
            LblErro.Text = "";
        }

        private void AtualizarTextos()
        {
            LblSubtitulo.Text = Idioma.Texto(_primeiroAcesso
                ? "Login.SubtitleCreate"
                : "Login.SubtitleUnlock");
            BtnPrincipal.Content = Idioma.Texto(_primeiroAcesso
                ? "Login.CreateVault"
                : "Login.Unlock");
        }

        private async Task ConfirmarAsync()
        {
            LblErro.Text = "";
            var senha = TxtSenha.Text ?? "";

            if (_primeiroAcesso)
            {
                if (senha.Length < 8)
                {
                    MostrarErro(Idioma.Texto("Login.Error.PasswordLength"));
                    return;
                }
                if (senha != (TxtConfirmar.Text ?? ""))
                {
                    MostrarErro(Idioma.Texto("Login.Error.PasswordMismatch"));
                    return;
                }

                byte[] chave;
                try
                {
                    chave = _auth.CriarSenhaMestra(senha);
                }
                catch (Exception ex)
                {
                    MostrarErro(ex.Message);
                    return;
                }

                await QrBackup.OferecerSalvarAsync(this, senha);
                _aoAutenticar(chave);
            }
            else
            {
                if (string.IsNullOrEmpty(senha))
                {
                    MostrarErro(Idioma.Texto("Login.Error.MasterPasswordRequired"));
                    return;
                }

                var chave = _auth.Autenticar(senha);
                if (chave != null)
                {
                    _aoAutenticar(chave);
                    return;
                }

                _tentativas++;
                if (_tentativas >= 5)
                {
                    MostrarErro(Idioma.Texto("Login.Error.TooManyAttempts"));
                    BtnPrincipal.IsEnabled = false;
                    var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                    t.Tick += (s, ev) =>
                    {
                        BtnPrincipal.IsEnabled = true;
                        _tentativas = 0;
                        LblErro.Text = "";
                        t.Stop();
                    };
                    t.Start();
                }
                else
                {
                    MostrarErro(Idioma.Formatar("Login.Error.WrongPassword", _tentativas));
                }

                TxtSenha.SelectAll();
                TxtSenha.Focus();
            }
        }

        private void MostrarErro(string msg) => LblErro.Text = msg;
    }
}
