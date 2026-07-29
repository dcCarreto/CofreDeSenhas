using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaRelatorioSeguranca : Window
    {
        private readonly Func<Task<RelatorioSegurancaCofre>> _reverificarVazamentos;
        private RelatorioSegurancaCofre _relatorio;
        private bool _vazamentosVerificados;

        public CategoriaRelatorioSeguranca? CategoriaSelecionada { get; private set; }

        public JanelaRelatorioSeguranca(RelatorioSegurancaCofre relatorio, bool vazamentosVerificados,
            Func<Task<RelatorioSegurancaCofre>> reverificarVazamentos)
        {
            _relatorio = relatorio ?? throw new ArgumentNullException(nameof(relatorio));
            _vazamentosVerificados = vazamentosVerificados;
            _reverificarVazamentos = reverificarVazamentos ?? throw new ArgumentNullException(nameof(reverificarVazamentos));

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            AtualizarConteudo();

            this.FecharComEsc();

            Opened += (s, e) => BtnFechar.Focus();
        }

        private void AtualizarConteudo()
        {
            AtualizarPontuacao();
            AtualizarTendencia();
            MontarLinhas();
        }

        private void AtualizarPontuacao()
        {
            HistoricoPontuacaoSeguranca.RegistrarPontuacao(_relatorio.Pontuacao);

            LblPontuacao.Text = _relatorio.Pontuacao.ToString();

            var (chaveRotulo, cor) = _relatorio.Pontuacao switch
            {
                >= 90 => ("SecurityReport.ScoreExcellent", Tema.StrengthExcellent),
                >= 70 => ("SecurityReport.ScoreStrong", Tema.StrengthStrong),
                >= 40 => ("SecurityReport.ScoreMedium", Tema.StrengthMedium),
                _ => ("SecurityReport.ScoreWeak", Tema.StrengthWeak)
            };

            LblPontuacao.Foreground = Tema.Pincel(cor);
            LblPontuacaoRotulo.Text = Idioma.Texto(chaveRotulo);
            LblPontuacaoRotulo.Foreground = Tema.Pincel(cor);

            LblTudoCerto.IsVisible = _relatorio.SemProblemas;
        }

        private void AtualizarTendencia()
        {
            var pontos = HistoricoPontuacaoSeguranca.Carregar();
            PainelBarrasTendencia.Children.Clear();

            PainelTendencia.IsVisible = pontos.Count >= 2;
            if (pontos.Count < 2)
                return;

            foreach (var ponto in pontos)
            {
                var cor = ponto.Pontuacao switch
                {
                    >= 90 => Tema.StrengthExcellent,
                    >= 70 => Tema.StrengthStrong,
                    >= 40 => Tema.StrengthMedium,
                    _ => Tema.StrengthWeak
                };

                var barra = new Border
                {
                    Width = 6,
                    Height = Math.Max(4, ponto.Pontuacao * 0.4),
                    CornerRadius = new CornerRadius(2),
                    Background = Tema.Pincel(cor),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                ToolTip.SetTip(barra, $"{ponto.DataUtc.ToLocalTime().ToString("d", Idioma.CulturaAtual)}: {ponto.Pontuacao}");

                PainelBarrasTendencia.Children.Add(barra);
            }
        }

        private void MontarLinhas()
        {
            PainelLinhas.Children.Clear();

            if (_relatorio.CertificadoBancoNaoExigido)
                PainelLinhas.Children.Add(CriarLinhaAviso(Idioma.Texto("SecurityReport.CertNotRequired")));

            PainelLinhas.Children.Add(CriarLinha(Idioma.Texto("SecurityReport.Weak"), _relatorio.Fracas, CategoriaRelatorioSeguranca.Fraca));
            PainelLinhas.Children.Add(CriarLinha(Idioma.Texto("SecurityReport.Repeated"), _relatorio.Repetidas, CategoriaRelatorioSeguranca.Repetida));
            PainelLinhas.Children.Add(CriarLinha(Idioma.Texto("SecurityReport.Old"), _relatorio.Antigas, CategoriaRelatorioSeguranca.Antiga));
            PainelLinhas.Children.Add(CriarLinhaComprometida());
            PainelLinhas.Children.Add(CriarLinha(Idioma.Texto("SecurityReport.NoTotp"), _relatorio.SemTotp, CategoriaRelatorioSeguranca.SemTotp));
            PainelLinhas.Children.Add(CriarLinha(Idioma.Texto("SecurityReport.NoUrl"), _relatorio.SemUrl, CategoriaRelatorioSeguranca.SemUrl));
            PainelLinhas.Children.Add(CriarLinha(Idioma.Texto("SecurityReport.NoCategory"), _relatorio.SemCategoria, CategoriaRelatorioSeguranca.SemCategoria));
        }

        private Control CriarLinha(string rotulo, int contagem, CategoriaRelatorioSeguranca categoria)
        {
            var lblRotulo = new TextBlock
            {
                Text = rotulo,
                FontSize = 13,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };

            var pillContagem = new Border
            {
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(10, 3),
                Background = Tema.Pincel(contagem > 0 ? Tema.AccentLight : Tema.RowHover),
                Child = new TextBlock
                {
                    Text = contagem.ToString(),
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = Tema.Pincel(contagem > 0 ? Tema.AccentText : Tema.TextSecondary)
                }
            };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(lblRotulo);
            Grid.SetColumn(pillContagem, 1);
            grid.Children.Add(pillContagem);

            var borda = new Border
            {
                Padding = new Thickness(14, 12),
                CornerRadius = new CornerRadius(10),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Child = grid,
                Focusable = contagem > 0,
                Cursor = contagem > 0 ? new Cursor(StandardCursorType.Hand) : Cursor.Default
            };

            if (contagem > 0)
            {
                AutomationProperties.SetName(borda, Idioma.Formatar("SecurityReport.RowAutomationName", rotulo, contagem));
                borda.PointerReleased += (s, e) => SelecionarCategoria(categoria);
            }

            return borda;
        }

        private Control CriarLinhaAviso(string mensagem)
        {
            var icone = new TextBlock
            {
                Text = "⚠",
                FontSize = 16,
                Foreground = Tema.Pincel(Tema.StatusWarning),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var lblMensagem = new TextBlock
            {
                Text = mensagem,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Tema.Pincel(Tema.TextPrimary)
            };

            var linha = new StackPanel { Orientation = Orientation.Horizontal };
            linha.Children.Add(icone);
            linha.Children.Add(lblMensagem);

            return new Border
            {
                Padding = new Thickness(14, 12),
                CornerRadius = new CornerRadius(10),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.StatusWarning),
                BorderThickness = new Thickness(1),
                Child = linha
            };
        }

        private Control CriarLinhaComprometida()
        {
            if (_vazamentosVerificados)
                return CriarLinha(Idioma.Texto("SecurityReport.Compromised"), _relatorio.Comprometidas, CategoriaRelatorioSeguranca.Comprometida);

            var lblRotulo = new TextBlock
            {
                Text = Idioma.Texto("SecurityReport.Compromised"),
                FontSize = 13,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };

            var btnVerificar = new Button
            {
                Content = Idioma.Texto("SecurityReport.CheckBreaches"),
                Height = 34,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnVerificar.Classes.Add("plano");
            AutomationProperties.SetName(btnVerificar, Idioma.Texto("SecurityReport.CheckBreaches"));
            btnVerificar.Click += async (s, e) => await VerificarVazamentosAsync(btnVerificar);

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(lblRotulo);
            Grid.SetColumn(btnVerificar, 1);
            grid.Children.Add(btnVerificar);

            var painel = new StackPanel { Spacing = 6 };
            painel.Children.Add(grid);
            painel.Children.Add(new TextBlock
            {
                Text = Idioma.Texto("SecurityReport.BreachesNotChecked"),
                FontSize = 11,
                Foreground = Tema.Pincel(Tema.TextSecondary)
            });

            return new Border
            {
                Padding = new Thickness(14, 12),
                CornerRadius = new CornerRadius(10),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Child = painel
            };
        }

        private async Task VerificarVazamentosAsync(Button botao)
        {
            var conteudoOriginal = botao.Content;
            botao.IsEnabled = false;
            botao.Content = "…";

            try
            {
                _relatorio = await _reverificarVazamentos();
                _vazamentosVerificados = true;
                AtualizarConteudo();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("SecurityReport.BreachCheckError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Message.NetworkErrorTitle"), TipoMensagem.Erro);
                botao.Content = conteudoOriginal;
                botao.IsEnabled = true;
            }
        }

        private void SelecionarCategoria(CategoriaRelatorioSeguranca categoria)
        {
            CategoriaSelecionada = categoria;
            Close(true);
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
