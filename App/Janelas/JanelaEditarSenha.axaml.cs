using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Globalization;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaEditarSenha : Window
    {
        private readonly IServicoSenha _servicoSenha;
        private readonly IServicoCriptografia? _criptografia;
        private readonly Senha _senhaAtual;
        private readonly ServicoAnexos? _servicoAnexos;
        private readonly ServicoTotp _totp = new();
        private readonly TotpPreview.Temporizador _timerTotp = new();
        private const int PeriodoTotp = 30;

        public JanelaEditarSenha(IServicoSenha servicoSenha, Senha senhaAtual, IServicoCriptografia? criptografia,
            ServicoAnexos? servicoAnexos = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _senhaAtual = senhaAtual ?? throw new ArgumentNullException(nameof(senhaAtual));
            _criptografia = criptografia;
            _servicoAnexos = servicoAnexos ?? (criptografia != null ? new ServicoAnexos(criptografia) : null);

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            AtualizarTitulo();
            TxtNomeServico.Text = _senhaAtual.NomeServico;
            TxtUsuario.Text = _senhaAtual.Usuario;
            TxtUrl.Text = _senhaAtual.Url ?? "";
            TxtNotas.Text = _senhaAtual.Notas ?? "";
            TxtEtiquetas.Text = Etiquetas.Formatar(_senhaAtual.Etiquetas);
            TxtTotp.Text = TotpAtualPlain();

            AtualizarCategorias();
            CmbCategoria.SelectedIndex = (int)_senhaAtual.Categoria;

            CmbTipo.ItemsSource = TemplatesCredencial.Rotulos;
            CmbTipo.SelectedIndex = TemplatesCredencial.ObterIndice(_senhaAtual.Tipo);
            CmbTipo.SelectionChanged += (s, e) => AtualizarCamposPorTipo();
            AtualizarCamposPorTipo(usarValoresIniciais: true);

            TxtTotp.TextChanged += (s, e) => AtualizarPreviewTotp();
            MontarHistorico();
            MontarCodigosRecuperacao();
            MontarAnexos();
            Idioma.Alterado += Idioma_Alterado;
            Closed += (s, e) =>
            {
                _timerTotp.Parar();
                Idioma.Alterado -= Idioma_Alterado;
            };
            AtualizarPreviewTotp();
        }

        private string TotpAtualPlain()
        {
            if (string.IsNullOrEmpty(_senhaAtual.TotpSegredo) || _criptografia == null)
                return "";

            try { return _criptografia.Descriptografar(_senhaAtual.TotpSegredo); }
            catch { return ""; }
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private void Idioma_Alterado(object? sender, EventArgs e)
        {
            AtualizarTitulo();
            AtualizarCategorias();
            AtualizarPreviewTotp();
            MontarHistorico();
            MontarCodigosRecuperacao();
            MontarAnexos();

            var selecionadoTipo = Math.Max(0, CmbTipo.SelectedIndex);
            CmbTipo.ItemsSource = TemplatesCredencial.Rotulos;
            CmbTipo.SelectedIndex = selecionadoTipo;
            AtualizarCamposPorTipo();
        }

        private void AtualizarTitulo()
        {
            Title = Idioma.Texto("Entry.EditTitle");
            LblTitulo.Text = Idioma.Formatar("Entry.EditTitleWithService", _senhaAtual.NomeServico);
        }

        private void AtualizarCategorias()
        {
            var selecionado = Math.Max(0, CmbCategoria.SelectedIndex);
            CmbCategoria.ItemsSource = CategoriasUI.Rotulos;
            CmbCategoria.SelectedIndex = selecionado;
        }

        private void AtualizarCamposPorTipo(bool usarValoresIniciais = false)
        {
            var tipo = TemplatesCredencial.ObterTipo(CmbTipo.SelectedIndex);
            LblUsuario.Text = TemplatesCredencial.RotuloUsuario(tipo);
            AutomationProperties.SetName(TxtUsuario, TemplatesCredencial.RotuloUsuario(tipo));
            LblSenha.Text = Idioma.Formatar("Entry.PasswordKeepCurrentFormat", TemplatesCredencial.RotuloSenha(tipo));
            AutomationProperties.SetName(TxtSenha, LblSenha.Text);

            var valoresAtuais = usarValoresIniciais
                ? DescriptografarCamposExtras()
                : PainelCamposExtras.Children
                    .OfType<TextBox>()
                    .Where(t => t.Tag is string)
                    .ToDictionary(t => (string)t.Tag!, t => t.Text ?? "");

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
                if (valoresAtuais.TryGetValue(campo.Chave, out var valor))
                    caixa.Text = valor;

                PainelCamposExtras.Children.Add(rotulo);
                PainelCamposExtras.Children.Add(caixa);
            }
        }

        private Dictionary<string, string> DescriptografarCamposExtras()
        {
            var resultado = new Dictionary<string, string>();
            if (_criptografia == null)
                return resultado;

            foreach (var (chave, valorCifrado) in _senhaAtual.CamposExtras)
            {
                try { resultado[chave] = _criptografia.Descriptografar(valorCifrado); }
                catch (Exception ex)
                {
                    Diagnostico.Registrar(ex, "CamposExtras");
                    resultado[chave] = Idioma.Texto("Entry.FieldDecryptError");
                }
            }

            return resultado;
        }

        private Dictionary<string, string> LerCamposExtras() =>
            PainelCamposExtras.Children
                .OfType<TextBox>()
                .Where(t => t.Tag is string)
                .ToDictionary(t => (string)t.Tag!, t => t.Text ?? "");

        private async void Salvar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtNomeServico.Text) || string.IsNullOrWhiteSpace(TxtUsuario.Text))
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Entry.EditRequired"), Idioma.Texto("Common.Validation"), TipoMensagem.Aviso);
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

                var novaSenha = TxtSenha.Text;
                if (string.IsNullOrWhiteSpace(novaSenha))
                {
                    novaSenha = _criptografia?.Descriptografar(_senhaAtual.SenhaHash);
                    if (string.IsNullOrEmpty(novaSenha))
                    {
                        await CaixaMensagem.MostrarAsync(this,
                            Idioma.Texto("Entry.RecoverCurrentPasswordError"),
                            Idioma.Texto("Entry.EditTitle"), TipoMensagem.Aviso);
                        return;
                    }
                }

                var (categoria, etiquetas) = LerCategoriaEEtiquetas();
                var tipo = TemplatesCredencial.ObterTipo(CmbTipo.SelectedIndex);
                await _servicoSenha.AtualizarSenhaAsync(
                    _senhaAtual.Id,
                    TxtNomeServico.Text!,
                    TxtUsuario.Text!,
                    novaSenha,
                    categoria,
                    string.IsNullOrWhiteSpace(TxtUrl.Text) ? null : TxtUrl.Text,
                    string.IsNullOrWhiteSpace(TxtNotas.Text) ? null : TxtNotas.Text,
                    etiquetas,
                    tipo,
                    LerCamposExtras());

                await _servicoSenha.DefinirTotpAsync(_senhaAtual.Id, totp);

                await _servicoSenha.PersistirAsync();
                Close(true);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.UpdateError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
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

        private (Categoria categoria, List<string> etiquetas) LerCategoriaEEtiquetas()
        {
            var categoria = (Categoria)Math.Max(0, CmbCategoria.SelectedIndex);
            var etiquetas = Etiquetas.Analisar(TxtEtiquetas.Text);

            if (categoria == Categoria.Other)
            {
                var indice = etiquetas.FindIndex(e => CategoriasUI.TentarObterCategoria(e, out _));
                if (indice >= 0)
                {
                    CategoriasUI.TentarObterCategoria(etiquetas[indice], out categoria);
                    etiquetas.RemoveAt(indice);
                }
            }

            return (categoria, etiquetas);
        }

        private void MontarHistorico()
        {
            PainelHistorico.Children.Clear();

            if (_criptografia == null || _senhaAtual.Historico.Count == 0)
            {
                ExpHistorico.IsVisible = false;
                return;
            }

            for (int i = _senhaAtual.Historico.Count - 1; i >= 0; i--)
                PainelHistorico.Children.Add(CriarLinhaHistorico(_senhaAtual.Historico[i]));

            ExpHistorico.Header = Idioma.Formatar("Entry.HistoryHeader", _senhaAtual.Historico.Count);
            ExpHistorico.IsVisible = true;
        }

        private Control CriarLinhaHistorico(HistoricoSenha item)
        {
            string plain;
            try { plain = _criptografia!.Descriptografar(item.SenhaHash); }
            catch { plain = string.Empty; }

            var data = item.DataAlteracao.ToLocalTime().ToString("g", Idioma.CulturaAtual);

            var lblData = new TextBlock
            {
                Text = Idioma.Formatar("Entry.HistoryReplacedOn", data),
                FontSize = 11,
                Foreground = Tema.Pincel(Tema.TextTertiary),
                VerticalAlignment = VerticalAlignment.Center
            };

            var campo = new TextBox
            {
                PasswordChar = '●',
                IsReadOnly = true,
                Text = plain,
                FontSize = 13,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            campo.Classes.Add("campo");
            campo.Classes.Add("revealPasswordButton");
            AutomationProperties.SetName(campo, Idioma.Texto("A11y.PreviousPassword"));

            var btnCopiar = CriarBotaoHistorico(Idioma.Texto("Row.CopyPassword"), Idioma.Texto("Row.CopyPassword"));
            btnCopiar.Click += async (_, _) => await CopiarAsync(plain, btnCopiar);

            var btnUsar = CriarBotaoHistorico(Idioma.Texto("Entry.HistoryUse"), Idioma.Texto("Entry.HistoryUseTooltip"));
            btnUsar.Click += (_, _) => UsarSenhaAnterior(plain);

            var acoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center
            };
            acoes.Children.Add(btnCopiar);
            acoes.Children.Add(btnUsar);

            var linha = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 4, 0, 0) };
            Grid.SetColumn(campo, 0);
            Grid.SetColumn(acoes, 1);
            linha.Children.Add(campo);
            linha.Children.Add(acoes);

            var painel = new StackPanel();
            painel.Children.Add(lblData);
            painel.Children.Add(linha);
            return painel;
        }

        private static Button CriarBotaoHistorico(string texto, string dica)
        {
            var botao = new Button
            {
                Classes = { "plano" },
                Content = texto,
                FontSize = 12,
                Padding = new Thickness(10, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTip.SetTip(botao, dica);
            AutomationProperties.SetName(botao, dica);
            return botao;
        }

        private void MontarCodigosRecuperacao()
        {
            PainelCodigosRecuperacao.Children.Clear();

            foreach (var item in _senhaAtual.CodigosRecuperacao)
                PainelCodigosRecuperacao.Children.Add(CriarLinhaCodigoRecuperacao(item));
        }

        private Control CriarLinhaCodigoRecuperacao(CodigoRecuperacao item)
        {
            string plain;
            if (_criptografia == null)
            {
                plain = string.Empty;
            }
            else
            {
                try { plain = _criptografia.Descriptografar(item.Codigo); }
                catch { plain = string.Empty; }
            }

            var campo = new TextBox
            {
                PasswordChar = '●',
                IsReadOnly = true,
                Text = plain,
                FontSize = 13,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = item.Usado ? 0.55 : 1.0
            };
            campo.Classes.Add("campo");
            campo.Classes.Add("revealPasswordButton");
            AutomationProperties.SetName(campo, Idioma.Texto("A11y.RecoveryCode"));

            var btnCopiar = CriarBotaoHistorico(Idioma.Texto("Row.CopyRecoveryCode"), Idioma.Texto("Row.CopyRecoveryCode"));
            btnCopiar.Click += async (_, _) => await CopiarAsync(plain, btnCopiar);

            var rotuloUsado = Idioma.Texto(item.Usado ? "Entry.RecoveryCodeUnmark" : "Entry.RecoveryCodeMarkUsed");
            var btnUsado = CriarBotaoHistorico(rotuloUsado, rotuloUsado);
            btnUsado.Click += async (_, _) => await AlternarUsadoAsync(item);

            var rotuloRemover = Idioma.Texto("Entry.RecoveryCodeRemove");
            var btnRemover = CriarBotaoHistorico(rotuloRemover, rotuloRemover);
            btnRemover.Click += async (_, _) => await RemoverCodigoAsync(item);

            var acoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center
            };
            acoes.Children.Add(btnCopiar);
            acoes.Children.Add(btnUsado);
            acoes.Children.Add(btnRemover);

            var linha = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            Grid.SetColumn(campo, 0);
            Grid.SetColumn(acoes, 1);
            linha.Children.Add(campo);
            linha.Children.Add(acoes);
            return linha;
        }

        private async void AdicionarCodigosRecuperacao_Click(object? sender, RoutedEventArgs e)
        {
            var linhas = (TxtNovosCodigos.Text ?? "")
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            if (linhas.Count == 0)
                return;

            try
            {
                await _servicoSenha.AdicionarCodigosRecuperacaoAsync(_senhaAtual.Id, linhas.Select(c => (c, false)));
                await _servicoSenha.PersistirAsync();
                TxtNovosCodigos.Text = "";
                MontarCodigosRecuperacao();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.RecoveryCodesAddError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task AlternarUsadoAsync(CodigoRecuperacao item)
        {
            try
            {
                await _servicoSenha.MarcarCodigoRecuperacaoAsync(_senhaAtual.Id, item.Id, !item.Usado);
                await _servicoSenha.PersistirAsync();
                MontarCodigosRecuperacao();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.RecoveryCodeMarkError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task RemoverCodigoAsync(CodigoRecuperacao item)
        {
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Entry.RecoveryCodeRemoveConfirm"), Idioma.Texto("Entry.RecoveryCodeRemove"));
            if (!confirmar)
                return;

            try
            {
                await _servicoSenha.RemoverCodigoRecuperacaoAsync(_senhaAtual.Id, item.Id);
                await _servicoSenha.PersistirAsync();
                MontarCodigosRecuperacao();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.RecoveryCodeRemoveError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void MontarAnexos()
        {
            PainelAnexos.Children.Clear();
            BtnAdicionarAnexo.IsEnabled = _servicoAnexos != null &&
                _senhaAtual.Anexos.Count < ServicoAnexos.QuantidadeMaximaPorCredencial;

            foreach (var item in _senhaAtual.Anexos)
                PainelAnexos.Children.Add(CriarLinhaAnexo(item));
        }

        private Control CriarLinhaAnexo(AnexoSenha item)
        {
            var nome = new TextBlock
            {
                Text = $"{item.NomeArquivo} ({FormatarTamanhoAnexo(item.TamanhoBytes)})",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            AutomationProperties.SetName(nome, item.NomeArquivo);

            var btnBaixar = CriarBotaoHistorico(Idioma.Texto("Entry.AttachmentDownload"), Idioma.Texto("Entry.AttachmentDownload"));
            btnBaixar.Click += async (_, _) => await BaixarAnexoAsync(item);

            var btnRemover = CriarBotaoHistorico(Idioma.Texto("Entry.AttachmentRemove"), Idioma.Texto("Entry.AttachmentRemove"));
            btnRemover.Click += async (_, _) => await RemoverAnexoAsync(item);

            var acoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center
            };
            acoes.Children.Add(btnBaixar);
            acoes.Children.Add(btnRemover);

            var linha = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            Grid.SetColumn(nome, 0);
            Grid.SetColumn(acoes, 1);
            linha.Children.Add(nome);
            linha.Children.Add(acoes);
            return linha;
        }

        private static string FormatarTamanhoAnexo(long bytes) =>
            bytes < 1024 * 1024 ? $"{Math.Max(1, bytes / 1024)} KB" : $"{bytes / 1024.0 / 1024.0:0.0} MB";

        private async void AdicionarAnexo_Click(object? sender, RoutedEventArgs e)
        {
            if (_servicoAnexos == null)
                return;

            var arquivos = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Idioma.Texto("Entry.AttachmentsAdd"),
                AllowMultiple = false
            });
            if (arquivos.Count == 0)
                return;

            try
            {
                await using var origem = await arquivos[0].OpenReadAsync();
                using var memoria = new MemoryStream();
                await origem.CopyToAsync(memoria);

                await _servicoAnexos.AdicionarAsync(_senhaAtual, arquivos[0].Name, memoria.ToArray());
                await _servicoSenha.PersistirAsync();
                MontarAnexos();
            }
            catch (LimiteAnexoExcedidoException ex)
            {
                await CaixaMensagem.MostrarAsync(this, ErrosUi.MensagemAmigavel(ex), Idioma.Texto("Entry.AttachmentsAdd"), TipoMensagem.Aviso);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.AttachmentAddError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task BaixarAnexoAsync(AnexoSenha item)
        {
            if (_servicoAnexos == null)
                return;

            var arquivo = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Idioma.Texto("Entry.AttachmentDownload"),
                SuggestedFileName = item.NomeArquivo
            });
            if (arquivo == null)
                return;

            try
            {
                var bytes = await _servicoAnexos.LerAsync(item);
                await using var destino = await arquivo.OpenWriteAsync();
                await destino.WriteAsync(bytes);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.AttachmentDownloadError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task RemoverAnexoAsync(AnexoSenha item)
        {
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Entry.AttachmentRemoveConfirm", item.NomeArquivo), Idioma.Texto("Entry.AttachmentRemove"));
            if (!confirmar || _servicoAnexos == null)
                return;

            _servicoAnexos.Remover(_senhaAtual, item.Id);
            await _servicoSenha.PersistirAsync();
            MontarAnexos();
        }

        private void UsarSenhaAnterior(string senha)
        {
            TxtSenha.Text = senha;
            TxtSenha.Focus();
            ExpHistorico.IsExpanded = false;
        }

        private async Task CopiarAsync(string texto, Button? origem = null)
        {
            if (string.IsNullOrEmpty(texto))
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(texto); }
                catch { }
            }

            int segundos = Preferencias.SegundosLimpezaClipboard;
            if (segundos > 0 && clipboard != null)
            {
                Acessibilidade.Anunciar(this,
                    Idioma.Formatar("A11y.CopiedWillClear", Idioma.Texto("A11y.PreviousPassword"), segundos));
                _ = ServicoLimpezaClipboard.ProgramarLimpezaAsync(new AreaTransferenciaAvalonia(clipboard), texto, segundos);

                if (origem != null)
                {
                    var mensagem = Idioma.Formatar("Row.PasswordCopiedClearing", segundos);
                    ToolTip.SetTip(origem, mensagem);
                    AutomationProperties.SetName(origem, mensagem);
                    var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    t.Tick += (s, e) =>
                    {
                        ToolTip.SetTip(origem, Idioma.Texto("Row.CopyPassword"));
                        AutomationProperties.SetName(origem, Idioma.Texto("Row.CopyPassword"));
                        t.Stop();
                    };
                    t.Start();
                }
            }
            else
            {
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", Idioma.Texto("A11y.PreviousPassword")));
            }
        }
    }
}
