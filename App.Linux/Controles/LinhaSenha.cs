using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GerenciadorDeSenhas.Modelos;

namespace AppLinux.Controles
{
    public class LinhaSenha : Border
    {
        private readonly Senha _senha;
        private readonly Func<Senha, string?> _obterSenhaPlain;
        private readonly Action<Senha> _onFavoritar;
        private readonly Action<Senha> _onEditar;

        private bool _revelada;
        private int _nivelForca = -1;
        private int _vazamentos = -1;

        private TextBlock _lblUsuario = null!;
        private TextBlock _lblIndicador = null!;
        private Button _btnOlho = null!;
        private Button _btnCopiar = null!;

        public Senha Senha => _senha;

        public int NivelForca
        {
            get => _nivelForca;
            set { _nivelForca = value; AtualizarIndicador(); }
        }

        public int Vazamentos
        {
            get => _vazamentos;
            set { _vazamentos = value; AtualizarIndicador(); }
        }

        private static readonly Color[] PaletaAvatar =
        {
            Color.FromUInt32(0xFF7C3AED),
            Color.FromUInt32(0xFF2563EB),
            Color.FromUInt32(0xFF16A34A),
            Color.FromUInt32(0xFFEA580C),
            Color.FromUInt32(0xFFDB2777),
            Color.FromUInt32(0xFF0891B2),
            Color.FromUInt32(0xFFCA8A04),
            Color.FromUInt32(0xFFDC2626),
        };

        public LinhaSenha(Senha senha, Func<Senha, string?> obterSenhaPlain,
            Action<Senha> onFavoritar, Action<Senha> onEditar)
        {
            _senha = senha;
            _obterSenhaPlain = obterSenhaPlain;
            _onFavoritar = onFavoritar;
            _onEditar = onEditar;

            Height = 58;
            Background = Tema.Pincel(Tema.CardBackground);
            BorderBrush = Tema.Pincel(Tema.Separator);
            BorderThickness = new Thickness(0, 0, 0, 1);

            Child = MontarLayout();
            AtualizarIndicador();

            PointerEntered += (s, e) => Background = Tema.Pincel(Tema.RowHover);
            PointerExited += (s, e) => Background = Tema.Pincel(Tema.CardBackground);
        }

        private Grid MontarLayout()
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("38,44,26,52*,48*,110,92,Auto"),
                Margin = new Thickness(4, 0, 10, 0)
            };

            var estrela = new TextBlock
            {
                Text = _senha.Favorito ? "★" : "☆",
                FontSize = 17,
                Foreground = Tema.Pincel(_senha.Favorito ? Tema.FavoriteColor : Tema.FavoriteBorderColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            estrela.PointerPressed += (s, e) => _onFavoritar(_senha);
            Grid.SetColumn(estrela, 0);
            grid.Children.Add(estrela);

            var avatar = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush(CorAvatar(_senha.NomeServico)),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(_senha.NomeServico)
                        ? "?" : _senha.NomeServico.Trim().Substring(0, 1).ToUpper(),
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(avatar, 1);
            grid.Children.Add(avatar);

            _lblIndicador = new TextBlock
            {
                Text = "",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_lblIndicador, 2);
            grid.Children.Add(_lblIndicador);

            var nome = new TextBlock
            {
                Text = _senha.NomeServico,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 4, 0)
            };
            Grid.SetColumn(nome, 3);
            grid.Children.Add(nome);

            _lblUsuario = new TextBlock
            {
                Text = _senha.Usuario,
                FontSize = 13,
                Foreground = Tema.Pincel(Tema.TextSecondary),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
            Grid.SetColumn(_lblUsuario, 4);
            grid.Children.Add(_lblUsuario);

            var (chipBg, chipFg, chipTexto) = InfoCategoria(_senha.Categoria);
            var chip = new Border
            {
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(chipBg),
                Padding = new Thickness(11, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = chipTexto,
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(chipFg),
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(chip, 5);
            grid.Children.Add(chip);

            var data = new TextBlock
            {
                Text = FormatarData(_senha.DataCriacao),
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.TextTertiary),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(data, 6);
            grid.Children.Add(data);

            _btnOlho = CriarBotaoAcao("👁");
            _btnOlho.Click += (s, e) => AlternarRevelar();

            _btnCopiar = CriarBotaoAcao("⧉");
            _btnCopiar.Click += async (s, e) => await CopiarAsync();

            var btnEditar = CriarBotaoAcao("✎");
            btnEditar.Click += (s, e) => _onEditar(_senha);

            var acoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                VerticalAlignment = VerticalAlignment.Center
            };
            acoes.Children.Add(_btnOlho);
            acoes.Children.Add(_btnCopiar);
            acoes.Children.Add(btnEditar);
            Grid.SetColumn(acoes, 7);
            grid.Children.Add(acoes);

            return grid;
        }

        private static Button CriarBotaoAcao(string icone)
        {
            var btn = new Button { Content = icone };
            btn.Classes.Add("icone-linha");
            return btn;
        }

        private void AtualizarIndicador()
        {
            if (_lblIndicador == null) return;

            if (_vazamentos > 0)
            {
                _lblIndicador.Text = "⚠";
                _lblIndicador.Foreground = Tema.Pincel(Color.FromUInt32(0xFFDC2626));
                ToolTip.SetTip(_lblIndicador, $"Senha comprometida — encontrada em {_vazamentos:N0} vazamento(s). Troque-a!");
                return;
            }

            string sufixo = _vazamentos == 0 ? " (não encontrada em vazamentos)" : "";
            switch (_nivelForca)
            {
                case 0:
                case 1:
                    _lblIndicador.Text = "●";
                    _lblIndicador.Foreground = Tema.Pincel(Tema.StrengthWeak);
                    ToolTip.SetTip(_lblIndicador, "Senha fraca" + sufixo);
                    break;
                case 2:
                    _lblIndicador.Text = "●";
                    _lblIndicador.Foreground = Tema.Pincel(Tema.StrengthMedium);
                    ToolTip.SetTip(_lblIndicador, "Senha média" + sufixo);
                    break;
                case 3:
                case 4:
                    _lblIndicador.Text = "●";
                    _lblIndicador.Foreground = Tema.Pincel(Tema.StrengthStrong);
                    ToolTip.SetTip(_lblIndicador, "Senha forte" + sufixo);
                    break;
                default:
                    _lblIndicador.Text = "";
                    ToolTip.SetTip(_lblIndicador, null);
                    break;
            }
        }

        private void AlternarRevelar()
        {
            _revelada = !_revelada;
            if (_revelada)
            {
                _lblUsuario.Text = _obterSenhaPlain(_senha) ?? "••••••••";
                _lblUsuario.FontFamily = (FontFamily)Application.Current!.FindResource("FonteMono")!;
                _lblUsuario.FontWeight = FontWeight.Bold;
                _lblUsuario.Foreground = Tema.Pincel(Tema.AccentPrimary);
                _btnOlho.Content = "🙈";
            }
            else
            {
                _lblUsuario.Text = _senha.Usuario;
                _lblUsuario.FontFamily = FontFamily.Default;
                _lblUsuario.FontWeight = FontWeight.Normal;
                _lblUsuario.Foreground = Tema.Pincel(Tema.TextSecondary);
                _btnOlho.Content = "👁";
            }
        }

        private async Task CopiarAsync()
        {
            var plain = _obterSenhaPlain(_senha);
            if (string.IsNullOrEmpty(plain)) return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(plain); } catch { }
            }

            _btnCopiar.Content = "✓";
            _btnCopiar.Foreground = Tema.Pincel(Tema.StrengthStrong);
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            t.Tick += (s, e) =>
            {
                _btnCopiar.Content = "⧉";
                _btnCopiar.ClearValue(Button.ForegroundProperty);
                t.Stop();
            };
            t.Start();
        }

        public static Color CorAvatar(string nome)
        {
            int hash = 0;
            foreach (char c in nome ?? "")
                hash = hash * 31 + c;
            return PaletaAvatar[Math.Abs(hash) % PaletaAvatar.Length];
        }

        private static (Color bg, Color fg, string texto) InfoCategoria(Categoria cat) => cat switch
        {
            Categoria.Personal => (Color.FromUInt32(0xFFEAF1FF), Color.FromUInt32(0xFF2563EB), "Pessoal"),
            Categoria.Work => (Color.FromUInt32(0xFFF1ECFE), Color.FromUInt32(0xFF7C3AED), "Trabalho"),
            Categoria.Finance => (Color.FromUInt32(0xFFE7F7EE), Color.FromUInt32(0xFF16A34A), "Finanças"),
            Categoria.Social => (Color.FromUInt32(0xFFFDEAF3), Color.FromUInt32(0xFFDB2777), "Social"),
            _ => (Color.FromUInt32(0xFFFDEEE0), Color.FromUInt32(0xFFEA580C), "Outro"),
        };

        private static string FormatarData(DateTime data)
        {
            var ptBR = CultureInfo.GetCultureInfo("pt-BR");
            return data.ToLocalTime().ToString("dd MMM yyyy", ptBR);
        }
    }
}
