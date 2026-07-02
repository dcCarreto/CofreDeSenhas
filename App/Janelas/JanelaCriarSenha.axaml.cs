using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaCriarSenha : Window
    {
        private readonly IServicoSenha _servicoSenha;
        private readonly ServicoTotp _totp = new();
        private DispatcherTimer? _timerTotp;

        public JanelaCriarSenha(IServicoSenha servicoSenha, string? senhaGerada = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));

            InitializeComponent();
            Icon = Recursos.IconeApp();

            CmbCategoria.ItemsSource = CategoriasUI.Rotulos;
            CmbCategoria.SelectedIndex = (int)Categoria.Personal;

            if (!string.IsNullOrEmpty(senhaGerada))
                TxtSenha.Text = senhaGerada;

            TxtTotp.TextChanged += (s, e) => AtualizarPreviewTotp();
            Closed += (s, e) => PararTimerTotp();

            Opened += (s, e) => TxtNomeServico.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private async void Salvar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtNomeServico.Text) ||
                    string.IsNullOrWhiteSpace(TxtUsuario.Text) ||
                    string.IsNullOrWhiteSpace(TxtSenha.Text))
                {
                    await CaixaMensagem.MostrarAsync(this,
                        "Preencha os campos obrigatórios (nome, usuário e senha).",
                        "Validação", TipoMensagem.Aviso);
                    return;
                }

                var totp = TxtTotp.Text;
                if (!string.IsNullOrWhiteSpace(totp) && !_totp.SegredoValido(totp))
                {
                    await CaixaMensagem.MostrarAsync(this,
                        "A chave de autenticação em duas etapas é inválida. Cole a chave secreta (Base32) ou um link otpauth://.",
                        "Validação", TipoMensagem.Aviso);
                    return;
                }

                var categoria = (Categoria)Math.Max(0, CmbCategoria.SelectedIndex);
                await _servicoSenha.CriarSenhaAsync(
                    TxtNomeServico.Text!,
                    TxtUsuario.Text!,
                    TxtSenha.Text!,
                    categoria,
                    string.IsNullOrWhiteSpace(TxtUrl.Text) ? null : TxtUrl.Text,
                    string.IsNullOrWhiteSpace(TxtNotas.Text) ? null : TxtNotas.Text,
                    string.IsNullOrWhiteSpace(totp) ? null : totp);

                await _servicoSenha.PersistirAsync();
                Close(true);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    $"Erro ao criar senha: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private void AtualizarPreviewTotp()
        {
            var entrada = TxtTotp.Text;
            if (string.IsNullOrWhiteSpace(entrada) || !_totp.SegredoValido(entrada))
            {
                PainelTotp.IsVisible = false;
                PararTimerTotp();
                return;
            }

            try
            {
                var codigo = _totp.Gerar(entrada);
                LblCodigoTotp.Text = FormatarCodigo(codigo.Codigo);
                LblContagemTotp.Text = $"expira em {codigo.SegundosRestantes}s";
                PainelTotp.IsVisible = true;
                GarantirTimerTotp();
            }
            catch
            {
                PainelTotp.IsVisible = false;
                PararTimerTotp();
            }
        }

        private void GarantirTimerTotp()
        {
            if (_timerTotp != null)
                return;

            _timerTotp = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerTotp.Tick += (s, e) => AtualizarPreviewTotp();
            _timerTotp.Start();
        }

        private void PararTimerTotp()
        {
            _timerTotp?.Stop();
            _timerTotp = null;
        }

        private static string FormatarCodigo(string codigo) =>
            codigo.Length == 6 ? codigo.Insert(3, " ") : codigo;
    }
}
