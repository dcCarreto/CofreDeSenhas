using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

            LblSubtitulo.Text = _primeiroAcesso
                ? "Crie uma senha mestra para proteger o cofre"
                : "Digite sua senha mestra para desbloquear";
            BtnPrincipal.Content = _primeiroAcesso ? "Criar cofre" : "Desbloquear";
            PainelConfirmar.IsVisible = _primeiroAcesso;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && BtnPrincipal.IsEnabled)
                    _ = ConfirmarAsync();
            };

            Opened += (s, e) => TxtSenha.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is not TextBox)
                BeginMoveDrag(e);
        }

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();

        private async void Principal_Click(object? sender, RoutedEventArgs e) => await ConfirmarAsync();

        private async Task ConfirmarAsync()
        {
            LblErro.Text = "";
            var senha = TxtSenha.Text ?? "";

            if (_primeiroAcesso)
            {
                if (senha.Length < 8)
                {
                    MostrarErro("A senha deve ter pelo menos 8 caracteres.");
                    return;
                }
                if (senha != (TxtConfirmar.Text ?? ""))
                {
                    MostrarErro("As senhas não coincidem.");
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
                    MostrarErro("Digite a senha mestra.");
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
                    MostrarErro("Muitas tentativas. Aguarde 5 segundos.");
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
                    MostrarErro($"Senha incorreta. Tentativa {_tentativas} de 5.");
                }

                TxtSenha.SelectAll();
                TxtSenha.Focus();
            }
        }

        private void MostrarErro(string msg) => LblErro.Text = msg;
    }
}
