using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaEditarSenha : Window
    {
        private readonly IServicoSenha _servicoSenha;
        private readonly IServicoCriptografia? _criptografia;
        private readonly Senha _senhaAtual;
        private readonly ServicoTotp _totp = new();
        private DispatcherTimer? _timerTotp;

        public JanelaEditarSenha(IServicoSenha servicoSenha, Senha senhaAtual, IServicoCriptografia? criptografia)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _senhaAtual = senhaAtual ?? throw new ArgumentNullException(nameof(senhaAtual));
            _criptografia = criptografia;

            InitializeComponent();
            Icon = Recursos.IconeApp();

            LblTitulo.Text = $"Editar senha — {_senhaAtual.NomeServico}";
            TxtNomeServico.Text = _senhaAtual.NomeServico;
            TxtUsuario.Text = _senhaAtual.Usuario;
            TxtUrl.Text = _senhaAtual.Url ?? "";
            TxtNotas.Text = _senhaAtual.Notas ?? "";
            TxtTotp.Text = TotpAtualPlain();

            CmbCategoria.ItemsSource = CategoriasUI.Rotulos;
            CmbCategoria.SelectedIndex = (int)_senhaAtual.Categoria;

            TxtTotp.TextChanged += (s, e) => AtualizarPreviewTotp();
            Closed += (s, e) => PararTimerTotp();
            AtualizarPreviewTotp();
        }

        private string TotpAtualPlain()
        {
            if (string.IsNullOrEmpty(_senhaAtual.TotpSegredo) || _criptografia == null)
                return "";

            try { return _criptografia.Descriptografar(_senhaAtual.TotpSegredo); }
            catch { return ""; }
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
                if (string.IsNullOrWhiteSpace(TxtNomeServico.Text) || string.IsNullOrWhiteSpace(TxtUsuario.Text))
                {
                    await CaixaMensagem.MostrarAsync(this,
                        "Preencha os campos obrigatórios.", "Validação", TipoMensagem.Aviso);
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

                var novaSenha = TxtSenha.Text;
                if (string.IsNullOrWhiteSpace(novaSenha))
                {
                    novaSenha = _criptografia?.Descriptografar(_senhaAtual.SenhaHash);
                    if (string.IsNullOrEmpty(novaSenha))
                    {
                        await CaixaMensagem.MostrarAsync(this,
                            "Não foi possível recuperar a senha atual. Digite uma nova senha.",
                            "Editar senha", TipoMensagem.Aviso);
                        return;
                    }
                }

                var categoria = (Categoria)Math.Max(0, CmbCategoria.SelectedIndex);
                await _servicoSenha.AtualizarSenhaAsync(
                    _senhaAtual.Id,
                    TxtNomeServico.Text!,
                    TxtUsuario.Text!,
                    novaSenha,
                    categoria,
                    string.IsNullOrWhiteSpace(TxtUrl.Text) ? null : TxtUrl.Text,
                    string.IsNullOrWhiteSpace(TxtNotas.Text) ? null : TxtNotas.Text);

                await _servicoSenha.DefinirTotpAsync(_senhaAtual.Id, totp);

                await _servicoSenha.PersistirAsync();
                Close(true);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    $"Erro ao atualizar senha: {ex.Message}", "Erro", TipoMensagem.Erro);
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
