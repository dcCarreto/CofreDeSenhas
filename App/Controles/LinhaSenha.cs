using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace CofreDeSenhas.Controles
{
    public class LinhaSenha : Border
    {
        private readonly Senha _senha;
        private readonly Func<Senha, string?> _obterSenhaPlain;
        private readonly Func<Senha, string?> _obterTotpPlain;
        private readonly Action<Senha> _onFavoritar;
        private readonly Action<Senha> _onFixar;
        private readonly Action<Senha> _onEditar;
        private readonly Func<Senha, Task> _onExcluir;
        private readonly Func<Senha, string, Task> _onRenomearServico;
        private readonly Func<Senha, TipoCampoCopiado, Task>? _onRegistrarCopia;
        private readonly ServicoTotp _totp = new();

        private bool _revelada;
        private bool _editandoServico;
        private bool _salvandoServico;
        private bool _modoPrivacidade;
        private string _textoCategoriaReal = "";
        private int _versaoAvatar;
        private int _nivelForca = -1;
        private int _vazamentos = -1;
        private IReadOnlyCollection<TipoAchadoAuditoriaSenha> _achadosAuditoria =
            Array.Empty<TipoAchadoAuditoriaSenha>();
        private int _diasSemAtualizacao;
        private int _ocorrenciasSenhaRepetida;

        private TextBlock _lblUsuario = null!;
        private TextBlock _lblServico = null!;
        private TextBlock _lblCategoria = null!;
        private TextBox _txtServico = null!;
        private Border _avatar = null!;
        private Image _avatarImagem = null!;
        private TextBlock _avatarTexto = null!;
        private Grid _grid = null!;
        private StackPanel _acoes = null!;
        private StackPanel _painelForca = null!;
        private TextBlock _lblAuditoria = null!;
        private Border[] _segmentosForca = Array.Empty<Border>();
        private Button _btnOlho = null!;
        private Button _btnCopiar = null!;
        private Button _btnFixar = null!;
        private Button? _btnTotp;
        private DispatcherTimer? _timerFeedbackUsuario;

        private static readonly Geometry IconeCheck = StreamGeometry.Parse("M5 12 L10 17 L19 7");
        private const string MascaraPrivacidade = "••••••••";

        public Senha Senha => _senha;

        public event EventHandler<Senha>? SolicitouDetalhes;

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

        public void DefinirAuditoria(ItemAuditoriaSenha? item)
        {
            _achadosAuditoria = item?.Achados ?? Array.Empty<TipoAchadoAuditoriaSenha>();
            _diasSemAtualizacao = item?.DiasSemAtualizacao ?? 0;
            _ocorrenciasSenhaRepetida = item?.OcorrenciasSenhaRepetida ?? 0;
            AtualizarIndicador();
        }

        public void DefinirModoPrivacidade(bool ativo)
        {
            if (_modoPrivacidade == ativo)
                return;

            _modoPrivacidade = ativo;
            _revelada = false;
            _btnOlho.IsEnabled = !ativo;

            RestaurarUsuarioOculto();
            AtualizarTextoServico();
            AtualizarTextoCategoria();
        }

        public LinhaSenha(Senha senha, Func<Senha, string?> obterSenhaPlain,
            Func<Senha, string?> obterTotpPlain, Action<Senha> onFavoritar, Action<Senha> onFixar, Action<Senha> onEditar,
            Func<Senha, Task> onExcluir, Func<Senha, string, Task> onRenomearServico,
            Func<Senha, TipoCampoCopiado, Task>? onRegistrarCopia = null)
        {
            _senha = senha;
            _obterSenhaPlain = obterSenhaPlain;
            _obterTotpPlain = obterTotpPlain;
            _onFavoritar = onFavoritar;
            _onFixar = onFixar;
            _onEditar = onEditar;
            _onExcluir = onExcluir;
            _onRenomearServico = onRenomearServico;
            _onRegistrarCopia = onRegistrarCopia;

            Height = 52;
            Background = Tema.Pincel(Tema.CardBackground);
            BorderBrush = Tema.Pincel(Tema.Separator);
            BorderThickness = new Thickness(0, 0, 0, 1);
            Focusable = true;
            Transitions = new Transitions
            {
                new BrushTransition { Property = BackgroundProperty, Duration = TimeSpan.FromMilliseconds(120) }
            };

            Child = MontarLayout();
            AtualizarIndicador();
            AutomationProperties.SetHelpText(this, Idioma.Texto("A11y.RowHelp"));

            PointerEntered += (s, e) =>
            {
                if (!IsFocused) Background = Tema.Pincel(Tema.RowHover);
                if (_acoes != null) _acoes.Opacity = 1;
            };
            PointerExited += (s, e) =>
            {
                if (!IsFocused) Background = Tema.Pincel(Tema.CardBackground);
                if (_acoes != null) _acoes.Opacity = 0.55;
            };
            PointerReleased += Linha_PointerReleased;
            GotFocus += (s, e) =>
            {
                Background = Tema.Pincel(Tema.RowHover);
                if (_acoes != null) _acoes.Opacity = 1;
            };
            LostFocus += (s, e) =>
            {
                Background = Tema.Pincel(Tema.CardBackground);
                if (_acoes != null) _acoes.Opacity = 0.55;
            };
        }

        private Grid MontarLayout()
        {
            _grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("42,140,6,240,6,108,6,92,6,200"),
                Margin = new Thickness(4, 0, 8, 0)
            };

            var estrela = new Button
            {
                Content = CriarIconeEstrela(_senha.Favorito),
                Width = 30,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0)
            };
            estrela.Classes.Add("icone-linha");
            var dicaEstrela = _senha.Favorito
                ? Idioma.Texto("Row.FavoriteRemove")
                : Idioma.Texto("Row.FavoriteAdd");
            ToolTip.SetTip(estrela, dicaEstrela);
            AutomationProperties.SetName(estrela, dicaEstrela);
            AutomationProperties.SetItemStatus(estrela, Idioma.Texto(_senha.Favorito ? "A11y.ToggleOn" : "A11y.ToggleOff"));
            estrela.Click += (s, e) => _onFavoritar(_senha);
            Grid.SetColumn(estrela, 0);
            _grid.Children.Add(estrela);

            var celulaServico = CriarCelulaServico();
            Grid.SetColumn(celulaServico, 1);
            _grid.Children.Add(celulaServico);

            _lblUsuario = new TextBlock
            {
                Text = _senha.Usuario,
                FontSize = 13,
                Foreground = Tema.Pincel(Tema.TextSecondary),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 10, 0)
            };
            _lblUsuario.Cursor = new Cursor(StandardCursorType.Hand);
            _lblUsuario.Focusable = true;
            ToolTip.SetTip(_lblUsuario, Idioma.Texto("Row.CopyUser"));
            AutomationProperties.SetName(_lblUsuario, $"{_senha.Usuario} — {Idioma.Texto("Row.CopyUser")}");
            AutomationProperties.SetControlTypeOverride(_lblUsuario, AutomationControlType.Button);
            AutomationProperties.SetHelpText(_lblUsuario, Idioma.Texto("Row.CopyUser"));
            _lblUsuario.KeyDown += async (s, e) =>
            {
                if (e.Key is Key.Enter or Key.Space)
                {
                    e.Handled = true;
                    await CopiarUsuarioAsync();
                }
            };
            _lblUsuario.PointerPressed += async (s, e) =>
            {
                e.Handled = true;
                await CopiarUsuarioAsync();
            };
            Grid.SetColumn(_lblUsuario, 3);
            _grid.Children.Add(_lblUsuario);

            var organizacao = CriarCelulaOrganizacao();
            Grid.SetColumn(organizacao, 5);
            _grid.Children.Add(organizacao);

            var forca = CriarCelulaForca();
            Grid.SetColumn(forca, 7);
            _grid.Children.Add(forca);

            _btnOlho = CriarBotaoAcao(Icone("IconeRevelar"), Idioma.Texto("Row.RevealPassword"));
            _btnOlho.Click += (s, e) => AlternarRevelar();

            _btnCopiar = CriarBotaoAcao(Icone("IconeCopiar"), Idioma.Texto("Row.CopyPassword"));
            _btnCopiar.Click += async (s, e) => await CopiarAsync();

            _btnFixar = new Button { Content = CriarIconePin(_senha.Fixado) };
            _btnFixar.Classes.Add("icone-linha");
            var dicaFixar = Idioma.Texto(_senha.Fixado ? "Row.UnpinEntry" : "Row.PinEntry");
            ToolTip.SetTip(_btnFixar, dicaFixar);
            AutomationProperties.SetName(_btnFixar, dicaFixar);
            _btnFixar.Click += (s, e) => _onFixar(_senha);

            var btnEditar = CriarBotaoAcao(Icone("IconeEditar"), Idioma.Texto("Row.EditEntry"));
            btnEditar.Click += (s, e) => _onEditar(_senha);

            var btnExcluir = CriarBotaoAcao(Icone("IconeExcluir"), Idioma.Texto("Row.DeleteEntry"));
            btnExcluir.Classes.Add("excluir");
            btnExcluir.Click += async (s, e) => await _onExcluir(_senha);

            _acoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0.55,
                Transitions = new Transitions
                {
                    new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(120) }
                }
            };
            _acoes.Children.Add(_btnOlho);
            _acoes.Children.Add(_btnCopiar);

            if (_senha.TotpSegredo != null)
            {
                _btnTotp = CriarBotaoAcao(Icone("IconeTotp"), Idioma.Texto("Row.CopyTotp"));
                _btnTotp.Click += async (s, e) => await CopiarCodigoTotpAsync();
                _acoes.Children.Add(_btnTotp);
            }

            _acoes.Children.Add(_btnFixar);
            _acoes.Children.Add(btnEditar);
            _acoes.Children.Add(btnExcluir);
            Grid.SetColumn(_acoes, 9);
            _grid.Children.Add(_acoes);

            return _grid;
        }

        private void Linha_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Source is Visual visual &&
                (visual.FindAncestorOfType<Button>(true) != null ||
                 visual.FindAncestorOfType<TextBox>(true) != null ||
                 visual.FindAncestorOfType<TextBlock>(true) is { } texto && (texto == _lblServico || texto == _lblUsuario)))
            {
                return;
            }

            SolicitouDetalhes?.Invoke(this, _senha);
            e.Handled = true;
        }

        private Border CriarAvatarServico()
        {
            _avatarTexto = new TextBlock
            {
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _avatarImagem = new Image
            {
                Width = 28,
                Height = 28,
                Stretch = Stretch.Uniform,
                IsVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var conteudo = new Grid
            {
                Width = 30,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            conteudo.Children.Add(_avatarTexto);
            conteudo.Children.Add(_avatarImagem);

            var avatar = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(9),
                VerticalAlignment = VerticalAlignment.Center,
                Child = conteudo
            };
            _avatar = avatar;
            AtualizarAvatarServico();
            return avatar;
        }

        private Grid CriarCelulaServico()
        {
            var celula = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Margin = new Thickness(8, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _avatar = CriarAvatarServico();
            Grid.SetColumn(_avatar, 0);
            celula.Children.Add(_avatar);

            var textos = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _lblServico = new TextBlock
            {
                Text = _senha.NomeServico,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(_lblServico, Idioma.Texto("Row.EditService"));
            AutomationProperties.SetName(_lblServico, $"{_senha.NomeServico} — {Idioma.Texto("Row.EditService")}");
            AutomationProperties.SetHelpText(_lblServico, Idioma.Texto("A11y.EditServiceHelp"));
            AutomationProperties.SetControlTypeOverride(_lblServico, AutomationControlType.Button);
            _lblServico.KeyDown += (s, e) =>
            {
                if (e.Key is Key.Enter or Key.Space)
                {
                    e.Handled = true;
                    IniciarEdicaoServico();
                }
            };
            _lblServico.PointerPressed += (s, e) =>
            {
                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    return;

                e.Handled = true;
                IniciarEdicaoServico();
            };

            _txtServico = new TextBox
            {
                Text = _senha.NomeServico,
                IsVisible = false,
                Height = 30,
                MinHeight = 30,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            _txtServico.Classes.Add("embutido");
            AutomationProperties.SetName(_txtServico, Idioma.Texto("Entry.ServiceName"));
            AutomationProperties.SetHelpText(_txtServico, Idioma.Texto("A11y.EditServiceMode"));
            _txtServico.KeyDown += Servico_KeyDown;
            _txtServico.LostFocus += Servico_LostFocus;

            var data = new TextBlock
            {
                Text = FormatarData(_senha.DataCriacao),
                FontSize = 11,
                Foreground = Tema.Pincel(Tema.TextTertiary),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 3, 0, 0)
            };
            Grid.SetRow(data, 1);

            textos.Children.Add(_lblServico);
            textos.Children.Add(_txtServico);
            textos.Children.Add(data);
            Grid.SetColumn(textos, 1);
            celula.Children.Add(textos);
            return celula;
        }

        private StackPanel CriarCelulaForca()
        {
            _painelForca = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _segmentosForca = Enumerable.Range(0, 4)
                .Select(_ => new Border
                {
                    Width = 18,
                    Height = 5,
                    CornerRadius = new CornerRadius(3),
                    Background = Tema.Pincel(Tema.TrailInactive)
                })
                .ToArray();

            foreach (var segmento in _segmentosForca)
                _painelForca.Children.Add(segmento);

            _lblAuditoria = new TextBlock
            {
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 92,
                IsVisible = false,
                Margin = new Thickness(0, 3, 0, 0)
            };

            var celula = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            celula.Children.Add(_painelForca);
            celula.Children.Add(_lblAuditoria);
            return celula;
        }

        private StackPanel CriarCelulaOrganizacao()
        {
            var (chipBg, chipFg, textoCategoria) = InfoCategoria(_senha.Categoria);
            var temEtiquetas = _senha.Categoria == Categoria.Other && _senha.Etiquetas.Count > 0;
            var textoChip = temEtiquetas ? TextoResumoEtiquetas(_senha.Etiquetas) : textoCategoria;
            _textoCategoriaReal = textoChip;
            var dica = _senha.Etiquetas.Count > 0
                ? Idioma.Formatar("Row.TagsTooltip", Etiquetas.Formatar(_senha.Etiquetas), textoCategoria)
                : Idioma.Formatar("Row.CategoryTooltip", textoCategoria);

            var painel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            _lblCategoria = new TextBlock
            {
                Text = textoChip,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(chipFg),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 86,
                VerticalAlignment = VerticalAlignment.Center
            };

            var chip = new Border
            {
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(chipBg),
                Padding = new Thickness(9, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 104,
                Child = _lblCategoria
            };
            ToolTip.SetTip(chip, dica);
            AutomationProperties.SetName(chip, dica.Replace('\n', ' '));
            ToolTip.SetTip(painel, dica);
            AutomationProperties.SetName(painel, dica.Replace('\n', ' '));
            painel.Children.Add(chip);

            return painel;
        }

        private void AtualizarAvatarServico()
        {
            if (_avatar == null || _avatarTexto == null || _avatarImagem == null)
                return;

            if (_modoPrivacidade)
            {
                _versaoAvatar++;
                _avatar.Background = Tema.Pincel(Tema.TrailInactive);
                _avatar.BorderThickness = new Thickness(0);
                _avatarTexto.Text = "•";
                _avatarTexto.FontSize = TamanhoTextoIcone("•");
                _avatarTexto.Foreground = Tema.Pincel(Tema.TextSecondary);
                _avatarTexto.IsVisible = true;
                _avatarImagem.Source = null;
                _avatarImagem.IsVisible = false;
                ToolTip.SetTip(_avatar, MascaraPrivacidade);
                return;
            }

            var icone = IconesServico.Obter(_senha.NomeServico, _senha.Url);
            _avatar.Background = new SolidColorBrush(icone.Fundo);
            _avatar.BorderThickness = new Thickness(0);
            _avatarTexto.Text = icone.Texto;
            _avatarTexto.FontSize = TamanhoTextoIcone(icone.Texto);
            _avatarTexto.Foreground = new SolidColorBrush(icone.Frente);
            _avatarTexto.IsVisible = true;
            _avatarImagem.Source = null;
            _avatarImagem.IsVisible = false;
            ToolTip.SetTip(_avatar, _senha.NomeServico);

            int versao = ++_versaoAvatar;
            _ = CarregarAvatarServicoAsync(icone, versao);
        }

        private void AtualizarTextoServico()
        {
            _lblServico.Text = _modoPrivacidade ? MascaraPrivacidade : _senha.NomeServico;
            AtualizarAvatarServico();
        }

        private void AtualizarTextoCategoria() =>
            _lblCategoria.Text = _modoPrivacidade ? MascaraPrivacidade : _textoCategoriaReal;

        private async Task CarregarAvatarServicoAsync(IconeServico icone, int versao)
        {
            var bitmap = await IconesServico.ObterBitmapAsync(icone);
            if (bitmap == null)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (versao != _versaoAvatar)
                    return;

                _avatarImagem.Source = bitmap;
                _avatarImagem.IsVisible = true;
                _avatarTexto.IsVisible = false;
                _avatar.Background = new SolidColorBrush(Color.FromUInt32(0xFFFFFFFF));
                _avatar.BorderBrush = Tema.Pincel(Tema.CardBorder);
                _avatar.BorderThickness = new Thickness(1);
            });
        }

        private static double TamanhoTextoIcone(string texto) => texto.Length switch
        {
            <= 1 => 17,
            2 => 14,
            _ => 11
        };

        private void IniciarEdicaoServico()
        {
            if (_editandoServico || _salvandoServico || _modoPrivacidade)
                return;

            _editandoServico = true;
            _txtServico.Text = _senha.NomeServico;
            _lblServico.IsVisible = false;
            _txtServico.IsVisible = true;
            Acessibilidade.Anunciar(this, Idioma.Texto("A11y.EditServiceMode"));

            Dispatcher.UIThread.Post(() =>
            {
                _txtServico.Focus();
                _txtServico.SelectAll();
            }, DispatcherPriority.Input);
        }

        private async void Servico_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await ConfirmarEdicaoServicoAsync();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CancelarEdicaoServico();
            }
        }

        private async void Servico_LostFocus(object? sender, RoutedEventArgs e)
        {
            if (_editandoServico)
                await ConfirmarEdicaoServicoAsync();
        }

        private async Task ConfirmarEdicaoServicoAsync()
        {
            if (!_editandoServico || _salvandoServico)
                return;

            _salvandoServico = true;
            string nomeAnterior = _senha.NomeServico;
            string novoNome = (_txtServico.Text ?? "").Trim();

            try
            {
                if (string.IsNullOrWhiteSpace(novoNome))
                {
                    _txtServico.Text = nomeAnterior;
                    EncerrarEdicaoServico();
                    return;
                }

                if (!string.Equals(nomeAnterior, novoNome, StringComparison.Ordinal))
                {
                    _txtServico.IsEnabled = false;
                    await _onRenomearServico(_senha, novoNome);
                    _senha.NomeServico = novoNome;
                    _lblServico.Text = novoNome;
                    AutomationProperties.SetName(_lblServico, $"{novoNome} — {Idioma.Texto("Row.EditService")}");
                    AtualizarAvatarServico();
                }

                EncerrarEdicaoServico();
            }
            catch
            {
                _txtServico.Text = nomeAnterior;
                EncerrarEdicaoServico();
            }
            finally
            {
                _txtServico.IsEnabled = true;
                _salvandoServico = false;
            }
        }

        private void CancelarEdicaoServico()
        {
            if (!_editandoServico)
                return;

            _txtServico.Text = _senha.NomeServico;
            EncerrarEdicaoServico();
        }

        private void EncerrarEdicaoServico()
        {
            _editandoServico = false;
            _txtServico.IsVisible = false;
            _lblServico.IsVisible = true;
            AutomationProperties.SetName(_lblServico, $"{_senha.NomeServico} — {Idioma.Texto("Row.EditService")}");
        }

        public void DefinirLargurasColunas(double servico, double usuario, double categoria, double data, double acoes)
        {
            if (_grid == null)
                return;

            _grid.ColumnDefinitions[1].Width = new GridLength(servico);
            _grid.ColumnDefinitions[3].Width = new GridLength(usuario);
            _grid.ColumnDefinitions[5].Width = new GridLength(categoria);
            _grid.ColumnDefinitions[7].Width = new GridLength(data);
            _grid.ColumnDefinitions[9].Width = new GridLength(acoes);
        }

        private static Geometry Icone(string chave) => (Geometry)Application.Current!.FindResource(chave)!;

        private static Button CriarBotaoAcao(Geometry icone, string dica)
        {
            var btn = new Button { Content = CriarIcone(icone) };
            btn.Classes.Add("icone-linha");
            ToolTip.SetTip(btn, dica);
            AutomationProperties.SetName(btn, dica);
            return btn;
        }

        private static AvaloniaPath CriarIcone(Geometry data, IBrush? stroke = null) => new()
        {
            Data = data,
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            Stroke = stroke ?? Tema.Pincel(Tema.TextSecondary),
            StrokeThickness = 2,
            StrokeLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent
        };

        private static AvaloniaPath CriarIconeEstrela(bool favorito) => new()
        {
            Data = Icone("IconeFavoritas"),
            Width = 16,
            Height = 16,
            Stretch = Stretch.Uniform,
            Stroke = Tema.Pincel(favorito ? Tema.FavoriteColor : Tema.FavoriteBorderColor),
            StrokeThickness = 1.8,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Fill = favorito ? Tema.Pincel(Tema.FavoriteColor) : Brushes.Transparent
        };

        private static AvaloniaPath CriarIconePin(bool fixado) => new()
        {
            Data = Icone("IconeFixar"),
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            Stroke = Tema.Pincel(fixado ? Tema.AccentPrimary : Tema.TextSecondary),
            StrokeThickness = 1.6,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Fill = fixado ? Tema.Pincel(Tema.AccentPrimary) : Brushes.Transparent
        };

        private static void DefinirIcone(Button botao, Geometry data)
        {
            botao.Content = CriarIcone(data);
        }

        private void AtualizarIndicador()
        {
            if (_painelForca == null || _segmentosForca.Length == 0) return;

            if (_vazamentos > 0)
            {
                var descricao = Idioma.Formatar("Row.PasswordCompromised", _vazamentos);
                DefinirForcaVisual(1, Tema.StrengthWeak, descricao);
                DefinirRotuloAuditoria(descricao, Tema.StrengthWeak);
                AtualizarNomeAcessivel(descricao);
                return;
            }

            if (_achadosAuditoria.Count > 0)
            {
                bool critico = _achadosAuditoria.Contains(TipoAchadoAuditoriaSenha.Fraca)
                    || _achadosAuditoria.Contains(TipoAchadoAuditoriaSenha.Repetida);
                var corAuditoria = critico ? Tema.StrengthWeak : Tema.StrengthMedium;
                var descricao = Idioma.Formatar("Row.AuditPrefix", string.Join("; ", DescreverAchadosAuditoria()));
                DefinirForcaVisual(critico ? 1 : 2, corAuditoria, descricao);
                DefinirRotuloAuditoria(RotuloAuditoriaCurto(), corAuditoria);
                AtualizarNomeAcessivel(descricao);
                return;
            }

            DefinirRotuloAuditoria(null, Tema.TextSecondary);
            string sufixo = _vazamentos == 0 ? Idioma.Texto("Row.NotFoundInBreaches") : "";
            switch (_nivelForca)
            {
                case 0:
                case 1:
                    DefinirForcaVisual(1, Tema.StrengthWeak, Idioma.Texto("Row.PasswordWeak"), sufixo);
                    break;
                case 2:
                    DefinirForcaVisual(2, Tema.StrengthMedium, Idioma.Texto("Row.PasswordMedium"), sufixo);
                    break;
                case 3:
                    DefinirForcaVisual(3, Tema.StrengthStrong, Idioma.Texto("Row.PasswordStrong"), sufixo);
                    break;
                case 4:
                    DefinirForcaVisual(4, Tema.StrengthExcellent, Idioma.Texto("Generator.StrengthExcellent"), sufixo);
                    break;
                default:
                    DefinirForcaVisual(0, Tema.TrailInactive, "");
                    AtualizarNomeAcessivel(null);
                    break;
            }
        }

        private void DefinirForcaVisual(int nivel, Color cor, string rotulo, string sufixo = "")
        {
            for (int i = 0; i < _segmentosForca.Length; i++)
                _segmentosForca[i].Background = Tema.Pincel(i < nivel ? cor : Tema.TrailInactive);

            var descricao = rotulo + sufixo;
            ToolTip.SetTip(_painelForca, string.IsNullOrWhiteSpace(descricao) ? null : descricao);
            AutomationProperties.SetName(_painelForca, descricao);
            AtualizarNomeAcessivel(rotulo);
        }

        private void AtualizarNomeAcessivel(string? status)
        {
            var partes = new List<string> { _senha.NomeServico };
            if (!string.IsNullOrWhiteSpace(_senha.Usuario))
                partes.Add(_senha.Usuario);
            partes.Add(CategoriasUI.Rotulo(_senha.Categoria));
            if (!string.IsNullOrWhiteSpace(status))
                partes.Add(status!);
            AutomationProperties.SetName(this, string.Join(". ", partes));
            AutomationProperties.SetHelpText(this, Idioma.Texto("A11y.RowHelp"));
        }

        private IEnumerable<string> DescreverAchadosAuditoria()
        {
            foreach (var achado in _achadosAuditoria)
            {
                yield return achado switch
                {
                    TipoAchadoAuditoriaSenha.Fraca => Idioma.Texto("Row.AuditWeak"),
                    TipoAchadoAuditoriaSenha.Repetida when _ocorrenciasSenhaRepetida > 0 =>
                        Idioma.Formatar("Row.AuditRepeatedWithCount", _ocorrenciasSenhaRepetida),
                    TipoAchadoAuditoriaSenha.Repetida => Idioma.Texto("Row.AuditRepeated"),
                    TipoAchadoAuditoriaSenha.Antiga => Idioma.Formatar("Row.AuditOld", _diasSemAtualizacao),
                    _ => Idioma.Texto("Row.AuditAlert")
                };
            }
        }

        private void DefinirRotuloAuditoria(string? texto, Color cor)
        {
            if (_lblAuditoria == null)
                return;

            if (string.IsNullOrWhiteSpace(texto))
            {
                _lblAuditoria.IsVisible = false;
                _lblAuditoria.Text = "";
                return;
            }

            _lblAuditoria.Text = texto;
            _lblAuditoria.Foreground = Tema.Pincel(cor);
            _lblAuditoria.IsVisible = true;
        }

        private string RotuloAuditoriaCurto()
        {
            foreach (var achado in _achadosAuditoria)
            {
                return achado switch
                {
                    TipoAchadoAuditoriaSenha.Fraca => Idioma.Texto("Row.AuditWeak"),
                    TipoAchadoAuditoriaSenha.Repetida => Idioma.Texto("Row.AuditRepeated"),
                    TipoAchadoAuditoriaSenha.Antiga => Idioma.Formatar("Row.AuditOld", _diasSemAtualizacao),
                    _ => Idioma.Texto("Row.AuditAlert")
                };
            }

            return "";
        }

        private void AlternarRevelar()
        {
            _revelada = !_revelada;
            if (_revelada)
            {
                MostrarSenhaRevelada();
                Acessibilidade.Anunciar(this, Idioma.Texto("A11y.PasswordVisible"));
            }
            else
            {
                RestaurarUsuarioOculto();
                Acessibilidade.Anunciar(this, Idioma.Texto("A11y.PasswordHidden"));
            }
        }

        public void EsconderSenhaSeRevelada()
        {
            if (!_revelada)
                return;

            _revelada = false;
            RestaurarUsuarioOculto();
        }

        private void MostrarSenhaRevelada()
        {
            _lblUsuario.Text = _obterSenhaPlain(_senha) ?? "••••••••";
            _lblUsuario.FontFamily = (FontFamily)Application.Current!.FindResource("FonteMono")!;
            _lblUsuario.FontWeight = FontWeight.Bold;
            _lblUsuario.Foreground = Tema.Pincel(Tema.AccentText);
            DefinirIcone(_btnOlho, Icone("IconeOcultar"));
            ToolTip.SetTip(_btnOlho, Idioma.Texto("Row.HidePassword"));
            AutomationProperties.SetName(_btnOlho, Idioma.Texto("Row.HidePassword"));
            AutomationProperties.SetName(_lblUsuario, Idioma.Texto("A11y.PasswordVisible"));
        }

        private void RestaurarUsuarioOculto()
        {
            _lblUsuario.Text = _modoPrivacidade ? MascaraPrivacidade : _senha.Usuario;
            _lblUsuario.ClearValue(TextBlock.FontFamilyProperty);
            _lblUsuario.FontWeight = FontWeight.Normal;
            _lblUsuario.Foreground = Tema.Pincel(Tema.TextSecondary);
            DefinirIcone(_btnOlho, Icone("IconeRevelar"));
            ToolTip.SetTip(_btnOlho, Idioma.Texto("Row.RevealPassword"));
            AutomationProperties.SetName(_btnOlho, Idioma.Texto("Row.RevealPassword"));
            AutomationProperties.SetName(_lblUsuario, $"{_senha.Usuario} — {Idioma.Texto("Row.CopyUser")}");
        }

        private Task RegistrarCopiaSeHabilitadoAsync(TipoCampoCopiado campo) =>
            _onRegistrarCopia != null && Preferencias.RegistrarHistoricoUso
                ? _onRegistrarCopia(_senha, campo)
                : Task.CompletedTask;

        internal async Task CopiarAsync()
        {
            var plain = _obterSenhaPlain(_senha);
            if (string.IsNullOrEmpty(plain)) return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            var vaiLimpar = await AreaTransferenciaFeedback.CopiarComAvisoAsync(clipboard, plain, this, Idioma.Texto("Row.CopyPassword"));
            await RegistrarCopiaSeHabilitadoAsync(TipoCampoCopiado.Senha);

            DefinirIcone(_btnCopiar, IconeCheck);
            _btnCopiar.Foreground = Tema.Pincel(Tema.StrengthStrong);

            if (vaiLimpar)
            {
                var mensagem = Idioma.Formatar("Row.PasswordCopiedClearing", Preferencias.SegundosLimpezaClipboard);
                ToolTip.SetTip(_btnCopiar, mensagem);
                AutomationProperties.SetName(_btnCopiar, mensagem);
            }

            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            t.Tick += (s, e) =>
            {
                DefinirIcone(_btnCopiar, Icone("IconeCopiar"));
                _btnCopiar.ClearValue(Button.ForegroundProperty);
                if (vaiLimpar)
                {
                    ToolTip.SetTip(_btnCopiar, Idioma.Texto("Row.CopyPassword"));
                    AutomationProperties.SetName(_btnCopiar, Idioma.Texto("Row.CopyPassword"));
                }
                t.Stop();
            };
            t.Start();
        }

        private async Task CopiarCodigoTotpAsync()
        {
            if (_btnTotp == null)
                return;

            var segredo = _obterTotpPlain(_senha);
            if (string.IsNullOrEmpty(segredo))
                return;

            string codigo;
            try { codigo = _totp.Gerar(segredo).Codigo; }
            catch { return; }

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(codigo); } catch { }
            }
            await RegistrarCopiaSeHabilitadoAsync(TipoCampoCopiado.Totp);

            DefinirIcone(_btnTotp, IconeCheck);
            _btnTotp.Foreground = Tema.Pincel(Tema.StrengthStrong);
            Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", Idioma.Texto("Row.CopyTotp")));
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            t.Tick += (s, e) =>
            {
                DefinirIcone(_btnTotp, Icone("IconeTotp"));
                _btnTotp.ClearValue(Button.ForegroundProperty);
                t.Stop();
            };
            t.Start();
        }

        internal async Task CopiarUsuarioAsync()
        {
            if (string.IsNullOrWhiteSpace(_senha.Usuario))
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(_senha.Usuario); } catch { }
            }
            await RegistrarCopiaSeHabilitadoAsync(TipoCampoCopiado.Usuario);

            _timerFeedbackUsuario?.Stop();
            _lblUsuario.Text = Idioma.Texto("Row.UserCopied");
            _lblUsuario.ClearValue(TextBlock.FontFamilyProperty);
            _lblUsuario.FontWeight = FontWeight.Bold;
            _lblUsuario.Foreground = Tema.Pincel(Tema.StrengthStrong);
            ToolTip.SetTip(_lblUsuario, Idioma.Texto("Row.UserCopied"));
            AutomationProperties.SetName(_lblUsuario, Idioma.Texto("Row.UserCopied"));
            Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", Idioma.Texto("Row.CopyUser")));

            _timerFeedbackUsuario = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1100) };
            _timerFeedbackUsuario.Tick += (s, e) =>
            {
                RestaurarTextoUsuario();
                ToolTip.SetTip(_lblUsuario, Idioma.Texto("Row.CopyUser"));
                _timerFeedbackUsuario?.Stop();
                _timerFeedbackUsuario = null;
            };
            _timerFeedbackUsuario.Start();
        }

        private void RestaurarTextoUsuario()
        {
            if (_revelada)
            {
                MostrarSenhaRevelada();
                return;
            }

            RestaurarUsuarioOculto();
        }

        private static string TextoResumoEtiquetas(IReadOnlyList<string> etiquetas)
        {
            if (etiquetas.Count == 0)
                return string.Empty;

            var primeira = etiquetas[0];
            return etiquetas.Count == 1 ? primeira : $"{primeira} +{etiquetas.Count - 1}";
        }

        private static (Color bg, Color fg, string texto) InfoCategoria(Categoria cat)
        {
            var categoria = cat switch
            {
                Categoria.Personal => Categoria.Personal,
                Categoria.Work => Categoria.Work,
                Categoria.Finance => Categoria.Finance,
                Categoria.Social => Categoria.Social,
                _ => Categoria.Other
            };
            var (bg, fg) = Acessibilidade.CoresCategoria(categoria);
            return (bg, fg, CategoriasUI.Rotulo(categoria));
        }

        private static string FormatarData(DateTime data)
        {
            return data.ToLocalTime().ToString("dd MMM yyyy", Idioma.CulturaAtual);
        }
    }
}
