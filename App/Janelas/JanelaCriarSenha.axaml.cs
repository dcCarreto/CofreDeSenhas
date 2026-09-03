using Avalonia;
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

        // Acumula por chave, não substitui: sem isto, ir e voltar entre tipos que não
        // compartilham campo (Cartão -> Login -> Cartão) apagava validade/CVV/bandeira
        // já digitados antes mesmo de salvar, já que só os campos do tipo anterior
        // ficavam disponíveis pra repovoar o painel.
        private readonly Dictionary<string, string> _camposExtrasAcumulados = new();

        public Senha? SenhaCriada { get; private set; }

        public JanelaCriarSenha(IServicoSenha servicoSenha, string? senhaGerada = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            AtualizarCategorias();
            CmbCategoria.SelectedIndex = (int)Categoria.Personal;

            CmbTipo.ItemsSource = TemplatesCredencial.Rotulos;
            CmbTipo.SelectedIndex = 0;
            CmbTipo.SelectionChanged += (s, e) => AtualizarCamposPorTipo();
            AtualizarCamposPorTipo();

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

            var selecionadoTipo = Math.Max(0, CmbTipo.SelectedIndex);
            CmbTipo.ItemsSource = TemplatesCredencial.Rotulos;
            CmbTipo.SelectedIndex = selecionadoTipo;
            AtualizarCamposPorTipo();
        }

        private void AtualizarCategorias()
        {
            var selecionado = Math.Max(0, CmbCategoria.SelectedIndex);
            CmbCategoria.ItemsSource = CategoriasUI.Rotulos;
            CmbCategoria.SelectedIndex = selecionado;
        }

        private void AtualizarCamposPorTipo()
        {
            var tipo = TemplatesCredencial.ObterTipo(CmbTipo.SelectedIndex);
            LblUsuario.Text = TemplatesCredencial.RotuloUsuario(tipo);
            AutomationProperties.SetName(TxtUsuario, TemplatesCredencial.RotuloUsuario(tipo));
            LblSenha.Text = TemplatesCredencial.RotuloSenha(tipo);
            AutomationProperties.SetName(TxtSenha, TemplatesCredencial.RotuloSenha(tipo));

            foreach (var caixa in PainelCamposExtras.Children.OfType<TextBox>())
                if (caixa.Tag is string chave)
                    _camposExtrasAcumulados[chave] = caixa.Text ?? "";

            PainelCamposExtras.Children.Clear();
            foreach (var campo in TemplatesCredencial.CamposExtras(tipo))
            {
                var rotulo = new TextBlock
                {
                    Text = campo.Rotulo,
                    FontSize = 12,
                    Foreground = Tema.Pincel(Tema.TextSecondary)
                };
                var caixa = new TextBox
                {
                    Classes = { "campo" },
                    Margin = new Thickness(0, 0, 0, 14),
                    Tag = campo.Chave
                };
                AutomationProperties.SetName(caixa, campo.Rotulo);
                if (_camposExtrasAcumulados.TryGetValue(campo.Chave, out var valor))
                    caixa.Text = valor;

                PainelCamposExtras.Children.Add(rotulo);
                PainelCamposExtras.Children.Add(caixa);
            }
        }

        private Dictionary<string, string> LerCamposExtras() =>
            PainelCamposExtras.Children
                .OfType<TextBox>()
                .Where(t => t.Tag is string)
                .ToDictionary(t => (string)t.Tag!, t => t.Text ?? "");

        private async void Salvar_Click(object? sender, RoutedEventArgs e)
        {
            var botao = sender as Button;
            if (botao != null)
                botao.IsEnabled = false;

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

                var (categoria, etiquetas) = CategoriasUI.LerCategoriaEEtiquetas(CmbCategoria.SelectedIndex, TxtEtiquetas.Text);
                var tipo = TemplatesCredencial.ObterTipo(CmbTipo.SelectedIndex);
                SenhaCriada = await _servicoSenha.CriarSenhaAsync(
                    TxtNomeServico.Text!,
                    TxtUsuario.Text!,
                    TxtSenha.Text!,
                    categoria,
                    string.IsNullOrWhiteSpace(TxtUrl.Text) ? null : TxtUrl.Text,
                    string.IsNullOrWhiteSpace(TxtNotas.Text) ? null : TxtNotas.Text,
                    string.IsNullOrWhiteSpace(totp) ? null : totp,
                    etiquetas,
                    tipo,
                    LerCamposExtras());

                await _servicoSenha.PersistirAsync();
                Close(true);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.CreateError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
            finally
            {
                if (botao != null)
                    botao.IsEnabled = true;
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
    }
}
