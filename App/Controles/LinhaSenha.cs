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

namespace CofreDeSenhas.Controles
{
    public class LinhaSenha : Border
    {
        private readonly Senha _senha;
        private readonly Func<Senha, string?> _obterSenhaPlain;
        private readonly Func<Senha, string?> _obterTotpPlain;
        private readonly Func<Senha, Task> _onFavoritar;
        private readonly Func<Senha, Task> _onFixar;
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
        private string _dicaCategoriaReal = "";
        private Border _chipCategoria = null!;
        private StackPanel _painelCategoria = null!;
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
        private Button _btnEditar = null!;
        private Button _btnCopiar = null!;
        private Button _btnFixar = null!;
        private Button? _btnTotp;
        private DispatcherTimer? _timerFeedbackUsuario;
        private DispatcherTimer? _timerFeedbackSenha;
        private DispatcherTimer? _timerFeedbackTotp;

        // internal pra JanelaPrincipal reaproveitar a mesma máscara na lista da
        // lixeira, que não usa LinhaSenha (é montada com controles crus) mas
        // precisa respeitar o mesmo modo privacidade — ver CriarLinhaLixeira.
        internal const string MascaraPrivacidade = "••••••••";

        private bool _pointerSobre;

        public Senha Senha => _senha;
        public bool Selecionada { get; private set; }

        // Pra JanelaPrincipal conseguir preservar uma edição de nome de serviço ainda
        // não confirmada através de um rebuild da lista — ver AtualizarLista.
        //
        // !_salvandoServico importa: ConfirmarEdicaoServicoAsync já deixa
        // _editandoServico=true durante o próprio await de _onRenomearServico (que por
        // sua vez chama CarregarSenhasAsync/AtualizarLista de novo) — sem essa checagem,
        // o rebuild disparado pelo PRÓPRIO confirm confundia o commit em andamento com
        // uma digitação do usuário e reabria a edição por cima do nome já salvo.
        public bool EmEdicaoDeServico => _editandoServico && !_salvandoServico;
        public string? TextoServicoEmEdicao => EmEdicaoDeServico ? _txtServico.Text : null;

        public event EventHandler<Senha>? SolicitouDetalhes;
        public event EventHandler<Senha>? SelecaoAlterada;

        public void DefinirSelecionada(bool selecionada)
        {
            Selecionada = selecionada;
            AtualizarFundo();
            AutomationProperties.SetItemStatus(this, Idioma.Texto(selecionada ? "A11y.ToggleOn" : "A11y.ToggleOff"));
        }

        private void AtualizarFundo()
        {
            Background = Selecionada
                ? Tema.Pincel(Tema.AccentLight)
                : Tema.Pincel(_pointerSobre || IsFocused ? Tema.RowHover : Tema.CardBackground);
        }

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

            // Sem isto, "Editar" abria JanelaEditarSenha com usuário, URL, notas e
            // campos extras em texto puro — um jeito de um clique pra ver tudo que o
            // modo privacidade acabou de mascarar, sem precisar desligá-lo.
            _btnEditar.IsEnabled = !ativo;

            if (ativo && _editandoServico)
                CancelarEdicaoServico();

            RestaurarUsuarioOculto();
            AtualizarTextoServico();
            AtualizarTextoCategoria();
            AtualizarIndicador();
        }

        public LinhaSenha(Senha senha, Func<Senha, string?> obterSenhaPlain,
            Func<Senha, string?> obterTotpPlain, Func<Senha, Task> onFavoritar, Func<Senha, Task> onFixar, Action<Senha> onEditar,
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
                _pointerSobre = true;
                AtualizarFundo();
                AtualizarOpacidadeAcoes();
            };
            PointerExited += (s, e) =>
            {
                _pointerSobre = false;
                AtualizarFundo();
                AtualizarOpacidadeAcoes();
            };
            PointerReleased += Linha_PointerReleased;
            GotFocus += (s, e) =>
            {
                AtualizarFundo();
                AtualizarOpacidadeAcoes();
            };
            LostFocus += (s, e) =>
            {
                AtualizarFundo();
                AtualizarOpacidadeAcoes();
            };
            DetachedFromVisualTree += (s, e) =>
            {
                _timerFeedbackUsuario?.Stop();
                _timerFeedbackSenha?.Stop();
                _timerFeedbackTotp?.Stop();
            };
        }

        private void AtualizarOpacidadeAcoes()
        {
            if (_acoes != null)
                _acoes.Opacity = _pointerSobre || IsFocused ? 1 : 0.55;
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
                Content = CriarIconeEstrela(_senha.Favorito, 18),
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
            estrela.Click += async (s, e) =>
            {
                estrela.IsEnabled = false;
                try { await _onFavoritar(_senha); }
                finally { estrela.IsEnabled = true; }
            };
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
                    await CopiarCelulaUsuarioAsync();
                }
            };
            _lblUsuario.PointerPressed += async (s, e) =>
            {
                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    return;

                e.Handled = true;
                await CopiarCelulaUsuarioAsync();
            };
            Grid.SetColumn(_lblUsuario, 3);
            _grid.Children.Add(_lblUsuario);

            var organizacao = CriarCelulaOrganizacao();
            Grid.SetColumn(organizacao, 5);
            _grid.Children.Add(organizacao);

            var forca = CriarCelulaForca();
            Grid.SetColumn(forca, 7);
            _grid.Children.Add(forca);

            _btnOlho = CriarBotaoAcaoImagem("IconeRevelar", Idioma.Texto("Row.RevealPassword"));
            _btnOlho.Click += (s, e) => AlternarRevelar();

            _btnCopiar = CriarBotaoAcaoImagem("IconeCopiar", Idioma.Texto("Row.CopyPassword"));
            _btnCopiar.Click += async (s, e) => await CopiarAsync();

            _btnFixar = new Button { Content = CriarIconePin(_senha.Fixado, 18) };
            _btnFixar.Classes.Add("icone-linha");
            var dicaFixar = Idioma.Texto(_senha.Fixado ? "Row.UnpinEntry" : "Row.PinEntry");
            ToolTip.SetTip(_btnFixar, dicaFixar);
            AutomationProperties.SetName(_btnFixar, dicaFixar);
            _btnFixar.Click += async (s, e) =>
            {
                _btnFixar.IsEnabled = false;
                try { await _onFixar(_senha); }
                finally { _btnFixar.IsEnabled = true; }
            };

            _btnEditar = CriarBotaoAcaoImagem("IconeEditar", Idioma.Texto("Row.EditEntry"));
            _btnEditar.Click += (s, e) => _onEditar(_senha);

            var btnExcluir = CriarBotaoAcaoImagem("IconeExcluir", Idioma.Texto("Row.DeleteEntry"));
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
                _btnTotp = CriarBotaoAcaoImagem("IconeTotp", Idioma.Texto("Row.CopyTotp"));
                _btnTotp.Click += async (s, e) => await CopiarCodigoTotpAsync();
                _acoes.Children.Add(_btnTotp);
            }

            _acoes.Children.Add(_btnFixar);
            _acoes.Children.Add(_btnEditar);
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

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                DefinirSelecionada(!Selecionada);
                SelecaoAlterada?.Invoke(this, _senha);
                e.Handled = true;
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
            painel.Children.Add(chip);

            _dicaCategoriaReal = dica;
            _chipCategoria = chip;
            _painelCategoria = painel;
            AtualizarAcessibilidadeCategoria();

            return painel;
        }

        private void AtualizarAcessibilidadeCategoria()
        {
            var texto = _modoPrivacidade ? MascaraPrivacidade : _dicaCategoriaReal;
            ToolTip.SetTip(_chipCategoria, texto);
            AutomationProperties.SetName(_chipCategoria, texto.Replace('\n', ' '));
            ToolTip.SetTip(_painelCategoria, texto);
            AutomationProperties.SetName(_painelCategoria, texto.Replace('\n', ' '));
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
            var nome = _modoPrivacidade ? MascaraPrivacidade : _senha.NomeServico;
            _lblServico.Text = nome;
            AutomationProperties.SetName(_lblServico, $"{nome} — {Idioma.Texto("Row.EditService")}");
            AtualizarAvatarServico();
        }

        private void AtualizarTextoCategoria()
        {
            _lblCategoria.Text = _modoPrivacidade ? MascaraPrivacidade : _textoCategoriaReal;
            AtualizarAcessibilidadeCategoria();
        }

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

        // internal (não só private) e com o parâmetro opcional pra JanelaPrincipal
        // conseguir retomar uma edição preservada através de um rebuild da lista com o
        // texto que o usuário já tinha digitado — ver AtualizarLista/TextoServicoEmEdicao.
        internal void IniciarEdicaoServico(string? textoInicial = null)
        {
            if (_editandoServico || _salvandoServico || _modoPrivacidade)
                return;

            _editandoServico = true;
            _txtServico.Text = textoInicial ?? _senha.NomeServico;
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

        private static Button CriarBotaoAcaoImagem(string chave, string dica)
        {
            var btn = new Button { Content = Recursos.ImagemIcone(chave, 18) };
            btn.Classes.Add("icone-linha");
            ToolTip.SetTip(btn, dica);
            AutomationProperties.SetName(btn, dica);
            return btn;
        }

        private static Icone CriarIconeEstrela(bool favorito, double tamanho) =>
            Recursos.ImagemIcone("IconeFavoritas", tamanho,
                Tema.Pincel(favorito ? Tema.FavoriteColor : Tema.FavoriteBorderColor),
                preenchido: favorito);

        private static Icone CriarIconePin(bool fixado, double tamanho) =>
            Recursos.ImagemIcone("IconeFixar", tamanho,
                Tema.Pincel(fixado ? Tema.AccentPrimary : Tema.TextSecondary),
                preenchido: fixado);

        private static void DefinirIconeImagem(Button botao, string chave, IBrush? cor = null)
        {
            botao.Content = Recursos.ImagemIcone(chave, 18, cor);
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
            var partes = new List<string>();
            if (_modoPrivacidade)
            {
                partes.Add(MascaraPrivacidade);
            }
            else
            {
                partes.Add(_senha.NomeServico);
                if (!string.IsNullOrWhiteSpace(_senha.Usuario))
                    partes.Add(_senha.Usuario);
                partes.Add(CategoriasUI.Rotulo(_senha.Categoria));
            }
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
            DefinirIconeImagem(_btnOlho, "IconeOcultar");
            ToolTip.SetTip(_btnOlho, Idioma.Texto("Row.HidePassword"));
            AutomationProperties.SetName(_btnOlho, Idioma.Texto("Row.HidePassword"));
            ToolTip.SetTip(_lblUsuario, Idioma.Texto("Row.CopyPassword"));
            AutomationProperties.SetName(_lblUsuario,
                $"{Idioma.Texto("A11y.PasswordVisible")} — {Idioma.Texto("Row.CopyPassword")}");
        }

        private void RestaurarUsuarioOculto()
        {
            _lblUsuario.Text = _modoPrivacidade ? MascaraPrivacidade : _senha.Usuario;
            _lblUsuario.ClearValue(TextBlock.FontFamilyProperty);
            _lblUsuario.FontWeight = FontWeight.Normal;
            _lblUsuario.Foreground = Tema.Pincel(Tema.TextSecondary);
            DefinirIconeImagem(_btnOlho, "IconeRevelar");
            ToolTip.SetTip(_btnOlho, Idioma.Texto("Row.RevealPassword"));
            AutomationProperties.SetName(_btnOlho, Idioma.Texto("Row.RevealPassword"));
            ToolTip.SetTip(_lblUsuario, Idioma.Texto("Row.CopyUser"));
            var usuarioAcessivel = _modoPrivacidade ? MascaraPrivacidade : _senha.Usuario;
            AutomationProperties.SetName(_lblUsuario, $"{usuarioAcessivel} — {Idioma.Texto("Row.CopyUser")}");
        }

        private Task CopiarCelulaUsuarioAsync() => _revelada ? CopiarAsync() : CopiarUsuarioAsync();

        private Task RegistrarCopiaSeHabilitadoAsync(TipoCampoCopiado campo) =>
            _onRegistrarCopia != null && Preferencias.RegistrarHistoricoUso
                ? _onRegistrarCopia(_senha, campo)
                : Task.CompletedTask;

        internal async Task CopiarAsync()
        {
            var plain = _obterSenhaPlain(_senha);
            if (string.IsNullOrEmpty(plain)) return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            var (copiado, vaiLimpar) = await AreaTransferenciaFeedback.CopiarComAvisoAsync(clipboard, plain, this, Idioma.Texto("Row.CopyPassword"));
            if (!copiado) return;

            await RegistrarCopiaSeHabilitadoAsync(TipoCampoCopiado.Senha);

            DefinirIconeImagem(_btnCopiar, "IconeCheck", Tema.Pincel(Tema.StrengthStrong));

            if (vaiLimpar)
            {
                var mensagem = Idioma.Formatar("Row.PasswordCopiedClearing", Preferencias.SegundosLimpezaClipboard);
                ToolTip.SetTip(_btnCopiar, mensagem);
                AutomationProperties.SetName(_btnCopiar, mensagem);
            }

            _timerFeedbackSenha?.Stop();
            _timerFeedbackSenha = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerFeedbackSenha.Tick += (s, e) =>
            {
                DefinirIconeImagem(_btnCopiar, "IconeCopiar");
                if (vaiLimpar)
                {
                    ToolTip.SetTip(_btnCopiar, Idioma.Texto("Row.CopyPassword"));
                    AutomationProperties.SetName(_btnCopiar, Idioma.Texto("Row.CopyPassword"));
                }
                _timerFeedbackSenha?.Stop();
                _timerFeedbackSenha = null;
            };
            _timerFeedbackSenha.Start();
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
            var (copiado, vaiLimpar) = await AreaTransferenciaFeedback.CopiarComAvisoAsync(clipboard, codigo, this, Idioma.Texto("Row.CopyTotp"));
            if (!copiado) return;

            await RegistrarCopiaSeHabilitadoAsync(TipoCampoCopiado.Totp);

            DefinirIconeImagem(_btnTotp, "IconeCheck", Tema.Pincel(Tema.StrengthStrong));

            if (vaiLimpar)
            {
                var mensagem = Idioma.Formatar("Row.TotpCopiedClearing", Preferencias.SegundosLimpezaClipboard);
                ToolTip.SetTip(_btnTotp, mensagem);
                AutomationProperties.SetName(_btnTotp, mensagem);
            }

            _timerFeedbackTotp?.Stop();
            _timerFeedbackTotp = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerFeedbackTotp.Tick += (s, e) =>
            {
                DefinirIconeImagem(_btnTotp, "IconeTotp");
                if (vaiLimpar)
                {
                    ToolTip.SetTip(_btnTotp, Idioma.Texto("Row.CopyTotp"));
                    AutomationProperties.SetName(_btnTotp, Idioma.Texto("Row.CopyTotp"));
                }
                _timerFeedbackTotp?.Stop();
                _timerFeedbackTotp = null;
            };
            _timerFeedbackTotp.Start();
        }

        internal async Task CopiarUsuarioAsync()
        {
            if (string.IsNullOrWhiteSpace(_senha.Usuario))
                return;

            // Mesmo caminho de CopiarAsync/CopiarCodigoTotpAsync (AreaTransferenciaFeedback,
            // que agenda a limpeza automática do clipboard) — antes disto, copiar o
            // usuário ia direto pro clipboard.SetTextAsync cru, e só senha e TOTP eram
            // apagados sozinhos depois de alguns segundos. Usuário ficava esquecido lá,
            // muitas vezes o próprio e-mail da pessoa, disponível pra qualquer app ler
            // (inclusive histórico/nuvem de área de transferência do Windows).
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            var (copiado, vaiLimpar) = await AreaTransferenciaFeedback.CopiarComAvisoAsync(clipboard, _senha.Usuario, this, Idioma.Texto("Row.CopyUser"));
            if (!copiado) return;

            await RegistrarCopiaSeHabilitadoAsync(TipoCampoCopiado.Usuario);

            var textoCopiado = vaiLimpar
                ? Idioma.Formatar("Row.UserCopiedClearing", Preferencias.SegundosLimpezaClipboard)
                : Idioma.Texto("Row.UserCopied");

            _timerFeedbackUsuario?.Stop();
            _lblUsuario.Text = textoCopiado;
            _lblUsuario.ClearValue(TextBlock.FontFamilyProperty);
            _lblUsuario.FontWeight = FontWeight.Bold;
            _lblUsuario.Foreground = Tema.Pincel(Tema.StrengthStrong);
            ToolTip.SetTip(_lblUsuario, textoCopiado);
            AutomationProperties.SetName(_lblUsuario, textoCopiado);

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
