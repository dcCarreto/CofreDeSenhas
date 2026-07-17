using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Globalization;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaCriarSenha : Window
    {
        private readonly IServicoSenha _servicoSenha;
        private readonly ServicoTotp _totp = new();
        private readonly TotpPreview.Temporizador _timerTotp = new();
        private const int PeriodoTotp = 30;

        public JanelaCriarSenha(IServicoSenha servicoSenha, string? senhaGerada = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

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
                _timerTotp.Parar();
                Idioma.Alterado -= Idioma_Alterado;
            };

            Opened += (s, e) => TxtNomeServico.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

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
                _timerTotp.Parar();
                return;
            }

            try
            {
                var codigo = _totp.Gerar(entrada);
                LblCodigoTotp.Text = TotpPreview.FormatarCodigo(codigo.Codigo);
                var contagem = Idioma.Formatar("Entry.TotpExpiresIn", codigo.SegundosRestantes);
                AnelTotp.Data = TotpPreview.ConstruirAnelProgresso(codigo.SegundosRestantes, PeriodoTotp, raio: 10, centro: 13);
                AutomationProperties.SetName(LblCodigoTotp,
                    $"{Idioma.Texto("A11y.TotpPreview")}: {LblCodigoTotp.Text}. {contagem}");
                PainelTotp.IsVisible = true;
                _timerTotp.Garantir(AtualizarPreviewTotp);
            }
            catch
            {
                PainelTotp.IsVisible = false;
                _timerTotp.Parar();
            }
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
    }
}
