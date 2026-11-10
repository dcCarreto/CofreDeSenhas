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

        private readonly ServicoGeracaoSenha _geracaoSenha = new();
        private const int TamanhoSenhaSugerida = 20;
        private string? _senhaSugerida;
        private string? _senhaSugeridaAplicada;
        private bool _suprimirSugestao;

        public Senha? SenhaCriada { get; private set; }

        public JanelaCriarSenha(IServicoSenha servicoSenha, string? senhaGerada = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            this.AtalhoAjuda("gerador");

            AtualizarCategorias();
            CmbCategoria.SelectedIndex = (int)Categoria.Personal;

            CmbTipo.ItemsSource = TemplatesCredencial.Rotulos;
            CmbTipo.SelectedIndex = 0;
            CmbTipo.SelectionChanged += (s, e) => AtualizarCamposPorTipo();
            AtualizarCamposPorTipo();

            if (!string.IsNullOrEmpty(senhaGerada))
                TxtSenha.Text = senhaGerada;

            ConfigurarSugestaoSenha();

            TxtTotp.TextChanged += (s, e) => AtualizarPreviewTotp();
            Idioma.Alterado += Idioma_Alterado;
            Closed += (s, e) =>
            {
                PopupSugestaoSenha.IsOpen = false;
                _timerTotp.Parar();
                Idioma.Alterado -= Idioma_Alterado;
            };

            Opened += (s, e) => TxtNomeServico.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            // O popup é uma janela à parte e não acompanha o arraste da janela dona.
            FecharSugestaoSenha();
            this.HabilitarArraste(e);
        }

        private void ConfigurarSugestaoSenha()
        {
            PopupSugestaoSenha.PlacementTarget = TxtSenha;

            TxtSenha.GotFocus += (s, e) => MostrarSugestaoSenha();
            // Tunnel + handledEventsToo: o TextBox marca o PointerPressed como tratado,
            // e clicar num campo que já tem foco não dispara GotFocus de novo.
            TxtSenha.AddHandler(PointerPressedEvent, (s, e) => MostrarSugestaoSenha(),
                RoutingStrategies.Tunnel, handledEventsToo: true);
            TxtSenha.AddHandler(KeyDownEvent, SenhaKeyDown, RoutingStrategies.Tunnel);
            TxtSenha.LostFocus += (s, e) =>
                // Adiado: deixa o foco assentar antes de decidir. Se o ponteiro está
                // sobre o popup, o Click do botão ainda vai chegar e ele mesmo fecha.
                Dispatcher.UIThread.Post(() =>
                {
                    if (!TxtSenha.IsFocused && !BordaSugestaoSenha.IsPointerOver)
                        FecharSugestaoSenha();
                });
            TxtSenha.TextChanged += (s, e) =>
            {
                // Digitar por conta própria descarta a sugestão, como no navegador.
                if (PopupSugestaoSenha.IsOpen && !PodeSugerirSenha())
                    FecharSugestaoSenha();
            };

            ScrollFormulario.ScrollChanged += (s, e) => FecharSugestaoSenha();
            Deactivated += (s, e) => FecharSugestaoSenha();
            PositionChanged += (s, e) => FecharSugestaoSenha();
        }

        // Só faz sentido sugerir senha aleatória onde o campo é de fato uma senha —
        // número de cartão e chave de licença vêm de fora, não se inventam.
        private bool TipoAceitaSenhaSugerida() =>
            TemplatesCredencial.ObterTipo(CmbTipo.SelectedIndex)
                is not (TipoCredencial.Cartao or TipoCredencial.ChaveLicenca);

        // Campo vazio, ou ainda com a última sugestão aplicada (para poder trocar por outra).
        private bool PodeSugerirSenha() =>
            TipoAceitaSenhaSugerida() &&
            (string.IsNullOrEmpty(TxtSenha.Text) || TxtSenha.Text == _senhaSugeridaAplicada);

        private void MostrarSugestaoSenha()
        {
            if (_suprimirSugestao || PopupSugestaoSenha.IsOpen || !PodeSugerirSenha())
                return;

            if (_senhaSugerida == null || _senhaSugerida == _senhaSugeridaAplicada)
                GerarSugestaoSenha();

            BordaSugestaoSenha.Width = TxtSenha.Bounds.Width;
            PopupSugestaoSenha.IsOpen = true;
            Acessibilidade.Anunciar(this, Idioma.Texto("Entry.SuggestPasswordHint"));
        }

        private void FecharSugestaoSenha() => PopupSugestaoSenha.IsOpen = false;

        private void GerarSugestaoSenha()
        {
            _senhaSugerida = _geracaoSenha.GerarSenha(TamanhoSenhaSugerida,
                incluirMaiusculas: true, incluirMinusculas: true,
                incluirNumeros: true, incluirEspeciais: true);
            LblSugestaoSenha.Text = _senhaSugerida;
        }

        private void AplicarSugestaoSenha()
        {
            if (_senhaSugerida == null)
                return;

            _senhaSugeridaAplicada = _senhaSugerida;
            TxtSenha.Text = _senhaSugerida;
            TxtSenha.CaretIndex = _senhaSugerida.Length;
            FecharSugestaoSenha();
            // Sem a supressão, devolver o foco dispararia GotFocus e reabriria o popup
            // (o campo ainda contém a sugestão aplicada, então PodeSugerirSenha é true).
            _suprimirSugestao = true;
            try { TxtSenha.Focus(); }
            finally { _suprimirSugestao = false; }
            Acessibilidade.Anunciar(this, Idioma.Texto("Entry.SuggestPasswordApplied"));
        }

        private void SenhaKeyDown(object? sender, KeyEventArgs e)
        {
            if (!PopupSugestaoSenha.IsOpen)
                return;

            if (e.Key == Key.Enter)
            {
                AplicarSugestaoSenha();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                FecharSugestaoSenha();
                e.Handled = true;
            }
        }

        private void UsarSugestaoSenha_Click(object? sender, RoutedEventArgs e) => AplicarSugestaoSenha();

        private void NovaSugestaoSenha_Click(object? sender, RoutedEventArgs e)
        {
            GerarSugestaoSenha();
            if (!TxtSenha.IsFocused)
                TxtSenha.Focus();
        }

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
            if (!TipoAceitaSenhaSugerida())
                FecharSugestaoSenha();
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
