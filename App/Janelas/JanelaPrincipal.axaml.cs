using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CofreDeSenhas.Controles;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaPrincipal : Window
    {
        private enum ColunaTabela { Servico, Usuario, Categoria, Data, Acoes }
        private enum ColunaOrdenacao { Servico, Usuario, Categoria, Forca }

        private IServicoSenha _servicoSenha;
        private IServicoSenha _servicoSenhaLocal;
        private readonly byte[] _chaveMestra;
        private readonly IServicoCriptografia? _criptografia;
        private readonly ServicoAnexos? _servicoAnexos;
        private IRepositorioSenha? _repositorioLocal;
        private ServicoSincronizacao? _servicoSincronizacao;
        private readonly ServicoDesbloqueioBiometrico _biometria = new();
        private readonly ServicoAuditoriaSenha _servicoAuditoria = new();
        private readonly ServicoVazamento _servicoVazamento = new();
        private readonly ServicoExportacao _servicoExportacao = new();
        private readonly ServicoImportacaoCsv _servicoImportacaoCsv = new();
        private readonly ServicoTotp _totp = new();
        private readonly Action? _aoBloquear;
        private readonly MonitorInatividade _monitor;
        private readonly DispatcherTimer _timerSincronizacao;
        private bool _sincronizando;
        private bool _conectadoAoBanco;
        private string? _descricaoConexaoAtual;
        private bool _falhaReconexaoAtual;

        private List<Senha> _senhasAtuais = new();
        private readonly List<LinhaSenha> _linhasSenha = new();
        private LinhaSenha? _linhaFocada;
        private readonly Dictionary<Guid, ItemAuditoriaSenha> _itensAuditoria = new();
        private ResultadoAuditoriaCofre? _resultadoAuditoria;
        private readonly Dictionary<Guid, int> _vazamentosPorId = new();
        private CategoriaRelatorioSeguranca? _filtroSeguranca;

        private bool _somenteFavoritos;
        private bool _somenteRecentes;
        private bool _ordenacaoDescendente;
        private ColunaOrdenacao _colunaOrdenacao = ColunaOrdenacao.Servico;
        private bool _navColapsada;
        private bool _naLixeira;
        private bool _modoPrivacidade;
        private string? _versaoDisponivel;
        private List<Senha> _itensLixeira = new();
        private Senha? _senhaDetalhe;
        private string _senhaDetalhePlain = "";
        private bool _senhaDetalheVisivel;
        private readonly TotpPreview.Temporizador _timerTotpDetalhe = new();
        private const int PeriodoTotpDetalhe = 30;
        private double _larguraServico = 140;
        private double _larguraUsuario = 240;
        private double _larguraCategoria = 108;
        private double _larguraData = 92;
        private double _larguraAcoes = 200;
        private ColunaTabela? _colunaEmRedimensionamento;
        private ColunaTabela? _colunaDireitaEmRedimensionamento;
        private double _inicioRedimensionamentoX;
        private double _larguraInicialRedimensionamento;
        private double _larguraDireitaInicialRedimensionamento;
        private bool _largurasIniciaisAplicadas;
        private double _larguraTabelaAnterior;

        private const double LarguraMinimaServico = 88;
        private const double LarguraMinimaUsuario = 160;
        private const double LarguraMinimaCategoria = 86;
        private const double LarguraMinimaData = 78;
        private const double LarguraMinimaAcoes = 200;

        public JanelaPrincipal(IServicoSenha servicoSenha, byte[] chaveMestra, IServicoCriptografia? criptografia = null,
            IRepositorioSenha? repositorioLocal = null, Action? aoBloquear = null,
            ServicoSincronizacao? servicoSincronizacao = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _servicoSenhaLocal = _servicoSenha;
            _chaveMestra = chaveMestra?.ToArray() ?? throw new ArgumentNullException(nameof(chaveMestra));
            _criptografia = criptografia;
            _servicoAnexos = criptografia != null ? new ServicoAnexos(criptografia) : null;
            _repositorioLocal = repositorioLocal;
            _servicoSincronizacao = servicoSincronizacao;
            _aoBloquear = aoBloquear;

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            Acessibilidade.RegistrarAnunciador(this, LblAnuncioLeitorTela);
            ConfigurarAcessibilidadeLeitorTela();

            CmbCategoria.ItemsSource = ConstruirFiltrosCategoria();
            CmbCategoria.SelectedIndex = 0;
            CmbEtiqueta.ItemsSource = ConstruirFiltrosEtiqueta(Array.Empty<Senha>());
            CmbEtiqueta.SelectedIndex = 0;

            Gerador.SolicitouSalvar += Gerador_SolicitouSalvar;
            Gerador.ShowHeader = false;

            AtualizarBotaoPrivacidade();
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            AtualizarContador();
            AtualizarEstadoConexao(null);
            MarcarIdiomaSelecionado();
            AtualizarMenuBiometria();

            _monitor = new MonitorInatividade(this, () => _aoBloquear?.Invoke());
            _monitor.Ajustar(Preferencias.MinutosBloqueio);
            BtnConfig.Flyout!.Opened += (s, e) =>
            {
                MarcarBloqueioSelecionado(Preferencias.MinutosBloqueio);
                MarcarLimpezaClipboardSelecionada(Preferencias.SegundosLimpezaClipboard);
                MarcarIdiomaSelecionado();
                MarcarAcessibilidadeSelecionada();
                ConfigurarAcessibilidadeLeitorTela();
                AtualizarMenuBiometria();
                MenuIconesOnline.IsChecked = Preferencias.IconesOnline;
                MenuHistoricoUso.IsChecked = Preferencias.RegistrarHistoricoUso;
                MenuVerificarAtualizacoes.IsChecked = Preferencias.VerificarAtualizacoes;
            };
            Idioma.Alterado += IdiomaGlobal_Alterado;
            Acessibilidade.Alterado += Acessibilidade_Alterado;
            AddHandler(KeyDownEvent, Atalho_KeyDown, RoutingStrategies.Tunnel);
            _timerSincronizacao = new DispatcherTimer();
            _timerSincronizacao.Tick += async (s, e) => await SincronizarAsync(silencioso: true);
            AjustarTimerSincronizacao();

            Closed += (s, e) =>
            {
                _monitor.Encerrar();
                _timerSincronizacao.Stop();
                Idioma.Alterado -= IdiomaGlobal_Alterado;
                Acessibilidade.Alterado -= Acessibilidade_Alterado;
                FecharDetalhes();
                foreach (var linha in _linhasSenha)
                    linha.EsconderSenhaSeRevelada();
                CryptographicOperations.ZeroMemory(_chaveMestra);
                _criptografia?.ZerarChave();
                _servicoSincronizacao?.ZerarChave();
            };

            Opened += async (s, e) =>
            {
                AjustarLargurasIniciais();
                await IniciarAsync();
                _ = VerificarAtualizacaoAsync();
                _ = SincronizarAsync(silencioso: true);
            };

            GridCabecalhoTabela.SizeChanged += GridCabecalhoTabela_SizeChanged;
        }

        private void Atalho_KeyDown(object? sender, KeyEventArgs e)
        {
            var atalho = AtalhosTeclado.Encontrar(e.Key, e.KeyModifiers);
            if (atalho == null)
                return;

            switch (atalho.Acao)
            {
                case AtalhosTeclado.Acao.Buscar:
                    TxtBusca.Focus();
                    TxtBusca.SelectAll();
                    break;
                case AtalhosTeclado.Acao.NovaSenha:
                    NovaSenha_Click(this, new RoutedEventArgs());
                    break;
                case AtalhosTeclado.Acao.AbrirGerador:
                    ToggleGerador_Click(this, new RoutedEventArgs());
                    break;
                case AtalhosTeclado.Acao.BloquearAgora:
                    BloquearAgora_Click(this, new RoutedEventArgs());
                    break;
                case AtalhosTeclado.Acao.CopiarUsuario:
                    _ = CopiarUsuarioLinhaFocadaAsync();
                    break;
                case AtalhosTeclado.Acao.CopiarSenha:
                    _ = CopiarSenhaLinhaFocadaAsync();
                    break;
                case AtalhosTeclado.Acao.ModoPrivacidade:
                    Privacidade_Click(this, new RoutedEventArgs());
                    break;
            }
            e.Handled = true;
        }

        private void BloquearAgora_Click(object? sender, RoutedEventArgs e) => _aoBloquear?.Invoke();

        private void Privacidade_Click(object? sender, RoutedEventArgs e)
        {
            _modoPrivacidade = !_modoPrivacidade;

            if (_modoPrivacidade)
                FecharDetalhes();

            foreach (var linha in _linhasSenha)
                linha.DefinirModoPrivacidade(_modoPrivacidade);

            AtualizarBotaoPrivacidade();
            Acessibilidade.Anunciar(this, Idioma.Texto(_modoPrivacidade ? "A11y.PrivacyModeOn" : "A11y.PrivacyModeOff"));
        }

        private void AtualizarBotaoPrivacidade()
        {
            BtnPrivacidade.Content = IconeOlhoPrivacidade(_modoPrivacidade);
            var dica = Idioma.Texto(_modoPrivacidade ? "Privacy.Disable" : "Privacy.Enable");
            ToolTip.SetTip(BtnPrivacidade, dica);
            AutomationProperties.SetName(BtnPrivacidade, dica);
        }

        private static Icone IconeOlhoPrivacidade(bool ativo) =>
            Recursos.ImagemIcone(ativo ? "IconeRevelar" : "IconeOcultar", 28);

        private async Task CopiarUsuarioLinhaFocadaAsync()
        {
            if (_naLixeira) return;
            var linha = _linhaFocada ?? _linhasSenha.FirstOrDefault();
            if (linha != null) await linha.CopiarUsuarioAsync();
        }

        private async Task CopiarSenhaLinhaFocadaAsync()
        {
            if (_naLixeira) return;
            var linha = _linhaFocada ?? _linhasSenha.FirstOrDefault();
            if (linha != null) await linha.CopiarAsync();
        }

        private async void AtalhosTeclado_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaAtalhosTeclado();
            await AbrirDialogoAsync<bool>(dlg);
        }

        private async Task IniciarAsync()
        {
            var perfil = Preferencias.UltimoBanco;
            if (_criptografia != null && perfil is { Conectado: true })
            {
                var cfg = MontarConexaoDoPerfil(perfil);
                if (cfg != null)
                {
                    await ConectarAsync(cfg, persistir: false, silencioso: true);
                    if (_conectadoAoBanco)
                    {
                        _ = VerificarBackupAgendadoAsync();
                        return;
                    }
                }
            }

            await CarregarSenhasAsync();
            _ = VerificarBackupAgendadoAsync();
        }

        private async Task VerificarBackupAgendadoAsync()
        {
            if (_criptografia == null)
                return;

            try
            {
                var frequencia = Preferencias.FrequenciaBackupAtual;

                var persistencia = new PersistenciaLocal(_criptografia);
                var backups = persistencia.ListarBackups();
                DateTime? ultimo = backups.Count > 0 ? backups[0].DataUtc : null;

                if (!AgendaBackup.Devido(ultimo, frequencia, DateTime.UtcNow))
                    return;

                var senhas = await _servicoSenhaLocal.ListarTodosAsync();
                if (senhas.Count == 0)
                    return;

                await persistencia.BackupAutomaticoAsync(senhas, _chaveMestra, Preferencias.MaximoBackups);
            }
            catch
            {
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == WindowStateProperty && Moldura != null)
            {
                bool maximizada = WindowState == WindowState.Maximized;
                Moldura.CornerRadius = new CornerRadius(maximizada ? 0 : 10);
                BtnMaximizar.Content = IconeJanela(maximizada ? "IconeRestaurar" : "IconeMaximizar");
            }
        }

        private void BarraTitulo_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;
            if (e.Source is Visual v && v.FindAncestorOfType<Button>(true) != null)
                return;
            BeginMoveDrag(e);
        }

        private void Redimensionar(object? sender, PointerPressedEventArgs e)
        {
            if (WindowState != WindowState.Normal) return;
            if (sender is Border b && b.Tag is string borda)
                BeginResizeDrag(Enum.Parse<WindowEdge>(borda), e);
        }

        private void RedimensionarColuna_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border divisor || divisor.Tag is not string tag)
                return;

            var coluna = Enum.Parse<ColunaTabela>(tag);
            var colunaDireita = ObterColunaDireita(coluna);
            if (colunaDireita == null)
                return;

            _colunaEmRedimensionamento = coluna;
            _colunaDireitaEmRedimensionamento = colunaDireita;
            _inicioRedimensionamentoX = e.GetPosition(GridCabecalhoTabela).X;
            _larguraInicialRedimensionamento = ObterLarguraColuna(coluna);
            _larguraDireitaInicialRedimensionamento = ObterLarguraColuna(colunaDireita.Value);
            e.Pointer.Capture(divisor);
            e.Handled = true;
        }

        private void RedimensionarColuna_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_colunaEmRedimensionamento == null || _colunaDireitaEmRedimensionamento == null)
                return;

            var colunaEsquerda = _colunaEmRedimensionamento.Value;
            var colunaDireita = _colunaDireitaEmRedimensionamento.Value;

            var delta = e.GetPosition(GridCabecalhoTabela).X - _inicioRedimensionamentoX;
            var minimoEsquerda = ObterLarguraMinimaColuna(colunaEsquerda);
            var minimoDireita = ObterLarguraMinimaColuna(colunaDireita);
            var deltaMinimo = minimoEsquerda - _larguraInicialRedimensionamento;
            var deltaMaximo = _larguraDireitaInicialRedimensionamento - minimoDireita;

            delta = Math.Clamp(delta, deltaMinimo, deltaMaximo);

            DefinirLarguraColuna(colunaEsquerda, _larguraInicialRedimensionamento + delta);
            DefinirLarguraColuna(colunaDireita, _larguraDireitaInicialRedimensionamento - delta);
            AplicarLargurasColunas();
            e.Handled = true;
        }

        private void RedimensionarColuna_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _colunaEmRedimensionamento = null;
            _colunaDireitaEmRedimensionamento = null;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        private void AjustarLargurasIniciais()
        {
            if (_largurasIniciaisAplicadas || GridCabecalhoTabela.Bounds.Width <= 0)
                return;

            _largurasIniciaisAplicadas = true;

            double larguraDisponivel = GridCabecalhoTabela.Bounds.Width;
            double fixo = 42 + 24 + 6 + 6 + 6 + 6;

            _larguraAcoes = Math.Clamp(larguraDisponivel * 0.17, LarguraMinimaAcoes, 220);
            _larguraCategoria = Math.Clamp(larguraDisponivel * 0.13, LarguraMinimaCategoria, 132);
            _larguraData = Math.Clamp(larguraDisponivel * 0.11, LarguraMinimaData, 118);

            double flexivel = Math.Max(
                LarguraMinimaServico + LarguraMinimaUsuario,
                larguraDisponivel - fixo - _larguraCategoria - _larguraData - _larguraAcoes);

            _larguraServico = Math.Clamp(flexivel * 0.44, LarguraMinimaServico, 260);
            _larguraUsuario = Math.Max(LarguraMinimaUsuario, flexivel - _larguraServico);

            _larguraTabelaAnterior = larguraDisponivel;
            AplicarLargurasColunas();
        }

        private void GridCabecalhoTabela_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (!_largurasIniciaisAplicadas)
            {
                AjustarLargurasIniciais();
                return;
            }

            if (_larguraTabelaAnterior <= 0)
                return;

            var larguraAtual = GridCabecalhoTabela.Bounds.Width;
            var delta = larguraAtual - _larguraTabelaAnterior;
            _larguraTabelaAnterior = larguraAtual;
            if (delta == 0)
                return;

            var larguraServicoAlvo = _larguraServico + delta;
            var larguraServicoAplicada = Math.Max(LarguraMinimaServico, larguraServicoAlvo);
            var sobra = larguraServicoAlvo - larguraServicoAplicada;

            DefinirLarguraColuna(ColunaTabela.Servico, larguraServicoAplicada);
            if (sobra != 0)
                DefinirLarguraColuna(ColunaTabela.Usuario, _larguraUsuario + sobra);

            AplicarLargurasColunas();
        }

        private double ObterLarguraColuna(ColunaTabela coluna) => coluna switch
        {
            ColunaTabela.Servico => _larguraServico,
            ColunaTabela.Usuario => _larguraUsuario,
            ColunaTabela.Categoria => _larguraCategoria,
            ColunaTabela.Data => _larguraData,
            ColunaTabela.Acoes => _larguraAcoes,
            _ => 0
        };

        private static ColunaTabela? ObterColunaDireita(ColunaTabela coluna) => coluna switch
        {
            ColunaTabela.Servico => ColunaTabela.Usuario,
            ColunaTabela.Usuario => ColunaTabela.Categoria,
            ColunaTabela.Categoria => ColunaTabela.Data,
            ColunaTabela.Data => ColunaTabela.Acoes,
            _ => null
        };

        private static double ObterLarguraMinimaColuna(ColunaTabela coluna) => coluna switch
        {
            ColunaTabela.Servico => LarguraMinimaServico,
            ColunaTabela.Usuario => LarguraMinimaUsuario,
            ColunaTabela.Categoria => LarguraMinimaCategoria,
            ColunaTabela.Data => LarguraMinimaData,
            ColunaTabela.Acoes => LarguraMinimaAcoes,
            _ => 0
        };

        private void DefinirLarguraColuna(ColunaTabela coluna, double largura)
        {
            switch (coluna)
            {
                case ColunaTabela.Servico:
                    _larguraServico = Math.Max(LarguraMinimaServico, largura);
                    break;
                case ColunaTabela.Usuario:
                    _larguraUsuario = Math.Max(LarguraMinimaUsuario, largura);
                    break;
                case ColunaTabela.Categoria:
                    _larguraCategoria = Math.Max(LarguraMinimaCategoria, largura);
                    break;
                case ColunaTabela.Data:
                    _larguraData = Math.Max(LarguraMinimaData, largura);
                    break;
                case ColunaTabela.Acoes:
                    _larguraAcoes = Math.Max(LarguraMinimaAcoes, largura);
                    break;
            }
        }

        private void AplicarLargurasColunas()
        {
            if (GridCabecalhoTabela == null)
                return;

            GridCabecalhoTabela.ColumnDefinitions[1].Width = new GridLength(_larguraServico);
            GridCabecalhoTabela.ColumnDefinitions[3].Width = new GridLength(_larguraUsuario);
            GridCabecalhoTabela.ColumnDefinitions[5].Width = new GridLength(_larguraCategoria);
            GridCabecalhoTabela.ColumnDefinitions[7].Width = new GridLength(_larguraData);
            GridCabecalhoTabela.ColumnDefinitions[9].Width = new GridLength(_larguraAcoes);

            foreach (var linha in _linhasSenha)
                linha.DefinirLargurasColunas(_larguraServico, _larguraUsuario, _larguraCategoria, _larguraData, _larguraAcoes);
        }

        private void Minimizar_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximizar_Click(object? sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();

        private void Idioma_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not string codigo ||
                string.Equals(codigo, Idioma.Atual.Codigo, StringComparison.OrdinalIgnoreCase))
                return;

            Idioma.Definir(codigo);
            Preferencias.Idioma = Idioma.Atual.Codigo;
            Preferencias.Salvar();
        }

        private void IdiomaGlobal_Alterado(object? sender, EventArgs e)
        {
            AtualizarBotaoPrivacidade();
            MarcarIdiomaSelecionado();
            AtualizarMenuBiometria();
            AtualizarFiltroOrganizacao();
            AtualizarContador();
            AtualizarEstadoConexao(_descricaoConexaoAtual, _falhaReconexaoAtual);
            ConfigurarAcessibilidadeLeitorTela();
            FiltrarSenhas();
        }

        private void MarcarIdiomaSelecionado()
        {
            if (MenuIdioma == null)
                return;

            foreach (var item in MenuIdioma.Items.OfType<MenuItem>())
                item.IsChecked = item.Tag is string codigo &&
                    string.Equals(codigo, Idioma.Atual.Codigo, StringComparison.OrdinalIgnoreCase);
        }

        private void Daltonismo_Alterado(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickDaltonismo(sender);

        private void Escala_Alterada(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickEscala(sender);

        private void AltoContraste_Alterado(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickAltoContraste(sender);

        private void ReduzirAnimacoes_Alterado(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickReducaoMovimento(sender);

        private async void IconesOnline_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            if (item.IsChecked)
            {
                var aceitou = await CaixaMensagem.ConfirmarAsync(this,
                    Idioma.Texto("Icons.ConsentMessage"),
                    Idioma.Texto("Settings.OnlineIcons"),
                    TipoMensagem.Info);
                if (!aceitou)
                {
                    item.IsChecked = false;
                    return;
                }

                Preferencias.IconesOnline = true;
            }
            else
            {
                Preferencias.IconesOnline = false;
                IconesServico.LimparCache();
            }

            Preferencias.Salvar();
            FiltrarSenhas();
        }

        private void HistoricoUso_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            Preferencias.RegistrarHistoricoUso = item.IsChecked;
            Preferencias.Salvar();
        }

        private void VerificarAtualizacoes_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            Preferencias.VerificarAtualizacoes = item.IsChecked;
            Preferencias.Salvar();

            if (item.IsChecked)
                _ = VerificarAtualizacaoAsync();
            else
                OcultarAvisoAtualizacao();
        }

        private async Task VerificarAtualizacaoAsync()
        {
            if (!Preferencias.VerificarAtualizacoes)
                return;

            var versao = await ServicoAtualizacao.VerificarNovaVersaoAsync();
            if (string.IsNullOrEmpty(versao) || string.Equals(versao, Preferencias.VersaoDispensada, StringComparison.OrdinalIgnoreCase))
                return;

            _versaoDisponivel = versao;
            LblAtualizacaoDisponivel.Text = Idioma.Formatar("Update.Available", versao);
            AutomationProperties.SetName(LblAtualizacaoDisponivel, Idioma.Formatar("Update.Available", versao));
            PainelAtualizacaoDisponivel.IsVisible = true;
        }

        private void AjustarTimerSincronizacao()
        {
            var perfil = Preferencias.Sincronizacao;
            if (perfil == null || perfil.FrequenciaMinutos <= 0 || _servicoSincronizacao == null)
            {
                _timerSincronizacao.Stop();
                return;
            }

            _timerSincronizacao.Interval = TimeSpan.FromMinutes(perfil.FrequenciaMinutos);
            _timerSincronizacao.Start();
        }

        private async void Sincronizacao_Click(object? sender, RoutedEventArgs e)
        {
            if (_repositorioLocal == null || _criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Sync.Title"), TipoMensagem.Aviso);
                return;
            }

            var dlg = new JanelaSincronizacao(_servicoSincronizacao,
                servico => _servicoSincronizacao = servico,
                () => SincronizarAsync(silencioso: false));

            await AbrirDialogoAsync<bool>(dlg);
            AjustarTimerSincronizacao();
        }

        private async Task<bool> SincronizarAsync(bool silencioso)
        {
            if (_servicoSincronizacao == null || Preferencias.Sincronizacao is not { } perfil || _sincronizando)
                return false;

            _sincronizando = true;
            try
            {
                var caminho = Path.Combine(perfil.Pasta, ServicoSincronizacao.NomeArquivo);

                var locais = new List<SenhaExportada>();
                var todasLocais = (await _servicoSenha.ListarTodosAsync()).Concat(await _servicoSenha.ListarLixeiraAsync());
                foreach (var s in todasLocais)
                {
                    var plain = ObterSenhaPlain(s);
                    if (plain == null)
                        continue;

                    locais.Add(new SenhaExportada
                    {
                        Id = s.Id,
                        NomeServico = s.NomeServico,
                        Usuario = s.Usuario,
                        Senha = plain,
                        Url = s.Url,
                        Categoria = s.Categoria,
                        Etiquetas = s.Etiquetas.ToList(),
                        Notas = s.Notas,
                        Tipo = s.Tipo,
                        CamposExtras = ObterCamposExtrasPlain(s),
                        TotpSegredo = ObterTotpPlain(s),
                        Historico = ObterHistoricoPlain(s),
                        CodigosRecuperacao = ObterCodigosRecuperacaoPlain(s),
                        Favorito = s.Favorito,
                        Fixado = s.Fixado,
                        NaLixeira = s.NaLixeira,
                        DataExclusao = s.DataExclusao,
                        DataCriacao = s.DataCriacao,
                        DataAtualizacao = s.DataAtualizacao
                    });
                }

                var remotas = await _servicoSincronizacao.LerAsync(caminho);
                var mescladas = ServicoSincronizacao.MesclarListas(locais, remotas);

                foreach (var item in mescladas)
                    await _servicoSenha.AplicarSincronizadoAsync(item);
                await _servicoSenha.PersistirAsync();

                var salt = Convert.FromBase64String(perfil.Salt);
                await _servicoSincronizacao.EscreverAsync(caminho, salt, perfil.Iteracoes, mescladas);

                perfil.UltimaSincronizacao = DateTime.UtcNow;
                Preferencias.Salvar();

                await CarregarSenhasAsync();
                return true;
            }
            catch
            {
                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Sync.Error"), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                return false;
            }
            finally
            {
                _sincronizando = false;
            }
        }

        private void AbrirNovaVersao_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(ServicoAtualizacao.UrlPaginaReleases) { UseShellExecute = true }); }
            catch { }
        }

        private void DispensarAtualizacao_Click(object? sender, RoutedEventArgs e)
        {
            Preferencias.VersaoDispensada = _versaoDisponivel;
            Preferencias.Salvar();
            OcultarAvisoAtualizacao();
        }

        private void OcultarAvisoAtualizacao()
        {
            PainelAtualizacaoDisponivel.IsVisible = false;
            _versaoDisponivel = null;
        }

        private void LeitorTela_Alterado(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickLeitorTela(this, sender);

        private void MarcarAcessibilidadeSelecionada() =>
            Acessibilidade.MarcarMenus(MenuDaltonismo, MenuEscala, MenuAltoContraste, MenuReduzirAnimacoes,
                MenuLeitorTela);

        private void Acessibilidade_Alterado(object? sender, EventArgs e)
        {
            ConfigurarAcessibilidadeLeitorTela();
            Gerador.AtualizarTema();
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            AtualizarDetalheVisual();
            AtualizarHistoricoDetalhes();
            FiltrarSenhas();
        }

        private void ConfigurarAcessibilidadeLeitorTela()
        {
            AutomationProperties.SetHelpText(MenuLeitorTela, Idioma.Texto("Access.ScreenReaderHelp"));
            AutomationProperties.SetHelpText(TxtBusca, Idioma.Texto("A11y.OptionalField"));
            AutomationProperties.SetHelpText(CmbCategoria, Idioma.Texto("A11y.OptionalField"));
            AutomationProperties.SetName(PainelLista, Idioma.Texto("A11y.ResultsList"));
            AutomationProperties.SetLiveSetting(LblStatus, AutomationLiveSetting.Polite);
            AutomationProperties.SetLiveSetting(LblConexao, AutomationLiveSetting.Polite);
            AutomationProperties.SetLiveSetting(LblVazio, AutomationLiveSetting.Polite);
            PintarFiltroFavoritos();
            AtualizarContador();
            AtualizarEstadoConexao(_descricaoConexaoAtual, _falhaReconexaoAtual);
        }

        private async Task<T> AbrirDialogoAsync<T>(Window dialogo)
        {
            Scrim.Mostrar(this);
            try
            {
                return await dialogo.ShowDialog<T>(this);
            }
            finally
            {
                Scrim.Ocultar(this);
            }
        }

        private async void Gerador_SolicitouSalvar(object? sender, string senha)
        {
            var dlg = new JanelaCriarSenha(_servicoSenha, senha);
            if (await AbrirDialogoAsync<bool>(dlg))
            {
                FecharGerador();
                await CarregarSenhasAsync();
            }
        }

        private async void NovaSenha_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaCriarSenha(_servicoSenha);
            if (await AbrirDialogoAsync<bool>(dlg))
                await CarregarSenhasAsync();
        }

        private async Task CarregarSenhasAsync()
        {
            try
            {
                LimparAuditoria();
                _senhasAtuais = await _servicoSenha.ListarTodosAsync();
                AtualizarFiltroOrganizacao();

                if (_naLixeira)
                {
                    await CarregarLixeiraAsync();
                }
                else
                {
                    FiltrarSenhas();
                    AtualizarContador();
                }
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.LoadError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void AtualizarLista(List<Senha> lista)
        {
            PainelLista.Children.Clear();
            _linhasSenha.Clear();
            _linhaFocada = null;

            LblVazio.IsVisible = lista.Count == 0;
            TxtVazioMensagem.Text = Idioma.Texto("Vault.Empty");
            BtnVazioNovaSenha.IsVisible = true;
            var estadoLista = lista.Count == 0
                ? Idioma.Texto("A11y.EmptyList")
                : Idioma.Plural(lista.Count, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural");
            AutomationProperties.SetName(PainelLista, $"{Idioma.Texto("A11y.ResultsList")}: {estadoLista}");
            AutomationProperties.SetItemStatus(PainelLista, estadoLista);
            AutomationProperties.SetName(LblVazio, Idioma.Texto("Vault.Empty"));

            foreach (var senha in lista)
            {
                var linha = new LinhaSenha(senha, ObterSenhaPlain, ObterTotpPlain, FavoritarToggle, FixarToggle, EditarSenha,
                    ExcluirSenhaAsync, RenomearServicoAsync, RegistrarCopiaLinhaAsync);
                linha.DefinirLargurasColunas(_larguraServico, _larguraUsuario, _larguraCategoria, _larguraData, _larguraAcoes);
                linha.DefinirModoPrivacidade(_modoPrivacidade);
                linha.SolicitouDetalhes += Linha_SolicitouDetalhes;
                linha.GotFocus += (s, e) => _linhaFocada = linha;

                var plain = ObterSenhaPlain(senha);
                if (!string.IsNullOrEmpty(plain))
                    linha.NivelForca = ForcaSenha.Calcular(plain);
                if (_itensAuditoria.TryGetValue(senha.Id, out var itemAuditoria))
                    linha.DefinirAuditoria(itemAuditoria);
                if (_vazamentosPorId.TryGetValue(senha.Id, out var vazamentos))
                    linha.Vazamentos = vazamentos;

                PainelLista.Children.Add(linha);
                _linhasSenha.Add(linha);
            }
        }

        private async Task CarregarLixeiraAsync()
        {
            try
            {
                _itensLixeira = await _servicoSenha.ListarLixeiraAsync();
                AtualizarListaLixeira();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.LoadError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void AtualizarListaLixeira()
        {
            PainelLista.Children.Clear();
            _linhasSenha.Clear();

            var lista = _itensLixeira
                .OrderByDescending(s => s.DataExclusao)
                .ToList();

            LblContadorHeader.Text = Idioma.Plural(lista.Count, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural");

            LblVazio.IsVisible = lista.Count == 0;
            TxtVazioMensagem.Text = Idioma.Texto("Trash.Empty");
            BtnVazioNovaSenha.IsVisible = false;
            AutomationProperties.SetName(LblVazio, Idioma.Texto("Trash.Empty"));

            foreach (var senha in lista)
                PainelLista.Children.Add(CriarLinhaLixeira(senha));
        }

        private Control CriarLinhaLixeira(Senha senha)
        {
            var icone = IconesServico.Obter(senha.NomeServico, senha.Url);
            var avatar = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(icone.Fundo),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            avatar.Child = new TextBlock
            {
                Text = icone.Texto,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(icone.Frente),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var lblServico = new TextBlock
            {
                Text = senha.NomeServico,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Tema.Pincel(Tema.TextPrimary)
            };
            var lblUsuario = new TextBlock
            {
                Text = senha.Usuario,
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.TextSecondary)
            };
            var info = new StackPanel { Spacing = 2, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            info.Children.Add(lblServico);
            info.Children.Add(lblUsuario);
            Grid.SetColumn(info, 1);

            var dataFormatada = senha.DataExclusao.HasValue
                ? senha.DataExclusao.Value.ToLocalTime().ToString("dd MMM yyyy", Idioma.CulturaAtual)
                : "";
            var lblData = new TextBlock
            {
                Text = Idioma.Formatar("Trash.DeletedOn", dataFormatada),
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.TextSecondary),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(12, 0)
            };
            Grid.SetColumn(lblData, 2);

            var btnRestaurar = new Button
            {
                Content = Idioma.Texto("Trash.Restore"),
                MinHeight = 34,
                FontSize = 12,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            btnRestaurar.Classes.Add("secundario");
            AutomationProperties.SetName(btnRestaurar, Idioma.Formatar("Trash.Restore") + " " + senha.NomeServico);
            btnRestaurar.Click += async (s, e) => await RestaurarDaLixeiraAsync(senha);
            Grid.SetColumn(btnRestaurar, 3);

            var btnExcluir = new Button
            {
                Width = 36,
                Height = 34,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            btnExcluir.Classes.Add("icone");
            btnExcluir.Content = Recursos.ImagemIcone("IconeExcluir", 22);
            ToolTip.SetTip(btnExcluir, Idioma.Texto("Trash.DeleteForever"));
            AutomationProperties.SetName(btnExcluir, Idioma.Formatar("Trash.DeleteForeverConfirm", senha.NomeServico));
            btnExcluir.Click += async (s, e) => await ExcluirDefinitivamenteAsync(senha);
            Grid.SetColumn(btnExcluir, 4);

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto"),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(avatar, 0);
            grid.Children.Add(avatar);
            grid.Children.Add(info);
            grid.Children.Add(lblData);
            grid.Children.Add(btnRestaurar);
            grid.Children.Add(btnExcluir);

            return new Border
            {
                Padding = new Thickness(14, 10),
                Margin = new Thickness(0, 0, 0, 6),
                CornerRadius = new CornerRadius(10),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Child = grid
            };
        }

        private async Task RestaurarDaLixeiraAsync(Senha senha)
        {
            try
            {
                await _servicoSenha.RestaurarSenhaAsync(senha.Id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", Idioma.Texto("Trash.Restore")));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Trash.RestoreError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task ExcluirDefinitivamenteAsync(Senha senha)
        {
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Trash.DeleteForeverConfirm", senha.NomeServico),
                Idioma.Texto("Trash.DeleteForever"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                _servicoAnexos?.RemoverTodos(senha);
                await _servicoSenha.RemoverDefinitivamenteAsync(senha.Id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Trash.DeleteForeverError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void EsvaziarLixeira_Click(object? sender, RoutedEventArgs e)
        {
            if (_itensLixeira.Count == 0)
                return;

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Trash.EmptyConfirm"),
                Idioma.Texto("Trash.EmptyConfirmTitle"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                if (_servicoAnexos != null)
                    foreach (var item in _itensLixeira)
                        _servicoAnexos.RemoverTodos(item);

                await _servicoSenha.EsvaziarLixeiraAsync();
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Trash.EmptyError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void LimparCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
                return;

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Vault.ClearConfirm"),
                Idioma.Texto("Vault.ClearTitle"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                await _servicoSenha.LimparCofreAsync();
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Vault.ClearError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void ExcluirCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Vault.DeleteTitle"), TipoMensagem.Aviso);
                return;
            }

            var dlg = new JanelaConfirmarSenhaMestra(
                Idioma.Texto("Vault.DeleteTitle"),
                Idioma.Texto("Vault.DeleteReauthInstruction"),
                Idioma.Texto("Vault.DeleteReauthButton"));
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Vault.DeleteConfirm"),
                Idioma.Texto("Vault.DeleteTitle"), TipoMensagem.Aviso);
            if (!confirmar)
                return;

            try
            {
                await _biometria.DesabilitarAsync();
                _servicoAnexos?.ApagarTudo();

                var persistencia = new PersistenciaLocal(_criptografia);
                await persistencia.ApagarTudoAsync();

                new AutenticacaoMestra().ExcluirSenhaMestra();

                Preferencias.UltimoBanco = null;
                Preferencias.Sincronizacao = null;
                Preferencias.Salvar();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Vault.DeleteError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                return;
            }

            await CaixaMensagem.MostrarAsync(this,
                Idioma.Texto("Vault.DeletedRestart"),
                Idioma.Texto("Vault.DeleteTitle"));
            Reiniciar();
        }

        private string? ObterSenhaPlain(Senha s)
        {
            try { return _criptografia?.Descriptografar(s.SenhaHash); }
            catch { return null; }
        }

        private string? ObterTotpPlain(Senha s)
        {
            if (string.IsNullOrEmpty(s.TotpSegredo))
                return null;
            try { return _criptografia?.Descriptografar(s.TotpSegredo); }
            catch { return null; }
        }

        private Dictionary<string, string> ObterCamposExtrasPlain(Senha s)
        {
            var resultado = new Dictionary<string, string>();
            if (_criptografia == null)
                return resultado;

            foreach (var (chave, valorCifrado) in s.CamposExtras)
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

        private List<HistoricoSenhaExportada> ObterHistoricoPlain(Senha s)
        {
            var historico = new List<HistoricoSenhaExportada>();
            if (_criptografia == null)
                return historico;

            foreach (var item in s.Historico)
            {
                try
                {
                    historico.Add(new HistoricoSenhaExportada
                    {
                        Senha = _criptografia.Descriptografar(item.SenhaHash),
                        DataAlteracao = item.DataAlteracao
                    });
                }
                catch
                {
                }
            }

            return historico;
        }

        private List<CodigoRecuperacaoExportado> ObterCodigosRecuperacaoPlain(Senha s)
        {
            var codigos = new List<CodigoRecuperacaoExportado>();
            if (_criptografia == null)
                return codigos;

            foreach (var item in s.CodigosRecuperacao)
            {
                try
                {
                    codigos.Add(new CodigoRecuperacaoExportado
                    {
                        Codigo = _criptografia.Descriptografar(item.Codigo),
                        Usado = item.Usado
                    });
                }
                catch
                {
                }
            }

            return codigos;
        }

        private async Task<List<AnexoExportado>> ObterAnexosExportadosAsync(Senha s)
        {
            var anexos = new List<AnexoExportado>();
            if (_servicoAnexos == null)
                return anexos;

            foreach (var item in s.Anexos)
            {
                try
                {
                    var bytes = await _servicoAnexos.LerAsync(item);
                    anexos.Add(new AnexoExportado
                    {
                        NomeArquivo = item.NomeArquivo,
                        ConteudoBase64 = Convert.ToBase64String(bytes)
                    });
                }
                catch
                {
                }
            }

            return anexos;
        }

        private void Filtro_Alterado(object? sender, SelectionChangedEventArgs e) => FiltrarSenhas();

        private void Busca_Alterada(object? sender, TextChangedEventArgs e) => FiltrarSenhas();

        private void FiltrarSenhas()
        {
            if (PainelLista == null) return;

            var termo = (TxtBusca.Text ?? "").Trim();
            var categoriaFiltro = (CmbCategoria.SelectedItem as FiltroOrganizacao)?.Categoria;
            var etiquetaFiltro = (CmbEtiqueta.SelectedItem as FiltroOrganizacao)?.Etiqueta;

            var filtradas = _senhasAtuais
                .Where(s => string.IsNullOrEmpty(termo) ||
                    s.NomeServico.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    s.Usuario.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    s.Etiquetas.Any(e => e.Contains(termo, StringComparison.OrdinalIgnoreCase)))
                .Where(s => categoriaFiltro == null || s.Categoria == categoriaFiltro)
                .Where(s => etiquetaFiltro == null ||
                    s.Etiquetas.Any(e => string.Equals(e, etiquetaFiltro, StringComparison.OrdinalIgnoreCase)))
                .Where(s => !_somenteFavoritos || s.Favorito)
                .Where(s => _filtroSeguranca == null || SenhaTemProblema(s, _filtroSeguranca.Value))
                .ToList();

            filtradas = _somenteRecentes
                ? filtradas
                    .OrderByDescending(s => s.DataAtualizacao)
                    .ThenByDescending(s => s.DataCriacao)
                    .ToList()
                : OrdenarPorColuna(filtradas);

            filtradas = filtradas.OrderByDescending(s => s.Fixado).ToList();

            AtualizarLista(filtradas);
        }

        private List<Senha> OrdenarPorColuna(List<Senha> lista) => _colunaOrdenacao switch
        {
            ColunaOrdenacao.Usuario => _ordenacaoDescendente
                ? lista.OrderByDescending(s => s.Usuario, StringComparer.CurrentCultureIgnoreCase).ToList()
                : lista.OrderBy(s => s.Usuario, StringComparer.CurrentCultureIgnoreCase).ToList(),
            ColunaOrdenacao.Categoria => _ordenacaoDescendente
                ? lista.OrderByDescending(RotuloOrdenacaoCategoria, StringComparer.CurrentCultureIgnoreCase).ToList()
                : lista.OrderBy(RotuloOrdenacaoCategoria, StringComparer.CurrentCultureIgnoreCase).ToList(),
            ColunaOrdenacao.Forca => _ordenacaoDescendente
                ? lista.OrderByDescending(NivelForcaDe).ToList()
                : lista.OrderBy(NivelForcaDe).ToList(),
            _ => _ordenacaoDescendente
                ? lista.OrderByDescending(s => s.NomeServico, StringComparer.CurrentCultureIgnoreCase).ToList()
                : lista.OrderBy(s => s.NomeServico, StringComparer.CurrentCultureIgnoreCase).ToList()
        };

        private static string RotuloOrdenacaoCategoria(Senha s) =>
            s.Categoria == Categoria.Other && s.Etiquetas.Count > 0
                ? s.Etiquetas[0]
                : CategoriasUI.Rotulo(s.Categoria);

        private int NivelForcaDe(Senha s)
        {
            var plain = ObterSenhaPlain(s);
            return string.IsNullOrEmpty(plain) ? -1 : ForcaSenha.Calcular(plain);
        }

        private bool SenhaTemProblema(Senha senha, CategoriaRelatorioSeguranca categoria) => categoria switch
        {
            CategoriaRelatorioSeguranca.Fraca => _itensAuditoria.TryGetValue(senha.Id, out var itemFraca) &&
                itemFraca.TemAchado(TipoAchadoAuditoriaSenha.Fraca),
            CategoriaRelatorioSeguranca.Repetida => _itensAuditoria.TryGetValue(senha.Id, out var itemRepetida) &&
                itemRepetida.TemAchado(TipoAchadoAuditoriaSenha.Repetida),
            CategoriaRelatorioSeguranca.Antiga => _itensAuditoria.TryGetValue(senha.Id, out var itemAntiga) &&
                itemAntiga.TemAchado(TipoAchadoAuditoriaSenha.Antiga),
            CategoriaRelatorioSeguranca.Comprometida => _vazamentosPorId.TryGetValue(senha.Id, out var contagem) && contagem > 0,
            CategoriaRelatorioSeguranca.SemTotp => string.IsNullOrEmpty(senha.TotpSegredo),
            CategoriaRelatorioSeguranca.SemUrl => string.IsNullOrWhiteSpace(senha.Url),
            CategoriaRelatorioSeguranca.SemCategoria => senha.Categoria == Categoria.Other && senha.Etiquetas.Count == 0,
            _ => false
        };

        private void AtualizarChipFiltroSeguranca()
        {
            if (BordaFiltroSeguranca == null) return;

            BordaFiltroSeguranca.IsVisible = _filtroSeguranca != null;
            if (_filtroSeguranca is { } categoria)
                LblFiltroSeguranca.Text = Idioma.Formatar("SecurityReport.FilterActive", Idioma.Texto(RotuloCategoriaSeguranca(categoria)));
        }

        private static string RotuloCategoriaSeguranca(CategoriaRelatorioSeguranca categoria) => categoria switch
        {
            CategoriaRelatorioSeguranca.Fraca => "SecurityReport.Weak",
            CategoriaRelatorioSeguranca.Repetida => "SecurityReport.Repeated",
            CategoriaRelatorioSeguranca.Antiga => "SecurityReport.Old",
            CategoriaRelatorioSeguranca.Comprometida => "SecurityReport.Compromised",
            CategoriaRelatorioSeguranca.SemTotp => "SecurityReport.NoTotp",
            CategoriaRelatorioSeguranca.SemUrl => "SecurityReport.NoUrl",
            CategoriaRelatorioSeguranca.SemCategoria => "SecurityReport.NoCategory",
            _ => "SecurityReport.Title"
        };

        private void LimparFiltroSeguranca_Click(object? sender, RoutedEventArgs e)
        {
            _filtroSeguranca = null;
            AtualizarChipFiltroSeguranca();
            FiltrarSenhas();
        }

        private void AtualizarFiltroOrganizacao()
        {
            if (CmbCategoria == null || CmbEtiqueta == null)
                return;

            AtualizarComboFiltro(CmbCategoria, ConstruirFiltrosCategoria());
            AtualizarComboFiltro(CmbEtiqueta, ConstruirFiltrosEtiqueta(_senhasAtuais));
        }

        private static void AtualizarComboFiltro(ComboBox combo, List<FiltroOrganizacao> filtros)
        {
            var selecionado = combo.SelectedItem as FiltroOrganizacao;
            combo.ItemsSource = filtros;

            if (selecionado != null)
            {
                var indice = filtros.FindIndex(f => f.MesmaSelecao(selecionado));
                if (indice >= 0)
                {
                    combo.SelectedIndex = indice;
                    return;
                }
            }

            combo.SelectedIndex = 0;
        }

        private static List<FiltroOrganizacao> ConstruirFiltrosCategoria()
        {
            var filtros = new List<FiltroOrganizacao> { FiltroOrganizacao.Todas() };
            var rotulos = CategoriasUI.Rotulos;
            for (int i = 0; i < rotulos.Length; i++)
                filtros.Add(FiltroOrganizacao.ParaCategoria(rotulos[i], (Categoria)i));

            return filtros;
        }

        private static List<FiltroOrganizacao> ConstruirFiltrosEtiqueta(IEnumerable<Senha> senhas)
        {
            var filtros = new List<FiltroOrganizacao> { FiltroOrganizacao.Todas() };
            foreach (var etiqueta in Etiquetas.Distintas(senhas))
                filtros.Add(FiltroOrganizacao.ParaEtiqueta(etiqueta));

            return filtros;
        }

        private void FiltroFavoritos_Click(object? sender, RoutedEventArgs e)
        {
            _somenteFavoritos = !_somenteFavoritos;
            _somenteRecentes = false;
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            FiltrarSenhas();
            Acessibilidade.Anunciar(BtnFavoritos, Idioma.Texto(_somenteFavoritos ? "A11y.FilterOn" : "A11y.FilterOff"));
        }

        private void PintarFiltroFavoritos()
        {
            if (_somenteFavoritos)
            {
                BtnFavoritos.Background = Tema.Pincel(Tema.AccentLight);
                BtnFavoritos.Foreground = Tema.Pincel(Tema.FavoriteColor);
            }
            else
            {
                BtnFavoritos.ClearValue(BackgroundProperty);
                BtnFavoritos.Foreground = Tema.Pincel(Tema.FavoriteBorderColor);
            }

            AutomationProperties.SetItemStatus(BtnFavoritos,
                Idioma.Texto(_somenteFavoritos ? "A11y.FilterOn" : "A11y.FilterOff"));
        }

        private void AtualizarContador()
        {
            int total = _senhasAtuais.Count;
            int favoritos = _senhasAtuais.Count(s => s.Favorito);
            LblContadorHeader.Text = Idioma.Plural(total, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural");
            var status = Idioma.Formatar("Vault.Status",
                total,
                Idioma.Texto(total == 1 ? "Vault.Status.PasswordSingular" : "Vault.Status.PasswordPlural"),
                favoritos,
                Idioma.Texto(favoritos == 1 ? "Vault.Status.FavoriteSingular" : "Vault.Status.FavoritePlural"));
            if (_resultadoAuditoria is { } auditoria)
            {
                status += " • " + (auditoria.TotalComAchados == 0
                    ? Idioma.Texto("Vault.Status.AuditOk")
                    : Idioma.Formatar("Vault.Status.WithAlert", auditoria.TotalComAchados));
            }

            LblStatus.Text = Idioma.Texto("Vault.Connection.Local");
            ToolTip.SetTip(LblStatus, status);
            AutomationProperties.SetName(LblStatus, $"{Idioma.Texto("A11y.VaultStatus")}: {LblStatus.Text}. {status}");
            AutomationProperties.SetName(LblContadorHeader, LblContadorHeader.Text ?? "");
        }

        private static Icone IconeJanela(string chave) => Recursos.ImagemIcone(chave, 28);

        private static string TextoBloqueioAutomatico()
        {
            int minutos = Preferencias.MinutosBloqueio;
            return minutos <= 0
                ? Idioma.Texto("Footer.AutoLockOff")
                : Idioma.Formatar("Footer.AutoLockCountdown", minutos);
        }

        private void ToggleGerador_Click(object? sender, RoutedEventArgs e)
        {
            if (PainelGeradorFlutuante.IsVisible)
                PainelGeradorFlutuante.IsVisible = false;
            else
                ExibirPainel(PainelGeradorFlutuante);
            AtualizarFabGerador();
        }

        private void FecharGerador_Click(object? sender, RoutedEventArgs e) => FecharGerador();

        private static void ExibirPainel(Border painel)
        {
            if (painel.IsVisible)
                return;

            painel.Transitions = null;

            if (Acessibilidade.ReduzirAnimacoes)
            {
                painel.Opacity = 1;
                painel.RenderTransform = null;
                painel.IsVisible = true;
                return;
            }

            painel.Opacity = 0;
            painel.RenderTransform = TransformOperations.Parse("translateY(10px)");
            painel.IsVisible = true;

            Dispatcher.UIThread.Post(() =>
            {
                painel.Transitions = new Transitions
                {
                    new DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(170),
                        Easing = new CubicEaseOut()
                    },
                    new TransformOperationsTransition
                    {
                        Property = RenderTransformProperty,
                        Duration = TimeSpan.FromMilliseconds(170),
                        Easing = new CubicEaseOut()
                    }
                };
                painel.Opacity = 1;
                painel.RenderTransform = TransformOperations.Parse("translateY(0px)");
            }, DispatcherPriority.Render);
        }

        private void FecharGerador()
        {
            PainelGeradorFlutuante.IsVisible = false;
            AtualizarFabGerador();
        }

        private void AtualizarFabGerador()
        {
            IconeFabGerador.RenderTransform = null;
        }

        private void ToggleNav_Click(object? sender, RoutedEventArgs e)
        {
            _navColapsada = !_navColapsada;

            NavRail.Transitions = Acessibilidade.ReduzirAnimacoes
                ? null
                : new Transitions
                {
                    new DoubleTransition
                    {
                        Property = WidthProperty,
                        Duration = TimeSpan.FromMilliseconds(150),
                        Easing = new CubicEaseOut()
                    }
                };
            NavRail.Width = _navColapsada ? 64 : 224;
            BtnFabGerador.Width = _navColapsada ? 40 : 60;
            BtnFabGerador.Height = _navColapsada ? 40 : 60;
            BtnFabGerador.CornerRadius = new CornerRadius(_navColapsada ? 20 : 30);
            BtnFabGerador.Margin = new Thickness(0, 0, 0, 16);
            IconeFabGerador.Width = _navColapsada ? 20 : 30;
            IconeFabGerador.Height = _navColapsada ? 20 : 30;

            foreach (var texto in TextosNav())
                texto.IsVisible = !_navColapsada;

            var padding = _navColapsada ? new Thickness(0, 12) : new Thickness(16, 12);
            foreach (var botao in BotoesNav())
                botao.Padding = padding;

            LblCategoriasNav.IsVisible = !_navColapsada;
        }

        private IEnumerable<TextBlock> TextosNav()
        {
            yield return LblNavCofre;
            yield return LblNavFavoritas;
            yield return LblNavRecentes;
            yield return LblNavLixeira;
            yield return LblCatJogos;
            yield return LblCatRedes;
            yield return LblCatEmail;
            yield return LblCatFinanceiro;
            yield return LblCatOutro;
        }

        private IEnumerable<Button> BotoesNav()
        {
            yield return BtnNavCofre;
            yield return BtnNavFavoritas;
            yield return BtnNavRecentes;
            yield return BtnNavLixeira;
            yield return BtnCatPessoal;
            yield return BtnCatSocial;
            yield return BtnCatTrabalho;
            yield return BtnCatFinancas;
            yield return BtnCatOutro;
        }

        private void NavCofre_Click(object? sender, RoutedEventArgs e)
        {
            SairDaLixeira();
            _somenteFavoritos = false;
            _somenteRecentes = false;
            if (CmbCategoria.SelectedIndex != 0)
                CmbCategoria.SelectedIndex = 0;
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private void NavFavoritas_Click(object? sender, RoutedEventArgs e)
        {
            SairDaLixeira();
            _somenteFavoritos = true;
            _somenteRecentes = false;
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private void NavRecentes_Click(object? sender, RoutedEventArgs e)
        {
            SairDaLixeira();
            _somenteFavoritos = false;
            _somenteRecentes = true;
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private async void NavLixeira_Click(object? sender, RoutedEventArgs e)
        {
            FecharDetalhes();
            FecharGerador();
            _naLixeira = true;
            _filtroSeguranca = null;
            AtualizarChipFiltroSeguranca();
            AjustarBarraFerramentas(lixeira: true);
            AtualizarNavegacao();
            await CarregarLixeiraAsync();
        }

        private void SairDaLixeira()
        {
            if (!_naLixeira)
                return;

            _naLixeira = false;
            AjustarBarraFerramentas(lixeira: false);
            AtualizarContador();
        }

        private void AjustarBarraFerramentas(bool lixeira)
        {
            TxtBusca.IsVisible = !lixeira;
            GridFiltroCategoria.IsVisible = !lixeira;
            BtnOrdenar.IsVisible = !lixeira;
            BtnVazamentos.IsVisible = !lixeira;
            BtnAuditoria.IsVisible = !lixeira;
            BtnRelatorioSeguranca.IsVisible = !lixeira;
            BtnFavoritos.IsVisible = !lixeira;
            DivisorAcoesLista.IsVisible = !lixeira;
            BtnNovaSenha.IsVisible = !lixeira;
            BtnEsvaziarLixeira.IsVisible = lixeira;
            BordaCabecalhoTabela.IsVisible = !lixeira;
            LblTituloVault.Text = Idioma.Texto(lixeira ? "Nav.Trash" : "Vault.Header");
        }

        private void NavCategoria_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string tag } || !Enum.TryParse<Categoria>(tag, out var categoria))
                return;

            SairDaLixeira();
            _somenteFavoritos = false;
            _somenteRecentes = false;
            SelecionarCategoria(categoria);
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private void SelecionarCategoria(Categoria categoria)
        {
            if (CmbCategoria.ItemsSource is not IEnumerable<FiltroOrganizacao> filtros)
                return;

            int indice = filtros.ToList().FindIndex(f => f.Categoria == categoria);
            if (indice >= 0)
                CmbCategoria.SelectedIndex = indice;
        }

        private void Ordenar_Click(object? sender, RoutedEventArgs e)
        {
            _ordenacaoDescendente = !_ordenacaoDescendente;
            _somenteRecentes = false;
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private void OrdenarColuna_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_naLixeira || sender is not Control { Tag: string tag } ||
                !Enum.TryParse<ColunaOrdenacao>(tag, out var coluna))
                return;

            if (_colunaOrdenacao == coluna)
            {
                _ordenacaoDescendente = !_ordenacaoDescendente;
            }
            else
            {
                _colunaOrdenacao = coluna;
                _ordenacaoDescendente = false;
            }
            _somenteRecentes = false;

            AtualizarNavegacao();
            FiltrarSenhas();
            e.Handled = true;
        }

        private void AtualizarNavegacao()
        {
            DefinirNavAtivo(BtnNavCofre, !_naLixeira && !_somenteFavoritos && !_somenteRecentes);
            DefinirNavAtivo(BtnNavFavoritas, !_naLixeira && _somenteFavoritos);
            DefinirNavAtivo(BtnNavRecentes, !_naLixeira && _somenteRecentes);
            DefinirNavAtivo(BtnNavLixeira, _naLixeira);
            AtualizarSetasOrdenacao();
        }

        private void AtualizarSetasOrdenacao()
        {
            var seta = _ordenacaoDescendente ? "▼" : "▲";
            AtualizarSetaColuna(SetaOrdenacaoServico, ColunaOrdenacao.Servico, seta);
            AtualizarSetaColuna(SetaOrdenacaoUsuario, ColunaOrdenacao.Usuario, seta);
            AtualizarSetaColuna(SetaOrdenacaoCategoria, ColunaOrdenacao.Categoria, seta);
            AtualizarSetaColuna(SetaOrdenacaoForca, ColunaOrdenacao.Forca, seta);
        }

        private void AtualizarSetaColuna(TextBlock rotulo, ColunaOrdenacao coluna, string seta)
        {
            bool ativa = !_somenteRecentes && !_naLixeira && _colunaOrdenacao == coluna;
            rotulo.IsVisible = ativa;
            rotulo.Text = ativa ? seta : "";
        }

        private static void DefinirNavAtivo(Button botao, bool ativo)
        {
            if (ativo && !botao.Classes.Contains("ativo"))
                botao.Classes.Add("ativo");
            else if (!ativo)
                botao.Classes.Remove("ativo");
        }

        private void Linha_SolicitouDetalhes(object? sender, Senha senha) => AbrirDetalhes(senha);

        private void AbrirDetalhes(Senha senha)
        {
            _senhaDetalhe = senha;
            _senhaDetalhePlain = ObterSenhaPlain(senha) ?? "";
            _senhaDetalheVisivel = false;

            TxtDetalheServico.Text = senha.NomeServico;
            TxtDetalheUsuario.Text = senha.Usuario;
            TxtDetalheUrl.Text = senha.Url ?? "";
            TxtDetalheNotas.Text = senha.Notas ?? "";

            LblDetalheUsuario.Text = TemplatesCredencial.RotuloUsuario(senha.Tipo);
            LblDetalheSenha.Text = TemplatesCredencial.RotuloSenha(senha.Tipo);

            TxtDetalheEtiquetas.Text = Etiquetas.Formatar(senha.Etiquetas);
            CmbDetalheCategoria.ItemsSource = CategoriasUI.Rotulos;
            CmbDetalheCategoria.SelectedIndex = (int)senha.Categoria;

            AtualizarDetalheVisual();
            AtualizarHistoricoDetalhes();
            AtualizarSenhaDetalhe();
            AtualizarTotpDetalhes();
            ExibirPainel(PainelDetalhes);
        }

        private void AtualizarHistoricoDetalhes()
        {
            if (_senhaDetalhe == null)
                return;

            LblDetalheCriada.Text = Idioma.Formatar("Entry.Usage.Created", FormatarDataDetalhe(_senhaDetalhe.DataCriacao));
            LblDetalheAtualizada.Text = Idioma.Formatar("Entry.Usage.Updated", FormatarDataDetalhe(_senhaDetalhe.DataAtualizacao));
            LblDetalheCopiaSenha.Text = Idioma.Formatar("Entry.Usage.CopyPasswordLabel", FormatarDataOuNunca(_senhaDetalhe.DataUltimaCopiaSenha));
            LblDetalheCopiaUsuario.Text = Idioma.Formatar("Entry.Usage.CopyUserLabel", FormatarDataOuNunca(_senhaDetalhe.DataUltimaCopiaUsuario));
            LblDetalheCopiaTotp.Text = Idioma.Formatar("Entry.Usage.CopyTotpLabel", FormatarDataOuNunca(_senhaDetalhe.DataUltimaCopiaTotp));
            LblDetalheCopiaTotp.IsVisible = _senhaDetalhe.TotpSegredo != null;
        }

        private static string FormatarDataDetalhe(DateTime data) =>
            data.ToLocalTime().ToString("dd MMM yyyy", Idioma.CulturaAtual);

        private static string FormatarDataOuNunca(DateTime? data) =>
            data.HasValue ? FormatarDataDetalhe(data.Value) : Idioma.Texto("Entry.Usage.Never");

        private void AtualizarDetalheVisual()
        {
            if (_senhaDetalhe == null || AvatarDetalhe == null)
                return;

            var icone = IconesServico.Obter(TxtDetalheServico.Text ?? _senhaDetalhe.NomeServico, TxtDetalheUrl.Text);
            AvatarDetalhe.Background = new SolidColorBrush(icone.Fundo);
            TxtAvatarDetalhe.Text = icone.Texto;
            TxtAvatarDetalhe.Foreground = new SolidColorBrush(icone.Frente);
            ToolTip.SetTip(AvatarDetalhe, TxtDetalheServico.Text ?? _senhaDetalhe.NomeServico);

            var (bg, fg) = Acessibilidade.CoresCategoria(_senhaDetalhe.Categoria);
            BadgeDetalheCategoria.Background = new SolidColorBrush(bg);
            TxtDetalheCategoria.Foreground = new SolidColorBrush(fg);
            TxtDetalheCategoria.Text = _senhaDetalhe.Categoria == Categoria.Other && _senhaDetalhe.Etiquetas.Count > 0
                ? string.Join(", ", _senhaDetalhe.Etiquetas)
                : CategoriasUI.Rotulo(_senhaDetalhe.Categoria);
        }

        private void AtualizarSenhaDetalhe()
        {
            TxtDetalheSenha.Text = _senhaDetalheVisivel
                ? _senhaDetalhePlain
                : new string('•', Math.Max(8, _senhaDetalhePlain.Length));
            TxtDetalheSenha.IsReadOnly = !_senhaDetalheVisivel;
            BtnDetalheRevelar.Content = Recursos.ImagemIcone(_senhaDetalheVisivel ? "IconeOcultar" : "IconeRevelar", 22);
            ToolTip.SetTip(BtnDetalheRevelar, Idioma.Texto(_senhaDetalheVisivel ? "Row.HidePassword" : "Row.RevealPassword"));
        }

        private (Categoria categoria, List<string> etiquetas) LerCategoriaDetalhes()
        {
            var categoria = (Categoria)Math.Max(0, CmbDetalheCategoria.SelectedIndex);
            var etiquetas = Etiquetas.Analisar(TxtDetalheEtiquetas.Text);

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

        private void AtualizarTotpDetalhes()
        {
            var segredo = _senhaDetalhe != null ? ObterTotpPlain(_senhaDetalhe) : null;
            if (string.IsNullOrEmpty(segredo) || !_totp.SegredoValido(segredo))
            {
                PainelDetalheTotp.IsVisible = false;
                _timerTotpDetalhe.Parar();
                return;
            }

            try
            {
                var codigo = _totp.Gerar(segredo);
                LblDetalheCodigoTotp.Text = TotpPreview.FormatarCodigo(codigo.Codigo);
                var contagem = Idioma.Formatar("Entry.TotpExpiresIn", codigo.SegundosRestantes);
                AnelDetalheTotp.Data = TotpPreview.ConstruirAnelProgresso(codigo.SegundosRestantes, PeriodoTotpDetalhe, raio: 9, centro: 12);
                AutomationProperties.SetName(LblDetalheCodigoTotp,
                    $"{Idioma.Texto("A11y.TotpPreview")}: {LblDetalheCodigoTotp.Text}. {contagem}");
                PainelDetalheTotp.IsVisible = true;
                _timerTotpDetalhe.Garantir(AtualizarTotpDetalhes);
            }
            catch
            {
                PainelDetalheTotp.IsVisible = false;
                _timerTotpDetalhe.Parar();
            }
        }

        private async void CopiarTotpDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            var segredo = _senhaDetalhe != null ? ObterTotpPlain(_senhaDetalhe) : null;
            if (string.IsNullOrEmpty(segredo))
                return;

            string codigo;
            try { codigo = _totp.Gerar(segredo).Codigo; }
            catch { return; }

            await CopiarDetalheAsync(codigo, Idioma.Texto("Row.CopyTotp"), campoRegistrado: TipoCampoCopiado.Totp);
        }

        private async void EdicaoCompletaDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null)
                return;

            var id = _senhaDetalhe.Id;
            var dlg = new JanelaEditarSenha(_servicoSenha, _senhaDetalhe, _criptografia, _servicoAnexos);
            if (await AbrirDialogoAsync<bool>(dlg))
                await CarregarSenhasAsync();

            var atualizada = _senhasAtuais.FirstOrDefault(s => s.Id == id);
            if (atualizada != null)
                AbrirDetalhes(atualizada);
            else
                FecharDetalhes();
        }

        private void FecharDetalhes_Click(object? sender, RoutedEventArgs e) => FecharDetalhes();

        private void FecharDetalhes()
        {
            PainelDetalhes.IsVisible = false;
            _senhaDetalhe = null;
            _senhaDetalhePlain = "";
            _senhaDetalheVisivel = false;
            TxtDetalheSenha.Text = "";
            _timerTotpDetalhe.Parar();
            PainelDetalheTotp.IsVisible = false;
        }

        private async void ExcluirDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null)
                return;

            var id = _senhaDetalhe.Id;
            await ExcluirSenhaAsync(_senhaDetalhe);
            if (_senhasAtuais.All(s => s.Id != id))
                FecharDetalhes();
        }

        private async void SalvarDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null)
                return;

            var servico = (TxtDetalheServico.Text ?? "").Trim();
            var usuario = (TxtDetalheUsuario.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(servico) || string.IsNullOrWhiteSpace(usuario))
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Entry.EditRequired"), Idioma.Texto("Common.Validation"), TipoMensagem.Aviso);
                return;
            }

            var senhaPlain = _senhaDetalheVisivel ? TxtDetalheSenha.Text : _senhaDetalhePlain;
            if (string.IsNullOrEmpty(senhaPlain))
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Entry.RecoverCurrentPasswordError"),
                    Idioma.Texto("Entry.EditTitle"), TipoMensagem.Aviso);
                return;
            }

            try
            {
                var id = _senhaDetalhe.Id;
                var (categoria, etiquetas) = LerCategoriaDetalhes();
                await _servicoSenha.AtualizarSenhaAsync(
                    id,
                    servico,
                    usuario,
                    senhaPlain,
                    categoria,
                    string.IsNullOrWhiteSpace(TxtDetalheUrl.Text) ? null : TxtDetalheUrl.Text,
                    string.IsNullOrWhiteSpace(TxtDetalheNotas.Text) ? null : TxtDetalheNotas.Text,
                    etiquetas);

                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();

                var atualizada = _senhasAtuais.FirstOrDefault(s => s.Id == id);
                if (atualizada != null)
                    AbrirDetalhes(atualizada);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.UpdateError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void RevelarSenhaDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalheVisivel)
                _senhaDetalhePlain = TxtDetalheSenha.Text ?? "";

            _senhaDetalheVisivel = !_senhaDetalheVisivel;
            AtualizarSenhaDetalhe();
        }

        private async void CopiarUsuarioDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(TxtDetalheUsuario.Text, Idioma.Texto("Row.CopyUser"), campoRegistrado: TipoCampoCopiado.Usuario);

        private async void CopiarSenhaDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(_senhaDetalheVisivel ? TxtDetalheSenha.Text : _senhaDetalhePlain,
                Idioma.Texto("Row.CopyPassword"), limparDepois: true, campoRegistrado: TipoCampoCopiado.Senha);

        private async void CopiarUrlDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(TxtDetalheUrl.Text, "URL");

        private async Task CopiarDetalheAsync(string? texto, string rotulo, bool limparDepois = false,
            TipoCampoCopiado? campoRegistrado = null)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(texto); } catch { }
            }

            int segundos = Preferencias.SegundosLimpezaClipboard;
            if (limparDepois && segundos > 0 && clipboard != null)
            {
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.CopiedWillClear", rotulo, segundos));
                AgendarFeedbackLimpezaSenhaDetalhes(segundos);
                _ = ServicoLimpezaClipboard.ProgramarLimpezaAsync(new AreaTransferenciaAvalonia(clipboard), texto, segundos);
            }
            else
            {
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", rotulo));
            }

            if (campoRegistrado.HasValue && Preferencias.RegistrarHistoricoUso && _senhaDetalhe != null)
            {
                await _servicoSenha.RegistrarCopiaAsync(_senhaDetalhe.Id, campoRegistrado.Value);
                await _servicoSenha.PersistirAsync();
                AtualizarHistoricoDetalhes();
            }
        }

        private void AgendarFeedbackLimpezaSenhaDetalhes(int segundos)
        {
            var mensagem = Idioma.Formatar("Row.PasswordCopiedClearing", segundos);
            ToolTip.SetTip(BtnCopiarSenhaDetalhes, mensagem);
            AutomationProperties.SetName(BtnCopiarSenhaDetalhes, mensagem);

            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Min(segundos, 3)) };
            t.Tick += (s, e) =>
            {
                BtnCopiarSenhaDetalhes.ClearValue(ToolTip.TipProperty);
                BtnCopiarSenhaDetalhes.ClearValue(AutomationProperties.NameProperty);
                t.Stop();
            };
            t.Start();
        }

        private async Task RegistrarCopiaLinhaAsync(Senha senha, TipoCampoCopiado campo)
        {
            await _servicoSenha.RegistrarCopiaAsync(senha.Id, campo);
            await _servicoSenha.PersistirAsync();

            if (_senhaDetalhe != null && _senhaDetalhe.Id == senha.Id)
                AtualizarHistoricoDetalhes();
        }

        private async void FavoritarToggle(Senha s)
        {
            try
            {
                if (s.Favorito) await _servicoSenha.RemoverDeFavoritoAsync(s.Id);
                else await _servicoSenha.MarcarComoFavoritoAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.FavoriteError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void FixarToggle(Senha s)
        {
            try
            {
                if (s.Fixado) await _servicoSenha.RemoverFixacaoAsync(s.Id);
                else await _servicoSenha.MarcarComoFixadoAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.PinError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void EditarSenha(Senha s)
        {
            var dlg = new JanelaEditarSenha(_servicoSenha, s, _criptografia, _servicoAnexos);
            if (await AbrirDialogoAsync<bool>(dlg))
                await CarregarSenhasAsync();
        }

        private async Task ExcluirSenhaAsync(Senha s)
        {
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Message.DeletePrompt", s.NomeServico, Idioma.Texto("Message.MoveToTrash")),
                Idioma.Texto("Message.DeleteTitle"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                await _servicoSenha.RemoverSenhaAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                RemoverSenhaDaLista(s.Id);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.DeleteError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void RemoverSenhaDaLista(Guid id)
        {
            _senhasAtuais.RemoveAll(s => s.Id == id);
            _itensAuditoria.Remove(id);
            _vazamentosPorId.Remove(id);
            if (_senhaDetalhe?.Id == id)
                FecharDetalhes();
            AtualizarFiltroOrganizacao();
            FiltrarSenhas();
            AtualizarContador();
        }

        private async Task RenomearServicoAsync(Senha s, string novoNome)
        {
            try
            {
                string nome = novoNome.Trim();
                if (string.IsNullOrWhiteSpace(nome) ||
                    string.Equals(nome, s.NomeServico, StringComparison.Ordinal))
                    return;

                var plain = ObterSenhaPlain(s);
                if (string.IsNullOrEmpty(plain))
                    throw new InvalidOperationException(Idioma.Texto("Message.RenameDecryptError"));

                await _servicoSenha.AtualizarSenhaAsync(s.Id, nome, s.Usuario, plain, s.Categoria, s.Url, s.Notas, s.Etiquetas);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.RenameError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                throw;
            }
        }

        private async void AuditarCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Message.AuditNoPasswords"),
                    Idioma.Texto("Message.AuditTitle"));
                return;
            }

            var conteudoOriginal = BtnAuditoria.Content;
            BtnAuditoria.IsEnabled = false;
            BtnAuditoria.Content = "…";

            try
            {
                var resultado = ExecutarAuditoria();
                FiltrarSenhas();
                AtualizarContador();

                await CaixaMensagem.MostrarAsync(this, MontarMensagemAuditoria(resultado), Idioma.Texto("Message.AuditTitle"),
                    resultado.TotalComAchados == 0 ? TipoMensagem.Info : TipoMensagem.Aviso);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.AuditError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
            finally
            {
                BtnAuditoria.Content = conteudoOriginal;
                BtnAuditoria.IsEnabled = true;
            }
        }

        private ResultadoAuditoriaCofre ExecutarAuditoria()
        {
            var resultado = _servicoAuditoria.Auditar(_senhasAtuais, ObterSenhaPlain);
            _resultadoAuditoria = resultado;
            _itensAuditoria.Clear();
            foreach (var item in resultado.Itens)
                _itensAuditoria[item.Senha.Id] = item;

            return resultado;
        }

        private void LimparAuditoria()
        {
            _resultadoAuditoria = null;
            _itensAuditoria.Clear();
            _vazamentosPorId.Clear();
            _filtroSeguranca = null;
            AtualizarChipFiltroSeguranca();
        }

        private static string MontarMensagemAuditoria(ResultadoAuditoriaCofre resultado)
        {
            if (resultado.TotalComAchados == 0)
            {
                var msg = Idioma.Formatar("Message.AuditSuccess", resultado.TotalSenhas);
                if (resultado.NaoAuditadas > 0)
                    msg += "\n" + Idioma.Formatar("Message.AuditIncomplete", resultado.NaoAuditadas);
                return msg;
            }

            var linhas = new List<string>
            {
                Idioma.Formatar("Message.AuditFoundHeader", resultado.TotalComAchados, resultado.TotalSenhas),
                Idioma.Formatar("Message.AuditWeakLine", resultado.TotalFracas),
                Idioma.Formatar("Message.AuditRepeatedLine", resultado.TotalRepetidas),
                Idioma.Formatar("Message.AuditOldLine", resultado.TotalAntigas)
            };

            if (resultado.NaoAuditadas > 0)
                linhas.Add(Idioma.Formatar("Message.AuditUnreadableLine", resultado.NaoAuditadas));

            linhas.Add("");
            linhas.Add(Idioma.Texto("Message.AuditMarked"));
            linhas.Add(Idioma.Formatar("Message.AuditOldDefinition", ServicoAuditoriaSenha.DiasSenhaAntigaPadrao));

            return string.Join(Environment.NewLine, linhas);
        }

        private async void VerificarVazamentos_Click(object? sender, RoutedEventArgs e)
        {
            if (_linhasSenha.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Message.BreachNoPasswords"),
                    Idioma.Texto("Message.BreachTitle"));
                return;
            }

            var conteudoOriginal = BtnVazamentos.Content;
            BtnVazamentos.IsEnabled = false;
            BtnVazamentos.Content = "…";

            int comprometidas = 0;
            int verificadas = 0;
            try
            {
                foreach (var linha in _linhasSenha)
                {
                    var plain = ObterSenhaPlain(linha.Senha);
                    if (string.IsNullOrEmpty(plain)) continue;

                    int contagem = await _servicoVazamento.VerificarAsync(plain);
                    linha.Vazamentos = contagem;
                    _vazamentosPorId[linha.Senha.Id] = contagem;
                    if (contagem > 0) comprometidas++;
                    verificadas++;
                }

                string msg = comprometidas == 0
                    ? Idioma.Formatar("Message.BreachSuccess", verificadas)
                    : Idioma.Formatar("Message.BreachWarning", comprometidas, verificadas);

                await CaixaMensagem.MostrarAsync(this, msg, Idioma.Texto("Message.BreachDoneTitle"),
                    comprometidas == 0 ? TipoMensagem.Info : TipoMensagem.Aviso);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.BreachNetworkError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Message.NetworkErrorTitle"), TipoMensagem.Erro);
            }
            finally
            {
                BtnVazamentos.Content = conteudoOriginal;
                BtnVazamentos.IsEnabled = true;
            }
        }

        private async void RelatorioSeguranca_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Message.AuditNoPasswords"),
                    Idioma.Texto("SecurityReport.Title"));
                return;
            }

            ExecutarAuditoria();
            var relatorio = ServicoRelatorioSeguranca.Gerar(_senhasAtuais, _resultadoAuditoria!, _vazamentosPorId);
            bool jaVerificouVazamentos = _vazamentosPorId.Count > 0;

            var dlg = new JanelaRelatorioSeguranca(relatorio, jaVerificouVazamentos, GerarRelatorioAtualizadoAsync);
            await AbrirDialogoAsync<bool>(dlg);

            if (dlg.CategoriaSelecionada is { } categoria)
            {
                SairDaLixeira();
                _somenteFavoritos = false;
                _somenteRecentes = false;
                _filtroSeguranca = categoria;
                PintarFiltroFavoritos();
                AtualizarNavegacao();
            }

            AtualizarChipFiltroSeguranca();
            FiltrarSenhas();
            AtualizarContador();
        }

        private async Task<RelatorioSegurancaCofre> GerarRelatorioAtualizadoAsync()
        {
            await VerificarVazamentosDoVaultAsync();
            return ServicoRelatorioSeguranca.Gerar(_senhasAtuais, _resultadoAuditoria!, _vazamentosPorId);
        }

        private async Task VerificarVazamentosDoVaultAsync()
        {
            foreach (var senha in _senhasAtuais)
            {
                var plain = ObterSenhaPlain(senha);
                if (string.IsNullOrEmpty(plain)) continue;

                int contagem = await _servicoVazamento.VerificarAsync(plain);
                _vazamentosPorId[senha.Id] = contagem;
            }

            foreach (var linha in _linhasSenha)
                if (_vazamentosPorId.TryGetValue(linha.Senha.Id, out var contagem))
                    linha.Vazamentos = contagem;
        }

        private async void Exportar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var senhas = await _servicoSenha.ListarTodosAsync();
                if (senhas.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Message.ExportEmpty"),
                        Idioma.Texto("Common.Export"));
                    return;
                }

                var dlg = new JanelaSenhaExportacao(modoExportar: true);
                if (!await AbrirDialogoAsync<bool>(dlg))
                    return;

                var arquivo = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = Idioma.Texto("ExportDialog.ExportTitle"),
                    SuggestedFileName = $"cofre-senhas-{DateTime.Now:yyyy-MM-dd}.gsenhas",
                    DefaultExtension = "gsenhas",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType(Idioma.Texto("Common.ExportedVaultFile")) { Patterns = new[] { "*.gsenhas" } },
                        new FilePickerFileType(Idioma.Texto("Common.AllFiles")) { Patterns = new[] { "*" } }
                    }
                });
                if (arquivo == null)
                    return;

                var itens = new List<SenhaExportada>();
                foreach (var s in senhas)
                {
                    var plain = ObterSenhaPlain(s);
                    if (plain == null) continue;
                    itens.Add(new SenhaExportada
                    {
                        NomeServico = s.NomeServico,
                        Usuario = s.Usuario,
                        Senha = plain,
                        Url = s.Url,
                        Categoria = s.Categoria,
                        Etiquetas = s.Etiquetas.ToList(),
                        Notas = s.Notas,
                        Tipo = s.Tipo,
                        CamposExtras = ObterCamposExtrasPlain(s),
                        TotpSegredo = ObterTotpPlain(s),
                        Historico = ObterHistoricoPlain(s),
                        CodigosRecuperacao = ObterCodigosRecuperacaoPlain(s),
                        Anexos = await ObterAnexosExportadosAsync(s),
                        Favorito = s.Favorito,
                        DataCriacao = s.DataCriacao,
                        DataAtualizacao = s.DataAtualizacao
                    });
                }

                await _servicoExportacao.ExportarAsync(arquivo.Path.LocalPath, itens, dlg.SenhaInformada);

                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ExportSuccess", itens.Count),
                    Idioma.Texto("Common.Export"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ExportError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void Importar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var arquivos = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = Idioma.Texto("ExportDialog.ImportTitle"),
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(Idioma.Texto("Common.ExportedVaultFile")) { Patterns = new[] { "*.gsenhas" } },
                        new FilePickerFileType(Idioma.Texto("Common.AllFiles")) { Patterns = new[] { "*" } }
                    }
                });
                if (arquivos.Count == 0)
                    return;

                var dlg = new JanelaSenhaExportacao(modoExportar: false);
                if (!await AbrirDialogoAsync<bool>(dlg))
                    return;

                List<SenhaExportada> itens;
                try
                {
                    itens = await _servicoExportacao.ImportarAsync(arquivos[0].Path.LocalPath, dlg.SenhaInformada);
                }
                catch (ErroLocalizavel ex)
                {
                    await CaixaMensagem.MostrarAsync(this, ErrosUi.MensagemAmigavel(ex), Idioma.Texto("Common.Import"), TipoMensagem.Aviso);
                    return;
                }

                if (itens.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Message.ImportEmpty"),
                        Idioma.Texto("Common.Import"));
                    return;
                }

                var (adicionadas, invalidas, duplicadas) = await ImportarComProgressoAsync(itens);

                var msg = Idioma.Formatar("Message.ImportSuccess", adicionadas);
                if (invalidas > 0)
                    msg += "\n" + Idioma.Formatar("Message.ImportIgnored", invalidas);
                if (duplicadas > 0)
                    msg += "\n" + Idioma.Formatar("Message.ImportDuplicates", duplicadas);
                await CaixaMensagem.MostrarAsync(this, msg, Idioma.Texto("Common.Import"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ImportError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void ImportarCsv_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var arquivos = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = Idioma.Texto("Settings.ImportCsv"),
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(Idioma.Texto("Common.CsvFile")) { Patterns = new[] { "*.csv" } },
                        new FilePickerFileType(Idioma.Texto("Common.AllFiles")) { Patterns = new[] { "*" } }
                    }
                });
                if (arquivos.Count == 0)
                    return;

                ResultadoImportacaoCsv resultado;
                try
                {
                    resultado = _servicoImportacaoCsv.ImportarArquivo(arquivos[0].Path.LocalPath);
                }
                catch (ErroLocalizavel ex)
                {
                    await CaixaMensagem.MostrarAsync(this, ErrosUi.MensagemAmigavel(ex), Idioma.Texto("Settings.ImportCsv"), TipoMensagem.Aviso);
                    return;
                }

                if (resultado.Itens.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Message.CsvEmpty"),
                        Idioma.Texto("Settings.ImportCsv"));
                    return;
                }

                var itensPreview = resultado.Itens
                    .Select(i => $"{i.NomeServico} — {i.Usuario}")
                    .ToList();
                var confirmar = await CaixaMensagem.ConfirmarComListaAsync(this,
                    Idioma.Formatar("Message.CsvConfirm", resultado.FormatoDetectado, resultado.Itens.Count),
                    Idioma.Texto("Settings.ImportCsv"), itensPreview);
                if (!confirmar)
                    return;

                var (adicionadas, invalidas, duplicadas) = await ImportarComProgressoAsync(resultado.Itens);
                invalidas += resultado.LinhasIgnoradas;

                var msg = Idioma.Formatar("Message.ImportSuccess", adicionadas);
                if (invalidas > 0)
                    msg += "\n" + Idioma.Formatar("Message.CsvIgnored", invalidas);
                if (duplicadas > 0)
                    msg += "\n" + Idioma.Formatar("Message.CsvDuplicates", duplicadas);
                msg += "\n\n" + Idioma.Texto("Message.CsvSecurity");
                await CaixaMensagem.MostrarAsync(this, msg, Idioma.Texto("Settings.ImportCsv"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ImportError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task<(int adicionadas, int invalidas, int duplicadas)> AplicarImportacaoAsync(
            List<SenhaExportada> itens, Action<int, int>? aoProgredir = null)
        {
            var existentes = await _servicoSenha.ListarTodosAsync();
            var chaves = new HashSet<string>(
                existentes.Select(s => s.NomeServico + " " + s.Usuario),
                StringComparer.OrdinalIgnoreCase);

            int adicionadas = 0, invalidas = 0, duplicadas = 0, processadas = 0;
            foreach (var item in itens)
            {
                if (string.IsNullOrWhiteSpace(item.NomeServico) ||
                    string.IsNullOrWhiteSpace(item.Usuario) ||
                    string.IsNullOrWhiteSpace(item.Senha))
                {
                    invalidas++;
                }
                else if (!chaves.Add(item.NomeServico + " " + item.Usuario))
                {
                    duplicadas++;
                }
                else
                {
                    var totp = _totp.SegredoValido(item.TotpSegredo) ? item.TotpSegredo : null;
                    var nova = await _servicoSenha.CriarSenhaAsync(
                        item.NomeServico, item.Usuario, item.Senha, item.Categoria, item.Url, item.Notas, totp, item.Etiquetas,
                        item.Tipo, item.CamposExtras);
                    if (item.Favorito)
                        await _servicoSenha.MarcarComoFavoritoAsync(nova.Id);
                    RestaurarHistorico(nova, item.Historico);
                    if (item.CodigosRecuperacao is { Count: > 0 })
                        await _servicoSenha.AdicionarCodigosRecuperacaoAsync(nova.Id,
                            item.CodigosRecuperacao.Select(c => (c.Codigo, c.Usado)));
                    await RestaurarAnexosAsync(nova, item.Anexos);
                    adicionadas++;
                }

                processadas++;
                aoProgredir?.Invoke(processadas, itens.Count);
            }

            await _servicoSenha.PersistirAsync();
            await CarregarSenhasAsync();

            return (adicionadas, invalidas, duplicadas);
        }

        private void MostrarProgressoImportacao()
        {
            BarraProgressoImportacao.Value = 0;
            LblProgressoImportacao.Text = Idioma.Formatar("Import.Progress", 0, 0);
            PainelProgressoImportacao.IsVisible = true;
        }

        private void AtualizarProgressoImportacao(int processadas, int total)
        {
            Dispatcher.UIThread.Post(() =>
            {
                BarraProgressoImportacao.Value = total == 0 ? 0 : processadas * 100.0 / total;
                LblProgressoImportacao.Text = Idioma.Formatar("Import.Progress", processadas, total);
            });
        }

        private void EsconderProgressoImportacao() => PainelProgressoImportacao.IsVisible = false;

        private async Task<(int adicionadas, int invalidas, int duplicadas)> ImportarComProgressoAsync(List<SenhaExportada> itens)
        {
            Scrim.Mostrar(this);
            MostrarProgressoImportacao();
            try
            {
                return await AplicarImportacaoAsync(itens, AtualizarProgressoImportacao);
            }
            finally
            {
                EsconderProgressoImportacao();
                Scrim.Ocultar(this);
            }
        }

        private void RestaurarHistorico(Senha destino, List<HistoricoSenhaExportada>? historico)
        {
            if (historico == null || historico.Count == 0 || _criptografia == null)
                return;

            destino.Historico = historico
                .Where(h => !string.IsNullOrEmpty(h.Senha))
                .Select(h => new HistoricoSenha
                {
                    SenhaHash = _criptografia.Criptografar(h.Senha),
                    DataAlteracao = h.DataAlteracao
                })
                .ToList();
        }

        private async Task RestaurarAnexosAsync(Senha destino, List<AnexoExportado>? anexos)
        {
            if (anexos == null || anexos.Count == 0 || _servicoAnexos == null)
                return;

            foreach (var item in anexos)
            {
                try
                {
                    var bytes = Convert.FromBase64String(item.ConteudoBase64);
                    await _servicoAnexos.AdicionarAsync(destino, item.NomeArquivo, bytes);
                }
                catch
                {
                }
            }
        }

        private async void AlterarSenhaMestra_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaAlterarSenhaMestra();
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            try
            {
                var servico = new ServicoMudancaSenhaMestra();
                await servico.AlterarAsync(dlg.SenhaAtual, dlg.NovaSenha);
            }
            catch (ErroLocalizavel ex)
            {
                await CaixaMensagem.MostrarAsync(this, ErrosUi.MensagemAmigavel(ex), Idioma.Texto("Master.ChangeTitle"), TipoMensagem.Aviso);
                return;
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Master.ChangeError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                return;
            }

            var biometriaEstavaHabilitada = _biometria.EstaHabilitado;
            await _biometria.DesabilitarAsync();
            await QrBackup.OferecerSalvarAsync(this, dlg.NovaSenha);

            var mensagem = Idioma.Texto("Master.ChangedRestart");
            if (biometriaEstavaHabilitada)
                mensagem += "\n\n" + Idioma.Texto("Biometric.DisabledAfterMasterChange");

            await CaixaMensagem.MostrarAsync(this,
                mensagem,
                Idioma.Texto("Master.ChangeTitle"));
            Reiniciar();
        }

        private async void RegerarQrCode_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaConfirmarSenhaMestra();
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            await QrBackup.OferecerSalvarAsync(this, dlg.SenhaConfirmada);
        }

        private async void Backup_Click(object? sender, RoutedEventArgs e)
        {
            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Backup.Title"), TipoMensagem.Aviso);
                return;
            }

            var persistencia = new PersistenciaLocal(_criptografia);
            var dlg = new JanelaBackup(persistencia, () => _servicoSenhaLocal.ListarTodosAsync(), _chaveMestra,
                permiteRestaurar: !_conectadoAoBanco);

            if (!await AbrirDialogoAsync<bool>(dlg) || dlg.BackupParaRestaurar is not { } caminho)
                return;

            await RestaurarBackupAsync(persistencia, caminho);
        }

        private async Task RestaurarBackupAsync(IPersistenciaLocal persistencia, string caminhoBackup)
        {
            try
            {
                var senhasRestauradas = await persistencia.CarregarBackupAsync(caminhoBackup);
                await persistencia.SalvarSenhasAsync(senhasRestauradas, _chaveMestra);

                _repositorioLocal = new RepositorioSenha(persistencia, _chaveMestra);
                _servicoSenhaLocal = new ServicoSenha(_repositorioLocal, _criptografia!);
                if (!_conectadoAoBanco)
                    _servicoSenha = _servicoSenhaLocal;

                LimparAuditoria();
                await CarregarSenhasAsync();

                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Backup.RestoreSuccess"), Idioma.Texto("Backup.RestoreTitle"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Backup.Error", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void Biometria_Click(object? sender, RoutedEventArgs e)
        {
            if (!_biometria.SistemaSuportado)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Biometric.UnsupportedPlatform"),
                    Idioma.Texto("Biometric.Title"),
                    TipoMensagem.Aviso);
                return;
            }

            if (_biometria.EstaHabilitado)
            {
                var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                    Idioma.Texto("Biometric.DisableConfirm"),
                    Idioma.Texto("Biometric.Title"));
                if (!confirmar)
                    return;

                await _biometria.DesabilitarAsync();
                AtualizarMenuBiometria();
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Biometric.Disabled"),
                    Idioma.Texto("Biometric.Title"));
                return;
            }

            var resultado = await _biometria.HabilitarAsync(this, _chaveMestra);
            AtualizarMenuBiometria();
            if (resultado.Sucesso)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Biometric.Enabled"),
                    Idioma.Texto("Biometric.Title"));
            }
            else if (!resultado.Cancelado)
            {
                await CaixaMensagem.MostrarAsync(this,
                    resultado.Mensagem ?? Idioma.Texto("Biometric.Unavailable"),
                    Idioma.Texto("Biometric.Title"),
                    TipoMensagem.Aviso);
            }
        }

        private void AtualizarMenuBiometria()
        {
            if (MenuBiometria == null)
                return;

            MenuBiometria.IsVisible = _biometria.SistemaSuportado;
            MenuBiometria.Header = Idioma.Texto(_biometria.EstaHabilitado
                ? "Settings.DisableWindowsHello"
                : "Settings.EnableWindowsHello");
        }

        private void Bloqueio_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not string tag || !int.TryParse(tag, out var minutos))
                return;

            Preferencias.MinutosBloqueio = minutos;
            Preferencias.Salvar();
            _monitor.Ajustar(minutos);
            MarcarBloqueioSelecionado(minutos);
            AtualizarEstadoConexao(_descricaoConexaoAtual, _falhaReconexaoAtual);
        }

        private void MarcarBloqueioSelecionado(int minutos)
        {
            if (MenuBloqueio == null)
                return;

            foreach (var item in MenuBloqueio.Items.OfType<MenuItem>())
                item.IsChecked = item.Tag is string tag && int.TryParse(tag, out var m) && m == minutos;
        }

        private void LimpezaClipboard_Alterada(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not string tag || !int.TryParse(tag, out var segundos))
                return;

            Preferencias.SegundosLimpezaClipboard = segundos;
            Preferencias.Salvar();
            MarcarLimpezaClipboardSelecionada(segundos);
        }

        private void MarcarLimpezaClipboardSelecionada(int segundos)
        {
            if (MenuLimpezaClipboard == null)
                return;

            foreach (var item in MenuLimpezaClipboard.Items.OfType<MenuItem>())
                item.IsChecked = item.Tag is string tag && int.TryParse(tag, out var s) && s == segundos;
        }

        private async void ConectarBanco_Click(object? sender, RoutedEventArgs e)
        {
            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Db.SelectTitle"), TipoMensagem.Aviso);
                return;
            }

            var seletor = new JanelaSelecionarBanco();
            if (!await AbrirDialogoAsync<bool>(seletor) || seletor.Selecionado is not { } tipo)
                return;

            var dlg = new JanelaConexaoBanco(tipo);
            if (!await AbrirDialogoAsync<bool>(dlg) || dlg.Conexao is not { } cfg)
                return;

            await ConectarAsync(cfg, persistir: true, silencioso: false);
        }

        private async Task ConectarAsync(ConexaoBanco cfg, bool persistir, bool silencioso)
        {
            try
            {
                var repoBanco = new RepositorioSenhaBanco(cfg);
                var espelho = _repositorioLocal != null
                    ? new RepositorioSenhaEspelhado(_repositorioLocal, repoBanco,
                        reconciliacaoJaRealizada: Preferencias.UltimoBanco?.ReconciliacaoInicialConcluida ?? false)
                    : null;
                IRepositorioSenha repoAtivo = (IRepositorioSenha?)espelho ?? repoBanco;
                var servico = new ServicoSenha(repoAtivo, _criptografia!);

                await servico.ListarTodosAsync();

                _servicoSenha = servico;
                _conectadoAoBanco = true;

                if (persistir)
                {
                    Preferencias.UltimoBanco = new PerfilBanco
                    {
                        Tipo = cfg.Tipo,
                        Host = cfg.Host,
                        Porta = cfg.Porta,
                        Banco = cfg.Banco,
                        Usuario = cfg.Usuario,
                        SenhaCifrada = cfg.Tipo == TipoBanco.SQLite || string.IsNullOrEmpty(cfg.SenhaServidor)
                            ? null
                            : _criptografia!.Criptografar(cfg.SenhaServidor),
                        Conectado = true,
                        ReconciliacaoInicialConcluida = espelho?.ReconciliacaoRealizadaNestaSessao == true
                    };
                    Preferencias.Salvar();
                }
                else if (espelho?.ReconciliacaoRealizadaNestaSessao == true && Preferencias.UltimoBanco != null)
                {
                    Preferencias.UltimoBanco.ReconciliacaoInicialConcluida = true;
                    Preferencias.Salvar();
                }

                AtualizarEstadoConexao(cfg.Descricao);
                await CarregarSenhasAsync();

                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Formatar("Db.ConnectedMessage", cfg.Descricao),
                        Idioma.Texto("Db.Database"));
            }
            catch (Exception ex)
            {
                _servicoSenha = _servicoSenhaLocal;
                _conectadoAoBanco = false;
                AtualizarEstadoConexao(null, falhaReconexao: silencioso);

                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Formatar("Db.ConnectError", ErrosUi.MensagemAmigavel(ex)),
                        Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private ConexaoBanco? MontarConexaoDoPerfil(PerfilBanco perfil)
        {
            var cfg = new ConexaoBanco
            {
                Tipo = perfil.Tipo,
                Host = perfil.Host,
                Porta = perfil.Porta,
                Banco = perfil.Banco,
                Usuario = perfil.Usuario
            };

            if (!string.IsNullOrEmpty(perfil.SenhaCifrada))
            {
                try { cfg.SenhaServidor = _criptografia!.Descriptografar(perfil.SenhaCifrada); }
                catch { return null; }
            }

            return cfg;
        }

        private async void DesconectarBanco_Click(object? sender, RoutedEventArgs e)
        {
            _servicoSenha = _servicoSenhaLocal;
            _conectadoAoBanco = false;

            if (Preferencias.UltimoBanco != null)
            {
                Preferencias.UltimoBanco.Conectado = false;
                Preferencias.UltimoBanco.SenhaCifrada = null;
                Preferencias.Salvar();
            }

            AtualizarEstadoConexao(null);
            await CarregarSenhasAsync();
        }

        private void AtualizarEstadoConexao(string? descricao, bool falhaReconexao = false)
        {
            _descricaoConexaoAtual = descricao;
            _falhaReconexaoAtual = falhaReconexao;

            string conexao;
            if (_conectadoAoBanco && descricao != null)
            {
                conexao = Idioma.Formatar("Vault.Connection.Connected", descricao);
                PontoConexao.Fill = Tema.Pincel(Tema.StatusConnected);
                MenuDesconectarBanco.IsVisible = true;
            }
            else if (falhaReconexao)
            {
                conexao = Idioma.Texto("Vault.Connection.DatabaseUnavailable");
                PontoConexao.Fill = Tema.Pincel(Tema.StatusWarning);
                MenuDesconectarBanco.IsVisible = true;
            }
            else
            {
                conexao = Idioma.Texto("Vault.Connection.Local");
                PontoConexao.Fill = Tema.Pincel(Tema.StatusLocal);
                MenuDesconectarBanco.IsVisible = false;
            }

            PontoConexao.IsVisible = false;
            LblConexao.Text = TextoBloqueioAutomatico();
            ToolTip.SetTip(LblConexao, conexao);

            AutomationProperties.SetName(LblConexao,
                $"{LblConexao.Text}. {Idioma.Texto("A11y.ConnectionStatus")}: {conexao}");
        }

        private void Reiniciar()
        {
            var executavel = Environment.ProcessPath;
            if (executavel != null)
            {
                try { Process.Start(executavel); } catch { }
            }
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }

        private sealed class FiltroOrganizacao
        {
            private FiltroOrganizacao(string rotulo, Categoria? categoria = null, string? etiqueta = null)
            {
                Rotulo = rotulo;
                Categoria = categoria;
                Etiqueta = etiqueta;
            }

            public string Rotulo { get; }
            public Categoria? Categoria { get; }
            public string? Etiqueta { get; }

            public static FiltroOrganizacao Todas() => new(Idioma.Texto("Vault.Filter.All"));

            public static FiltroOrganizacao ParaCategoria(string rotulo, Categoria categoria) =>
                new(rotulo, categoria);

            public static FiltroOrganizacao ParaEtiqueta(string etiqueta) =>
                new(etiqueta, etiqueta: etiqueta);

            public bool MesmaSelecao(FiltroOrganizacao outra) =>
                Categoria == outra.Categoria &&
                string.Equals(Etiqueta, outra.Etiqueta, StringComparison.OrdinalIgnoreCase);

            public override string ToString() => Rotulo;
        }
    }
}
