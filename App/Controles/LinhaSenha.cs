using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Controles
{
    public class LinhaSenha : Border
    {
        private readonly Senha _senha;
        private readonly Func<Senha, string?> _obterSenhaPlain;
        private readonly Func<Senha, string?> _obterTotpPlain;
        private readonly Action<Senha> _onFavoritar;
        private readonly Action<Senha> _onEditar;
        private readonly Func<Senha, Task> _onExcluir;
        private readonly Func<Senha, string, Task> _onRenomearServico;
        private readonly ServicoTotp _totp = new();

        private bool _revelada;
        private bool _editandoServico;
        private bool _salvandoServico;
        private int _versaoAvatar;
        private int _nivelForca = -1;
        private int _vazamentos = -1;
        private IReadOnlyCollection<TipoAchadoAuditoriaSenha> _achadosAuditoria =
            Array.Empty<TipoAchadoAuditoriaSenha>();
        private int _diasSemAtualizacao;
        private int _ocorrenciasSenhaRepetida;

        private TextBlock _lblUsuario = null!;
        private TextBlock _lblServico = null!;
        private TextBlock _lblIndicador = null!;
        private TextBox _txtServico = null!;
        private Border _avatar = null!;
        private Image _avatarImagem = null!;
        private TextBlock _avatarTexto = null!;
        private Grid _grid = null!;
        private Button _btnOlho = null!;
        private Button _btnCopiar = null!;
        private Button? _btnTotp;
        private DispatcherTimer? _timerFeedbackUsuario;

        private const string IconeOlho =
            "M2.5 12 C4.8 7.5 8.1 5.5 12 5.5 C15.9 5.5 19.2 7.5 21.5 12 C19.2 16.5 15.9 18.5 12 18.5 C8.1 18.5 4.8 16.5 2.5 12 Z M12 15.5 C13.9 15.5 15.5 13.9 15.5 12 C15.5 10.1 13.9 8.5 12 8.5 C10.1 8.5 8.5 10.1 8.5 12 C8.5 13.9 10.1 15.5 12 15.5 Z";
        private const string IconeOlhoFechado =
            "M4 4 L20 20 M6.2 6.9 C4.7 8 3.5 9.7 2.5 12 C4.8 16.5 8.1 18.5 12 18.5 C13.2 18.5 14.3 18.3 15.3 17.8 M9.1 9.1 C8.7 9.7 8.5 10.8 8.5 12 C8.5 13.9 10.1 15.5 12 15.5 C13.2 15.5 14.2 14.9 14.8 14 M10.1 5.7 C10.7 5.6 11.3 5.5 12 5.5 C15.9 5.5 19.2 7.5 21.5 12 C20.8 13.4 19.9 14.6 18.9 15.6";
        private const string IconeCopiar =
            "M8 8 L18 8 L18 20 L8 20 Z M6 16 L4 16 L4 4 L14 4 L14 6";
        private const string IconeEditar =
            "M4 17 L4 20 L7 20 L18.5 8.5 L15.5 5.5 Z M14.8 6.2 L17.8 9.2";
        private const string IconeCheck =
            "M5 12 L10 17 L19 7";
        private const string IconeChave =
            "M12 8 m-4 0 a4 4 0 1 0 8 0 a4 4 0 1 0 -8 0 M12 8 m-1.5 0 a1.5 1.5 0 1 0 3 0 a1.5 1.5 0 1 0 -3 0 M12 12 L12 20 M12 16 L15 16 M12 19 L14 19";
        private const string IconeExcluir =
            "M5 6 L19 6 M9 6 L9 4 L15 4 L15 6 M8 9 L8.7 20 L15.3 20 L16 9 M10.5 11 L10.5 18 M13.5 11 L13.5 18";

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

        public void DefinirAuditoria(ItemAuditoriaSenha? item)
        {
            _achadosAuditoria = item?.Achados ?? Array.Empty<TipoAchadoAuditoriaSenha>();
            _diasSemAtualizacao = item?.DiasSemAtualizacao ?? 0;
            _ocorrenciasSenhaRepetida = item?.OcorrenciasSenhaRepetida ?? 0;
            AtualizarIndicador();
        }

        public LinhaSenha(Senha senha, Func<Senha, string?> obterSenhaPlain,
            Func<Senha, string?> obterTotpPlain, Action<Senha> onFavoritar, Action<Senha> onEditar,
            Func<Senha, Task> onExcluir, Func<Senha, string, Task> onRenomearServico)
        {
            _senha = senha;
            _obterSenhaPlain = obterSenhaPlain;
            _obterTotpPlain = obterTotpPlain;
            _onFavoritar = onFavoritar;
            _onEditar = onEditar;
            _onExcluir = onExcluir;
            _onRenomearServico = onRenomearServico;

            Height = 64;
            Background = Tema.Pincel(Tema.CardBackground);
            BorderBrush = Tema.Pincel(Tema.Separator);
            BorderThickness = new Thickness(0, 0, 0, 1);
            Focusable = true;

            Child = MontarLayout();
            AtualizarIndicador();
            AutomationProperties.SetHelpText(this, Idioma.Texto("A11y.RowHelp"));

            PointerEntered += (s, e) => { if (!IsFocused) Background = Tema.Pincel(Tema.RowHover); };
            PointerExited += (s, e) => { if (!IsFocused) Background = Tema.Pincel(Tema.CardBackground); };
            GotFocus += (s, e) => Background = Tema.Pincel(Tema.RowHover);
            LostFocus += (s, e) => Background = Tema.Pincel(Tema.CardBackground);
        }

        private Grid MontarLayout()
        {
            _grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("42,44,26,140,240,108,92,96"),
                Margin = new Thickness(4, 0, 8, 0)
            };

            var estrela = new Button
            {
                Content = _senha.Favorito ? "★" : "☆",
                Width = 30,
                Height = 30,
                FontSize = 17,
                Foreground = Tema.Pincel(_senha.Favorito ? Tema.FavoriteColor : Tema.FavoriteBorderColor),
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

            _avatar = CriarAvatarServico();
            Grid.SetColumn(_avatar, 1);
            _grid.Children.Add(_avatar);

            _lblIndicador = new TextBlock
            {
                Text = "",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_lblIndicador, 2);
            _grid.Children.Add(_lblIndicador);

            var celulaServico = CriarCelulaServico();
            Grid.SetColumn(celulaServico, 3);
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
            Grid.SetColumn(_lblUsuario, 4);
            _grid.Children.Add(_lblUsuario);

            var organizacao = CriarCelulaOrganizacao();
            Grid.SetColumn(organizacao, 5);
            _grid.Children.Add(organizacao);

            var data = new TextBlock
            {
                Text = FormatarData(_senha.DataCriacao),
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.TextTertiary),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(data, 6);
            _grid.Children.Add(data);

            _btnOlho = CriarBotaoAcao(IconeOlho, Idioma.Texto("Row.RevealPassword"));
            _btnOlho.Click += (s, e) => AlternarRevelar();

            _btnCopiar = CriarBotaoAcao(IconeCopiar, Idioma.Texto("Row.CopyPassword"));
            _btnCopiar.Click += async (s, e) => await CopiarAsync();

            var btnEditar = CriarBotaoAcao(IconeEditar, Idioma.Texto("Row.EditEntry"));
            btnEditar.Click += (s, e) => _onEditar(_senha);

            var btnExcluir = CriarBotaoAcao(IconeExcluir, Idioma.Texto("Row.DeleteEntry"));
            btnExcluir.Click += async (s, e) => await _onExcluir(_senha);

            var acoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center
            };
            acoes.Children.Add(_btnOlho);
            acoes.Children.Add(_btnCopiar);

            if (_senha.TotpSegredo != null)
            {
                _btnTotp = CriarBotaoAcao(IconeChave, Idioma.Texto("Row.CopyTotp"));
                _btnTotp.Click += async (s, e) => await CopiarCodigoTotpAsync();
                acoes.Children.Add(_btnTotp);
            }

            acoes.Children.Add(btnEditar);
            acoes.Children.Add(btnExcluir);
            Grid.SetColumn(acoes, 7);
            _grid.Children.Add(acoes);

            return _grid;
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
                Margin = new Thickness(10, 0, 8, 0),
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

            celula.Children.Add(_lblServico);
            celula.Children.Add(_txtServico);
            return celula;
        }

        private StackPanel CriarCelulaOrganizacao()
        {
            var (chipBg, chipFg, textoCategoria) = InfoCategoria(_senha.Categoria);
            var temEtiquetas = _senha.Categoria == Categoria.Other && _senha.Etiquetas.Count > 0;
            var textoChip = temEtiquetas ? TextoResumoEtiquetas(_senha.Etiquetas) : textoCategoria;
            var dica = temEtiquetas
                ? Idioma.Formatar("Row.TagsTooltip", Etiquetas.Formatar(_senha.Etiquetas), textoCategoria)
                : Idioma.Formatar("Row.CategoryTooltip", textoCategoria);

            var painel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            var chip = new Border
            {
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(chipBg),
                Padding = new Thickness(9, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 104,
                Child = new TextBlock
                {
                    Text = textoChip,
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(chipFg),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 86,
                    VerticalAlignment = VerticalAlignment.Center
                }
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
            if (_editandoServico || _salvandoServico)
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

            _grid.ColumnDefinitions[3].Width = new GridLength(servico);
            _grid.ColumnDefinitions[4].Width = new GridLength(usuario);
            _grid.ColumnDefinitions[5].Width = new GridLength(categoria);
            _grid.ColumnDefinitions[6].Width = new GridLength(data);
            _grid.ColumnDefinitions[7].Width = new GridLength(acoes);
        }

        private static Button CriarBotaoAcao(string icone, string dica)
        {
            var btn = new Button { Content = CriarIcone(icone) };
            btn.Classes.Add("icone-linha");
            ToolTip.SetTip(btn, dica);
            AutomationProperties.SetName(btn, dica);
            return btn;
        }

        private static PathIcon CriarIcone(string data) => new()
        {
            Data = StreamGeometry.Parse(data),
            Width = 14,
            Height = 14
        };

        private static void DefinirIcone(Button botao, string data)
        {
            botao.Content = CriarIcone(data);
        }

        private void AtualizarIndicador()
        {
            if (_lblIndicador == null) return;

            if (_vazamentos > 0)
            {
                var descricao = Idioma.Formatar("Row.PasswordCompromised", _vazamentos);
                _lblIndicador.Text = "⚠";
                _lblIndicador.Foreground = Tema.Pincel(Tema.StrengthWeak);
                ToolTip.SetTip(_lblIndicador, descricao);
                AutomationProperties.SetName(_lblIndicador, descricao);
                AtualizarNomeAcessivel(descricao);
                return;
            }

            if (_achadosAuditoria.Count > 0)
            {
                bool critico = _achadosAuditoria.Contains(TipoAchadoAuditoriaSenha.Fraca)
                    || _achadosAuditoria.Contains(TipoAchadoAuditoriaSenha.Repetida);
                var descricao = Idioma.Formatar("Row.AuditPrefix", string.Join("; ", DescreverAchadosAuditoria()));
                _lblIndicador.Text = "⚠";
                _lblIndicador.Foreground = Tema.Pincel(critico ? Tema.StrengthWeak : Tema.StrengthMedium);
                ToolTip.SetTip(_lblIndicador, descricao);
                AutomationProperties.SetName(_lblIndicador, descricao);
                AtualizarNomeAcessivel(descricao);
                return;
            }

            string sufixo = _vazamentos == 0 ? Idioma.Texto("Row.NotFoundInBreaches") : "";
            switch (_nivelForca)
            {
                case 0:
                case 1:
                    DefinirForca("○", Tema.StrengthWeak, Idioma.Texto("Row.PasswordWeak"), sufixo);
                    break;
                case 2:
                    DefinirForca("◐", Tema.StrengthMedium, Idioma.Texto("Row.PasswordMedium"), sufixo);
                    break;
                case 3:
                case 4:
                    DefinirForca("●", Tema.StrengthStrong, Idioma.Texto("Row.PasswordStrong"), sufixo);
                    break;
                default:
                    _lblIndicador.Text = "";
                    ToolTip.SetTip(_lblIndicador, null);
                    AutomationProperties.SetName(_lblIndicador, "");
                    AtualizarNomeAcessivel(null);
                    break;
            }
        }

        private void DefinirForca(string glifo, Color cor, string rotulo, string sufixo)
        {
            _lblIndicador.Text = glifo;
            _lblIndicador.Foreground = Tema.Pincel(cor);
            ToolTip.SetTip(_lblIndicador, rotulo + sufixo);
            AutomationProperties.SetName(_lblIndicador, rotulo);
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

        private void AlternarRevelar()
        {
            _revelada = !_revelada;
            if (_revelada)
            {
                _lblUsuario.Text = _obterSenhaPlain(_senha) ?? "••••••••";
                _lblUsuario.FontFamily = (FontFamily)Application.Current!.FindResource("FonteMono")!;
                _lblUsuario.FontWeight = FontWeight.Bold;
                _lblUsuario.Foreground = Tema.Pincel(Tema.AccentPrimary);
                DefinirIcone(_btnOlho, IconeOlhoFechado);
                ToolTip.SetTip(_btnOlho, Idioma.Texto("Row.HidePassword"));
                AutomationProperties.SetName(_btnOlho, Idioma.Texto("Row.HidePassword"));
                AutomationProperties.SetName(_lblUsuario, Idioma.Texto("A11y.PasswordVisible"));
                Acessibilidade.Anunciar(this, Idioma.Texto("A11y.PasswordVisible"));
            }
            else
            {
                _lblUsuario.Text = _senha.Usuario;
                _lblUsuario.FontFamily = FontFamily.Default;
                _lblUsuario.FontWeight = FontWeight.Normal;
                _lblUsuario.Foreground = Tema.Pincel(Tema.TextSecondary);
                DefinirIcone(_btnOlho, IconeOlho);
                ToolTip.SetTip(_btnOlho, Idioma.Texto("Row.RevealPassword"));
                AutomationProperties.SetName(_btnOlho, Idioma.Texto("Row.RevealPassword"));
                AutomationProperties.SetName(_lblUsuario, $"{_senha.Usuario} — {Idioma.Texto("Row.CopyUser")}");
                Acessibilidade.Anunciar(this, Idioma.Texto("A11y.PasswordHidden"));
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

            DefinirIcone(_btnCopiar, IconeCheck);
            _btnCopiar.Foreground = Tema.Pincel(Tema.StrengthStrong);
            Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", Idioma.Texto("Row.CopyPassword")));
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            t.Tick += (s, e) =>
            {
                DefinirIcone(_btnCopiar, IconeCopiar);
                _btnCopiar.ClearValue(Button.ForegroundProperty);
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

            DefinirIcone(_btnTotp, IconeCheck);
            _btnTotp.Foreground = Tema.Pincel(Tema.StrengthStrong);
            Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", Idioma.Texto("Row.CopyTotp")));
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            t.Tick += (s, e) =>
            {
                DefinirIcone(_btnTotp, IconeChave);
                _btnTotp.ClearValue(Button.ForegroundProperty);
                t.Stop();
            };
            t.Start();
        }

        private async Task CopiarUsuarioAsync()
        {
            if (string.IsNullOrWhiteSpace(_senha.Usuario))
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(_senha.Usuario); } catch { }
            }

            _timerFeedbackUsuario?.Stop();
            _lblUsuario.Text = Idioma.Texto("Row.UserCopied");
            _lblUsuario.FontFamily = FontFamily.Default;
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
                _lblUsuario.Text = _obterSenhaPlain(_senha) ?? "••••••••";
                _lblUsuario.FontFamily = (FontFamily)Application.Current!.FindResource("FonteMono")!;
                _lblUsuario.FontWeight = FontWeight.Bold;
                _lblUsuario.Foreground = Tema.Pincel(Tema.AccentPrimary);
                AutomationProperties.SetName(_lblUsuario, Idioma.Texto("A11y.PasswordVisible"));
                return;
            }

            _lblUsuario.Text = _senha.Usuario;
            _lblUsuario.FontFamily = FontFamily.Default;
            _lblUsuario.FontWeight = FontWeight.Normal;
            _lblUsuario.Foreground = Tema.Pincel(Tema.TextSecondary);
            AutomationProperties.SetName(_lblUsuario, $"{_senha.Usuario} — {Idioma.Texto("Row.CopyUser")}");
        }

        public static Color CorAvatar(string nome)
        {
            return IconesServico.CorFallback(nome);
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
