using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Servicos;

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

            CmbModoGerador.ItemsSource = new[] { "Senha", "Frase-senha" };
            CmbModoGerador.SelectedIndex = 0;
            CmbSeparadorFrase.ItemsSource = new[] { "-", "_", ".", "espaço" };
            CmbSeparadorFrase.SelectedIndex = 0;

            SliderTamanho.ValueChanged += (s, e) => LblTamanhoValor.Text = SliderTamanho.Value.ToString();
            SliderQuantidade.ValueChanged += (s, e) => LblQuantidadeValor.Text = SliderQuantidade.Value.ToString();
            SliderPalavras.ValueChanged += (s, e) => LblPalavrasValor.Text = SliderPalavras.Value.ToString();
            CmbModoGerador.SelectionChanged += ModoGerador_Alterado;

            AtualizarModoGerador();
            AplicarPermiteSalvar();
        }

        public bool PermiteSalvar
        {
            get => _permiteSalvar;
            set { _permiteSalvar = value; AplicarPermiteSalvar(); }
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

        private Window? JanelaDona => TopLevel.GetTopLevel(this) as Window;

        private IClipboard? AreaTransferencia => TopLevel.GetTopLevel(this)?.Clipboard;

        private bool ModoFraseSenha => CmbModoGerador.SelectedIndex == 1;

        private string ItemGeradoNome => ModoFraseSenha ? "frase-senha" : "senha";

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
            BtnGerar.Content = fraseSenha ? "Gerar frase-senha" : "Gerar nova senha";
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
                    await CaixaMensagem.MostrarAsync(janela, ex.Message, "Aviso", TipoMensagem.Aviso);
                return;
            }

            _senhaGerada = _senhasGeradas[0];
            AtualizarSenhaGerada();
            AtualizarBarraForca();
            AtualizarListaSenhasGeradas();
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
                Text = $"{(ModoFraseSenha ? "Frases-senha" : "Senhas")} geradas ({_senhasGeradas.Count})",
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var btnCopiarTodas = new Button
            {
                Content = "Copiar todas",
                Height = 26,
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.AccentPrimary),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            btnCopiarTodas.Classes.Add("plano");
            btnCopiarTodas.Click += async (s, e) =>
            {
                if (AreaTransferencia != null)
                    try { await AreaTransferencia.SetTextAsync(string.Join(Environment.NewLine, _senhasGeradas)); } catch { }
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

            var btnCopiar = new Button { Content = "⧉", Width = 28, Height = 28 };
            btnCopiar.Classes.Add("icone-linha");
            btnCopiar.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            btnCopiar.Margin = new Thickness(0, 0, 5, 0);
            btnCopiar.Click += async (s, e) =>
            {
                if (AreaTransferencia != null)
                    try { await AreaTransferencia.SetTextAsync(senha); } catch { }
                btnCopiar.Content = "✓";
                btnCopiar.Foreground = Tema.Pincel(Tema.StrengthStrong);
                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                t.Tick += (ss, ee) =>
                {
                    btnCopiar.Content = "⧉";
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
                1 => ("Fraca", Tema.StrengthWeak),
                2 => ("Média", Tema.StrengthMedium),
                3 => ("Forte", Tema.StrengthStrong),
                4 => ("Excelente", Tema.StrengthExcelent),
                _ => ("—", Tema.TextSecondary)
            };

            LblForca.Text = texto;
            LblForca.Foreground = Tema.Pincel(cor);

            var segmentos = new[] { SegForca1, SegForca2, SegForca3, SegForca4 };
            for (int i = 0; i < segmentos.Length; i++)
                segmentos[i].Background = Tema.Pincel(i < _nivelForca ? cor : Tema.TrailInactive);
        }

        private void OlhoGerada_Click(object? sender, RoutedEventArgs e)
        {
            _mostrarSenha = !_mostrarSenha;
            AtualizarSenhaGerada();
            AtualizarListaSenhasGeradas();
        }

        private async void CopiarGerada_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_senhaGerada) || AreaTransferencia == null)
                return;
            await AreaTransferencia.SetTextAsync(_senhaGerada);
            if (JanelaDona is { } janela)
                await CaixaMensagem.MostrarAsync(janela,
                    ModoFraseSenha ? "Frase-senha copiada!" : "Senha copiada!",
                    "Sucesso");
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
                        $"Gere uma {ItemGeradoNome} primeiro", "Aviso", TipoMensagem.Aviso);
                return;
            }

            SolicitouSalvar?.Invoke(this, _senhaGerada);
        }
    }
}
