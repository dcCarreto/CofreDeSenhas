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

            AtualizarCategorias();
            CmbCategoria.SelectedIndex = (int)Categoria.Personal;
            CmbCategoria.SelectionChanged += Categoria_Alterada;
            AtualizarCampoCategoriaPersonalizada();

            if (!string.IsNullOrEmpty(senhaGerada))
                TxtSenha.Text = senhaGerada;

            TxtTotp.TextChanged += (s, e) => AtualizarPreviewTotp();
            Idioma.Alterado += Idioma_Alterado;
            Closed += (s, e) =>
            {
                PararTimerTotp();
                Idioma.Alterado -= Idioma_Alterado;
            };

            Opened += (s, e) => TxtNomeServico.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private void Idioma_Alterado(object? sender, EventArgs e)
        {
            AtualizarCategorias();
            AtualizarPreviewTotp();
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
                if (string.IsNullOrWhiteSpace(TxtNomeServico.Text) ||
                    string.IsNullOrWhiteSpace(TxtUsuario.Text) ||
                    string.IsNullOrWhiteSpace(TxtSenha.Text))
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Entry.CreateRequired"),
                        Idioma.Texto("Common.Validation"), TipoMensagem.Aviso);
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

                var (categoria, categoriasPersonalizadas) = LerCategoria();
                await _servicoSenha.CriarSenhaAsync(
                    TxtNomeServico.Text!,
                    TxtUsuario.Text!,
                    TxtSenha.Text!,
                    categoria,
                    string.IsNullOrWhiteSpace(TxtUrl.Text) ? null : TxtUrl.Text,
                    string.IsNullOrWhiteSpace(TxtNotas.Text) ? null : TxtNotas.Text,
                    string.IsNullOrWhiteSpace(totp) ? null : totp,
                    categoriasPersonalizadas);

                await _servicoSenha.PersistirAsync();
                Close(true);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.CreateError", ex.Message), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
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
