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

            AtualizarTitulo();
            TxtNomeServico.Text = _senhaAtual.NomeServico;
            TxtUsuario.Text = _senhaAtual.Usuario;
            TxtUrl.Text = _senhaAtual.Url ?? "";
            TxtNotas.Text = _senhaAtual.Notas ?? "";
            TxtCategoriaPersonalizada.Text = CategoriaPersonalizadaAtual();
            TxtTotp.Text = TotpAtualPlain();

            AtualizarCategorias();
            CmbCategoria.SelectedIndex = (int)_senhaAtual.Categoria;
            CmbCategoria.SelectionChanged += Categoria_Alterada;
            AtualizarCampoCategoriaPersonalizada();

            TxtTotp.TextChanged += (s, e) => AtualizarPreviewTotp();
            Idioma.Alterado += Idioma_Alterado;
            Closed += (s, e) =>
            {
                PararTimerTotp();
                Idioma.Alterado -= Idioma_Alterado;
            };
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

        private void Idioma_Alterado(object? sender, EventArgs e)
        {
            AtualizarTitulo();
            AtualizarCategorias();
            AtualizarPreviewTotp();
        }

        private void AtualizarTitulo()
        {
            Title = Idioma.Texto("Entry.EditTitle");
            LblTitulo.Text = Idioma.Formatar("Entry.EditTitleWithService", _senhaAtual.NomeServico);
        }

        private void AtualizarCategorias()
        {
            var selecionado = Math.Max(0, CmbCategoria.SelectedIndex);
            CmbCategoria.ItemsSource = CategoriasUI.Rotulos;
            CmbCategoria.SelectedIndex = selecionado;
        }

        private async void Salvar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtNomeServico.Text) || string.IsNullOrWhiteSpace(TxtUsuario.Text))
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Entry.EditRequired"), Idioma.Texto("Common.Validation"), TipoMensagem.Aviso);
                    return;
                }

                var totp = TxtTotp.Text;
                if (!string.IsNullOrWhiteSpace(totp) && !_totp.SegredoValido(totp))
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Entry.TotpInvalid"),
                        Idioma.Texto("Common.Validation"), TipoMensagem.Aviso);
                    return;
                }

                var novaSenha = TxtSenha.Text;
                if (string.IsNullOrWhiteSpace(novaSenha))
                {
                    novaSenha = _criptografia?.Descriptografar(_senhaAtual.SenhaHash);
                    if (string.IsNullOrEmpty(novaSenha))
                    {
                        await CaixaMensagem.MostrarAsync(this,
                            Idioma.Texto("Entry.RecoverCurrentPasswordError"),
                            Idioma.Texto("Entry.EditTitle"), TipoMensagem.Aviso);
                        return;
                    }
                }

                var (categoria, categoriasPersonalizadas) = LerCategoria();
                await _servicoSenha.AtualizarSenhaAsync(
                    _senhaAtual.Id,
                    TxtNomeServico.Text!,
                    TxtUsuario.Text!,
                    novaSenha,
                    categoria,
                    string.IsNullOrWhiteSpace(TxtUrl.Text) ? null : TxtUrl.Text,
                    string.IsNullOrWhiteSpace(TxtNotas.Text) ? null : TxtNotas.Text,
                    categoriasPersonalizadas);

                await _servicoSenha.DefinirTotpAsync(_senhaAtual.Id, totp);

                await _servicoSenha.PersistirAsync();
                Close(true);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.UpdateError", ex.Message), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
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
                LblContagemTotp.Text = Idioma.Formatar("Entry.TotpExpiresIn", codigo.SegundosRestantes);
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

        private string CategoriaPersonalizadaAtual() =>
            _senhaAtual.Categoria == Categoria.Other && _senhaAtual.Etiquetas.Count > 0
                ? _senhaAtual.Etiquetas[0]
                : "";

        private void Categoria_Alterada(object? sender, SelectionChangedEventArgs e) =>
            AtualizarCampoCategoriaPersonalizada();

        private void AtualizarCampoCategoriaPersonalizada()
        {
            bool visivel = (Categoria)Math.Max(0, CmbCategoria.SelectedIndex) == Categoria.Other;
            LblCategoriaPersonalizada.IsVisible = visivel;
            TxtCategoriaPersonalizada.IsVisible = visivel;
            if (!visivel)
                TxtCategoriaPersonalizada.Text = "";
        }

        private (Categoria categoria, List<string> categoriasPersonalizadas) LerCategoria()
        {
            var categoria = (Categoria)Math.Max(0, CmbCategoria.SelectedIndex);
            if (categoria != Categoria.Other)
                return (categoria, new List<string>());

            var texto = TxtCategoriaPersonalizada.Text;
            if (CategoriasUI.TentarObterCategoria(texto, out var existente))
                return (existente, new List<string>());

            return (Categoria.Other, Etiquetas.Normalizar(new[] { texto ?? "" }));
        }

        private static string FormatarCodigo(string codigo) =>
            codigo.Length == 6 ? codigo.Insert(3, " ") : codigo;
    }
}
