using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaConfirmarSenhaMestra : Window
    {
        private readonly AutenticacaoMestra _auth;
        private readonly Func<string, bool>? _validador;
        private readonly ControleTentativasLogin _controleTentativas;
        private DispatcherTimer? _timerBloqueio;

        public string SenhaConfirmada { get; private set; } = string.Empty;

        public JanelaConfirmarSenhaMestra(string? titulo = null, string? instrucao = null, string? textoBotao = null,
            Func<string, bool>? validador = null, AutenticacaoMestra? auth = null)
        {
            _auth = auth ?? new AutenticacaoMestra();
            _validador = validador;
            _controleTentativas = new ControleTentativasLogin(_auth.PastaApp);
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            Acessibilidade.RegistrarAnunciador(this, LblAnuncioLeitorTela);

            Title = titulo ?? Idioma.Texto("Qr.RegenerateTitle");
            if (titulo != null)
                LblTitulo.Text = titulo;
            if (instrucao != null)
                LblInstrucao.Text = instrucao;
            if (textoBotao != null)
                BtnConfirmar.Content = textoBotao;

            this.FecharComEscConfirmarComEnter(Confirmar);

            Closed += (s, e) => _timerBloqueio?.Stop();

            Opened += (s, e) =>
            {
                TxtSenha.Focus();

                // Mesmo bloqueio de tentativas da tela de login (ver ControleTentativasLogin):
                // sem isto, este diálogo — reaberto pra excluir/limpar o cofre, regerar o QR
                // code da senha mestra ou ativar sincronização — deixava forçar a senha mestra
                // à vontade numa sessão já desbloqueada e sem vigilância, já que nunca fecha o
                // cofre de verdade e por isso nunca herdava o limite da tela de login.
                if (_controleTentativas.ObterBloqueioAtivo() is { } bloqueioAtivo)
                {
                    MostrarErro(Idioma.Texto("Login.Error.TooManyAttempts"));
                    IniciarContagemBloqueio(bloqueioAtivo);
                }
            };
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e, origem => origem is TextBox);

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private void Gerar_Click(object? sender, RoutedEventArgs e) => Confirmar();

        private void Confirmar()
        {
            if (!BtnConfirmar.IsEnabled)
                return;

            var senha = TxtSenha.Text ?? "";
            if (string.IsNullOrEmpty(senha))
            {
                MostrarErro(Idioma.Texto("Qr.ErrorMasterRequired"));
                return;
            }

            var valida = _validador?.Invoke(senha) ?? (_auth.Autenticar(senha) != null);
            if (!valida)
            {
                var (tentativas, bloqueioAte) = _controleTentativas.RegistrarFalha();
                if (bloqueioAte is { } ate)
                {
                    MostrarErro(Idioma.Texto("Login.Error.TooManyAttempts"));
                    IniciarContagemBloqueio(ate);
                }
                else
                {
                    MostrarErro(Idioma.Formatar("Login.Error.WrongPassword", tentativas));
                }
                return;
            }

            _controleTentativas.RegistrarSucesso();
            SenhaConfirmada = senha;
            Close(true);
        }

        private void IniciarContagemBloqueio(DateTime bloqueadoAteUtc)
        {
            BtnConfirmar.IsEnabled = false;
            _timerBloqueio?.Stop();

            var restante = bloqueadoAteUtc - DateTime.UtcNow;
            if (restante < TimeSpan.FromMilliseconds(1))
                restante = TimeSpan.FromMilliseconds(1);

            var t = new DispatcherTimer { Interval = restante };
            t.Tick += (s, ev) =>
            {
                BtnConfirmar.IsEnabled = true;
                LblErro.Text = "";
                t.Stop();
            };
            _timerBloqueio = t;
            t.Start();
        }

        private void MostrarErro(string msg) => this.MostrarErroInline(LblErro, msg, TxtSenha);
    }
}
