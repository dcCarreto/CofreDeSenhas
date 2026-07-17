using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Servicos;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace CofreDeSenhas.Controles
{
    public partial class GeradorSenha : UserControl
    {
        private readonly ServicoGeracaoSenha _servicoGeracaoSenha = new();
        private readonly List<string> _senhasGeradas = new();

        private string _senhaGerada = "";
        private bool _mostrarSenha = true;
        private bool _permiteSalvar = true;
        private int _nivelForca;


        public event EventHandler<string>? SolicitouSalvar;

        public GeradorSenha()
        {
            InitializeComponent();

            SliderTamanho.ValueChanged += (s, e) => LblTamanhoValor.Text = SliderTamanho.Value.ToString();
            SliderQuantidade.ValueChanged += (s, e) => LblQuantidadeValor.Text = SliderQuantidade.Value.ToString();
            SliderPalavras.ValueChanged += (s, e) => LblPalavrasValor.Text = SliderPalavras.Value.ToString();
            CmbModoGerador.SelectionChanged += ModoGerador_Alterado;
            Idioma.Alterado += Idioma_Alterado;
            DetachedFromVisualTree += (s, e) => Idioma.Alterado -= Idioma_Alterado;

            AtualizarTextos();
            CmbModoGerador.SelectedIndex = 0;
            CmbSeparadorFrase.SelectedIndex = 0;
            AtualizarModoGerador();
            ConfigurarAcessibilidade();
            AplicarPermiteSalvar();
        }

        public bool PermiteSalvar
        {
            get => _permiteSalvar;
            set { _permiteSalvar = value; AplicarPermiteSalvar(); }
        }

        private bool _mostrarCabecalho = true;

        public bool ShowHeader
        {
            get => _mostrarCabecalho;
            set { _mostrarCabecalho = value; LblCabecalho.IsVisible = value; }
        }

        public void AtualizarTema()
        {
            AtualizarBarraForca();
            AtualizarListaSenhasGeradas();
        }

        private void AplicarPermiteSalvar()
        {
            BtnSalvar.IsVisible = _permiteSalvar;
            Grid.SetColumn(BtnLimpar, _permiteSalvar ? 2 : 0);
            Grid.SetColumnSpan(BtnLimpar, _permiteSalvar ? 1 : 3);
        }

        private static AvaloniaPath CriarIcone(string chave, double tamanho, IBrush? stroke = null) => new()
        {
            Data = (Geometry)Application.Current!.FindResource(chave)!,
            Width = tamanho,
            Height = tamanho,
            Stretch = Stretch.Uniform,
            Stroke = stroke ?? Tema.Pincel(Tema.TextSecondary),
            StrokeThickness = 2,
            StrokeLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent
        };

        private static StackPanel CriarConteudoBotao(string icone, string texto, double tamanhoIcone, IBrush? stroke = null) => new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                CriarIcone(icone, tamanhoIcone, stroke),
                new TextBlock
                {
                    Text = texto,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        private Window? JanelaDona => TopLevel.GetTopLevel(this) as Window;

        private IClipboard? AreaTransferencia => TopLevel.GetTopLevel(this)?.Clipboard;

        private bool ModoFraseSenha => CmbModoGerador.SelectedIndex == 1;

        private string ItemGeradoNome => ModoFraseSenha
            ? Idioma.Texto("Generator.Item.Passphrase")
            : Idioma.Texto("Generator.Item.Password");

        private void Idioma_Alterado(object? sender, EventArgs e)
        {
            AtualizarTextos();
            ConfigurarAcessibilidade();
        }

        private void AtualizarTextos()
        {
            var modo = Math.Max(0, CmbModoGerador.SelectedIndex);
            var separador = Math.Max(0, CmbSeparadorFrase.SelectedIndex);

            CmbModoGerador.ItemsSource = new[]
            {
                Idioma.Texto("Generator.Mode.Password"),
                Idioma.Texto("Generator.Mode.Passphrase")
            };
            CmbModoGerador.SelectedIndex = modo;

            CmbSeparadorFrase.ItemsSource = new[] { "-", "_", ".", Idioma.Texto("Generator.Separator.Space") };
            CmbSeparadorFrase.SelectedIndex = separador;

            AtualizarModoGerador();
            AtualizarBarraForca();
            AtualizarListaSenhasGeradas();
            ConfigurarAcessibilidade();
        }

        private void ConfigurarAcessibilidade()
        {
            AutomationProperties.SetName(TxtSenhaGerada, Idioma.Texto("A11y.GeneratedPassword"));
            AutomationProperties.SetHelpText(TxtSenhaGerada, Idioma.Texto("A11y.GeneratedPasswordHelp"));
            AutomationProperties.SetName(PainelGeradas, Idioma.Texto("A11y.GeneratedList"));
            AutomationProperties.SetLiveSetting(LblForca, AutomationLiveSetting.Polite);
            AutomationProperties.SetName(BtnGerar, Idioma.Texto(ModoFraseSenha
                ? "Generator.GeneratePassphrase"
                : "Generator.GeneratePassword"));
            AutomationProperties.SetName(BtnSalvar, Idioma.Texto("Generator.SaveToVault"));
            AutomationProperties.SetName(BtnLimpar, Idioma.Texto("Generator.Clear"));
        }

        private void ModoGerador_Alterado(object? sender, SelectionChangedEventArgs e)
        {
            AtualizarModoGerador();
            LimparGeracao();
        }

        private void AtualizarModoGerador()
        {
            bool fraseSenha = ModoFraseSenha;
            PainelSenhaCaracteres.IsVisible = !fraseSenha;
            PainelFraseSenha.IsVisible = fraseSenha;
            var texto = Idioma.Texto(fraseSenha
                ? "Generator.GeneratePassphrase"
                : "Generator.GeneratePassword");
            BtnGerar.Content = CriarConteudoBotao("IconeGerar", texto, 15, Brushes.White);
            AutomationProperties.SetName(BtnGerar, texto);
        }

        private string SeparadorFraseSelecionado()
        {
            return CmbSeparadorFrase.SelectedIndex switch
            {
                1 => "_",
                2 => ".",
                3 => " ",
                _ => "-"
            };
        }

        private async void Gerar_Click(object? sender, RoutedEventArgs e)
        {
            _senhasGeradas.Clear();
            try
            {
                if (ModoFraseSenha)
                {
                    _senhasGeradas.AddRange(_servicoGeracaoSenha.GerarFrasesSenha(
                        SliderQuantidade.Value,
                        SliderPalavras.Value,
                        SeparadorFraseSelecionado(),
                        ToggleCapitalizarFrase.Checked,
                        ToggleNumeroFrase.Checked));
                }
                else
                {
                    _senhasGeradas.AddRange(_servicoGeracaoSenha.GerarSenhas(
                        SliderQuantidade.Value,
                        SliderTamanho.Value,
                        ToggleMaiusculas.Checked,
                        ToggleMinusculas.Checked,
                        ToggleNumeros.Checked,
                        ToggleEspeciais.Checked));
                }
            }
            catch (ArgumentException ex)
            {
                if (JanelaDona is { } janela)
                    await CaixaMensagem.MostrarAsync(janela, ex.Message, Idioma.Texto("Common.Warning"), TipoMensagem.Aviso);
                return;
            }

            _senhaGerada = _senhasGeradas[0];
            AtualizarSenhaGerada();
            AtualizarBarraForca();
            AtualizarListaSenhasGeradas();
            Acessibilidade.Anunciar(this,
                Idioma.Formatar("A11y.GeneratedReady", _senhasGeradas.Count, ItemGeradoNome));
        }

        private void AtualizarSenhaGerada()
        {
            TxtSenhaGerada.IsVisible = _senhasGeradas.Count <= 1;
            TxtSenhaGerada.Text = string.IsNullOrEmpty(_senhaGerada)
                ? ""
                : TextoSenhaVisivel(_senhaGerada);
        }

        private string TextoSenhaVisivel(string senha) =>
            _mostrarSenha ? senha : new string('•', senha.Length);

        private void AtualizarListaSenhasGeradas()
        {
            PainelGeradas.Children.Clear();
            PainelGeradas.IsVisible = false;

            if (_senhasGeradas.Count <= 1)
                return;

            PainelGeradas.IsVisible = true;

            var titulo = new TextBlock
            {
                Text = Idioma.Formatar(ModoFraseSenha
                    ? "Generator.PassphrasesGenerated"
                    : "Generator.PasswordsGenerated", _senhasGeradas.Count),
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var btnCopiarTodas = new Button
            {
                Content = CriarConteudoBotao("IconeCopiar", Idioma.Texto("Generator.CopyAll"), 13, Tema.Pincel(Tema.AccentText)),
                Height = 26,
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.AccentText),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            btnCopiarTodas.Classes.Add("plano");
            AutomationProperties.SetName(btnCopiarTodas, Idioma.Texto("A11y.CopyAllGenerated"));
            btnCopiarTodas.Click += async (s, e) =>
            {
                var texto = string.Join(Environment.NewLine, _senhasGeradas);
                if (AreaTransferencia != null)
                    try { await AreaTransferencia.SetTextAsync(texto); } catch { }

                int segundos = Preferencias.SegundosLimpezaClipboard;
                if (segundos > 0 && AreaTransferencia != null)
                {
                    Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.CopiedWillClear", Idioma.Texto("Generator.CopyAll"), segundos));
                    _ = ServicoLimpezaClipboard.ProgramarLimpezaAsync(new AreaTransferenciaAvalonia(AreaTransferencia), texto, segundos);
                }
                else
                {
                    Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", Idioma.Texto("Generator.CopyAll")));
                }
            };

            var header = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            header.Children.Add(titulo);
            header.Children.Add(btnCopiarTodas);
            PainelGeradas.Children.Add(header);

            foreach (var senha in _senhasGeradas)
                PainelGeradas.Children.Add(CriarItemSenhaGerada(senha));
        }

        private Border CriarItemSenhaGerada(string senha)
        {
            var lbl = new TextBlock
            {
                Text = TextoSenhaVisivel(senha),
                FontFamily = (FontFamily)Application.Current!.FindResource("FonteMono")!,
                FontSize = 13,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(12, 9, 8, 9)
            };

            var btnCopiar = new Button { Content = CriarIcone("IconeCopiar", 13), Width = 28, Height = 28 };
            btnCopiar.Classes.Add("icone-linha");
            btnCopiar.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            btnCopiar.Margin = new Thickness(0, 0, 5, 0);
            AutomationProperties.SetName(btnCopiar, Idioma.Texto("A11y.CopyGenerated"));
            AutomationProperties.SetHelpText(btnCopiar, Idioma.Texto("A11y.GeneratedPasswordHelp"));
            btnCopiar.Click += async (s, e) =>
            {
                if (AreaTransferencia != null)
                    try { await AreaTransferencia.SetTextAsync(senha); } catch { }
                btnCopiar.Content = CriarIcone("IconeCheck", 13);
                btnCopiar.Foreground = Tema.Pincel(Tema.StrengthStrong);

                int segundos = Preferencias.SegundosLimpezaClipboard;
                bool vaiLimpar = segundos > 0 && AreaTransferencia != null;
                if (vaiLimpar)
                {
                    Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.CopiedWillClear", ItemGeradoNome, segundos));
                    _ = ServicoLimpezaClipboard.ProgramarLimpezaAsync(new AreaTransferenciaAvalonia(AreaTransferencia!), senha, segundos);
                }
                else
                {
                    Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", ItemGeradoNome));
                }

                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                t.Tick += (ss, ee) =>
                {
                    btnCopiar.Content = CriarIcone("IconeCopiar", 13);
                    btnCopiar.ClearValue(ForegroundProperty);
                    t.Stop();
                };
                t.Start();
            };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(lbl);
            Grid.SetColumn(btnCopiar, 1);
            grid.Children.Add(btnCopiar);

            return new Border
            {
                MinHeight = 38,
                CornerRadius = new CornerRadius(8),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 6),
                Child = grid
            };
        }

        private void AtualizarBarraForca()
        {
            _nivelForca = ForcaSenha.Calcular(_senhaGerada);
            var (texto, cor) = _nivelForca switch
            {
                1 => (Idioma.Texto("Generator.StrengthWeak"), Tema.StrengthWeak),
                2 => (Idioma.Texto("Generator.StrengthMedium"), Tema.StrengthMedium),
                3 => (Idioma.Texto("Generator.StrengthStrong"), Tema.StrengthStrong),
                4 => (Idioma.Texto("Generator.StrengthExcellent"), Tema.StrengthExcellent),
                _ => ("—", Tema.TextSecondary)
            };

            LblForca.Text = texto;
            LblForca.Foreground = Tema.Pincel(cor);
            AutomationProperties.SetName(LblForca, $"{Idioma.Texto("Generator.Strength")}: {texto}");

            var segmentos = new[] { SegForca1, SegForca2, SegForca3, SegForca4 };
            for (int i = 0; i < segmentos.Length; i++)
                segmentos[i].Background = Tema.Pincel(i < _nivelForca ? cor : Tema.TrailInactive);
        }

        private void OlhoGerada_Click(object? sender, RoutedEventArgs e)
        {
            _mostrarSenha = !_mostrarSenha;
            AtualizarSenhaGerada();
            AtualizarListaSenhasGeradas();
            var estado = Idioma.Texto(_mostrarSenha ? "A11y.PasswordVisible" : "A11y.PasswordHidden");
            AutomationProperties.SetName(TxtSenhaGerada, $"{Idioma.Texto("A11y.GeneratedPassword")}. {estado}");
            Acessibilidade.Anunciar(this, estado);
        }

        private async void CopiarGerada_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_senhaGerada) || AreaTransferencia == null)
                return;
            await AreaTransferencia.SetTextAsync(_senhaGerada);

            int segundos = Preferencias.SegundosLimpezaClipboard;
            string mensagem;
            if (segundos > 0)
            {
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.CopiedWillClear", ItemGeradoNome, segundos));
                _ = ServicoLimpezaClipboard.ProgramarLimpezaAsync(new AreaTransferenciaAvalonia(AreaTransferencia), _senhaGerada, segundos);
                mensagem = ModoFraseSenha
                    ? Idioma.Formatar("Generator.PassphraseCopiedClearing", segundos)
                    : Idioma.Formatar("Generator.PasswordCopiedClearing", segundos);
            }
            else
            {
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", ItemGeradoNome));
                mensagem = ModoFraseSenha ? Idioma.Texto("Generator.PassphraseCopied") : Idioma.Texto("Generator.PasswordCopied");
            }

            if (JanelaDona is { } janela)
                await CaixaMensagem.MostrarAsync(janela, mensagem, Idioma.Texto("Common.Success"));
        }

        private void Limpar_Click(object? sender, RoutedEventArgs e)
        {
            LimparGeracao();
        }

        private void LimparGeracao()
        {
            _senhaGerada = "";
            _mostrarSenha = true;
            _senhasGeradas.Clear();
            AtualizarSenhaGerada();
            AtualizarBarraForca();
            AtualizarListaSenhasGeradas();
        }

        private async void SalvarNoCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_senhaGerada))
            {
                if (JanelaDona is { } janela)
                    await CaixaMensagem.MostrarAsync(janela,
                        Idioma.Formatar("Generator.GenerateFirst", ItemGeradoNome), Idioma.Texto("Common.Warning"), TipoMensagem.Aviso);
                return;
            }

            SolicitouSalvar?.Invoke(this, _senhaGerada);
        }
    }
}
