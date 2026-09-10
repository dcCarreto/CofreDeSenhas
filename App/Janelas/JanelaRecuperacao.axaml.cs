using System.Security.Cryptography;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    // "Esqueci a senha mestra": a pessoa digita a chave de recuperação, escolhe
    // uma senha mestra nova e o cofre é re-cifrado com ela. Só reset — não é uma
    // credencial alternativa permanente.
    public partial class JanelaRecuperacao : Window
    {
        private readonly AutenticacaoMestra _auth;
        private readonly ControleTentativasLogin _tentativas;
        private DispatcherTimer? _timerBloqueio;
        private bool _emAndamento;

        public bool Recuperado { get; private set; }

        public JanelaRecuperacao(AutenticacaoMestra? auth = null)
        {
            _auth = auth ?? new AutenticacaoMestra();
            _tentativas = new ControleTentativasLogin(_auth.PastaApp);

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            Acessibilidade.RegistrarAnunciador(this, LblAnuncioLeitorTela);

            this.FecharComEscConfirmarComEnter(() => { _ = Recuperar(); });
            Closed += (s, e) => _timerBloqueio?.Stop();

            Opened += (s, e) =>
            {
                TxtChave.Focus();
                if (_tentativas.ObterBloqueioAtivo() is { } ate)
                {
                    MostrarErro(Idioma.Texto("Login.Error.TooManyAttempts"));
                    IniciarContagemBloqueio(ate);
                }
            };
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) =>
            this.HabilitarArraste(e, origem => origem is TextBox);

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close();

        private async void Recuperar_Click(object? sender, RoutedEventArgs e) => await Recuperar();

        private async Task Recuperar()
        {
            if (_emAndamento || !BtnRecuperar.IsEnabled)
                return;

            LblErro.Text = "";
            var chaveDigitada = TxtChave.Text ?? "";
            if (string.IsNullOrWhiteSpace(chaveDigitada))
            {
                MostrarErro(Idioma.Texto("Recovery.ErrorKeyRequired"));
                return;
            }

            var chaveRecuperada = new ServicoRecuperacao(_auth.PastaApp).Recuperar(chaveDigitada);
            if (chaveRecuperada == null)
            {
                var (tentativas, bloqueioAte) = _tentativas.RegistrarFalha();
                if (bloqueioAte is { } ate)
                {
                    MostrarErro(Idioma.Texto("Login.Error.TooManyAttempts"));
                    IniciarContagemBloqueio(ate);
                }
                else
                {
                    MostrarErro(Idioma.Texto("Recovery.ErrorKeyInvalid"));
                }
                return;
            }

            _tentativas.RegistrarSucesso();

            var nova = TxtNovaSenha.Text ?? "";
            if (nova.Length < AutenticacaoMestra.TamanhoMinimoSenha)
            {
                CryptographicOperations.ZeroMemory(chaveRecuperada);
                MostrarErro(Idioma.Texto("Login.Error.PasswordLength"));
                return;
            }
            if (nova != (TxtConfirmar.Text ?? ""))
            {
                CryptographicOperations.ZeroMemory(chaveRecuperada);
                MostrarErro(Idioma.Texto("Login.Error.PasswordMismatch"));
                return;
            }

            _emAndamento = true;
            BtnRecuperar.IsEnabled = false;
            try
            {
                var chaveNova = await new ServicoMudancaSenhaMestra(_auth.PastaApp)
                    .AlterarComChaveAsync(chaveRecuperada, nova);
                CryptographicOperations.ZeroMemory(chaveRecuperada);

                var segredoNovo = new ServicoRecuperacao(_auth.PastaApp).Habilitar(chaveNova);
                CryptographicOperations.ZeroMemory(chaveNova);

                Recuperado = true;
                await RecuperacaoUi.MostrarChaveAsync(this, segredoNovo);
                Close();
            }
            catch (ErroLocalizavel ex)
            {
                CryptographicOperations.ZeroMemory(chaveRecuperada);
                MostrarErro(Idioma.Texto(ex.Chave));
                _emAndamento = false;
                BtnRecuperar.IsEnabled = true;
            }
            catch (Exception ex)
            {
                CryptographicOperations.ZeroMemory(chaveRecuperada);
                MostrarErro(ErrosUi.MensagemAmigavel(ex));
                _emAndamento = false;
                BtnRecuperar.IsEnabled = true;
            }
        }

        private void IniciarContagemBloqueio(DateTime bloqueadoAteUtc)
        {
            BtnRecuperar.IsEnabled = false;
            _timerBloqueio?.Stop();

            var restante = bloqueadoAteUtc - DateTime.UtcNow;
            if (restante < TimeSpan.FromMilliseconds(1))
                restante = TimeSpan.FromMilliseconds(1);

            var t = new DispatcherTimer { Interval = restante };
            t.Tick += (s, ev) =>
            {
                BtnRecuperar.IsEnabled = true;
                LblErro.Text = "";
                t.Stop();
            };
            _timerBloqueio = t;
            t.Start();
        }

        private void MostrarErro(string msg) => this.MostrarErroInline(LblErro, msg, TxtChave);
    }
}
