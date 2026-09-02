using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
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
        private RepositorioSenhaEspelhado? _repositorioEspelhado;
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
        private readonly DispatcherTimer _timerBusca = new() { Interval = TimeSpan.FromMilliseconds(150) };
        // internal só pra teste inspecionar o agendamento sem esperar uma hora de
        // verdade — ver App.Testes (InternalsVisibleTo).
        internal readonly DispatcherTimer _timerBackupAgendado;
        private DispatcherTimer? _timerFeedbackSenhaDetalhes;
        private DispatcherTimer? _timerFeedbackUsuarioDetalhes;
        private bool _sincronizando;
        private bool _conectadoAoBanco;
        // internal só pra teste conseguir semear um ponto de espera controlável (via
        // TaskCompletionSource) antes de chamar ConectarAsync — cria um yield real e
        // determinístico logo no início de ConectarAposAsync (seu primeiro await),
        // que uma corrida via timing puro com E/S local não consegue garantir. Ver
        // App.Testes (InternalsVisibleTo).
        internal Task _tarefaConexaoAtual = Task.CompletedTask;
        // Incrementado a cada nova tentativa de conexão e a cada desconexão manual —
        // deixa ConectarAposAsync saber, depois dos awaits, se ainda é a tentativa
        // mais recente antes de aplicar o resultado. Sem isto, desconectar (ou trocar
        // pra outro banco) enquanto uma conexão anterior ainda está em voo não
        // impedia essa conexão abandonada de terminar depois e reconectar o cofre por
        // cima da escolha mais recente do usuário.
        // internal só pra teste conseguir simular de forma determinística "uma
        // desconexão aconteceu enquanto uma conexão anterior ainda estava em voo" —
        // com SQLite local a E/S real termina rápido demais pra forçar essa corrida
        // de forma confiável só com timing. Ver App.Testes (InternalsVisibleTo).
        internal int _geracaoConexao;
        private string? _descricaoConexaoAtual;
        private bool _falhaReconexaoAtual;

        private List<Senha> _senhasAtuais = new();
        private List<Senha> _senhasFiltradasAtuais = new();
        private readonly HashSet<Guid> _selecionados = new();
        private readonly List<LinhaSenha> _linhasSenha = new();
        private LinhaSenha? _linhaFocada;
        private (Guid Id, string Texto)? _edicaoServicoPendente;
        private readonly Dictionary<Guid, ItemAuditoriaSenha> _itensAuditoria = new();
        private ResultadoAuditoriaCofre? _resultadoAuditoria;
        private readonly Dictionary<Guid, int> _vazamentosPorId = new();
        private readonly Dictionary<Guid, (string Cifra, string Plain, int Forca)> _cachePlain = new();
        private CategoriaRelatorioSeguranca? _filtroSeguranca;

        private bool _somenteFavoritos;
        private bool _somenteRecentes;
        private bool _ordenacaoDescendente;
        private ColunaOrdenacao _colunaOrdenacao = ColunaOrdenacao.Servico;
        private bool _navColapsada;
        private bool _naLixeira;
        private bool _modoPrivacidade;
        private bool _bloqueadoAteReiniciar;
        private string? _versaoDisponivel;
        private string? _notasVersaoDisponivel;
        private bool _atualizando;
        private List<Senha> _itensLixeira = new();
        private Senha? _senhaDetalhe;
        private string _senhaDetalhePlain = "";
        // Baseline imutável pra detectar edição de senha, capturada uma vez em
        // AbrirDetalhes e nunca mais tocada — diferente de _senhaDetalhePlain, que
        // RevelarSenhaDetalhes_Click atualiza a cada vez que a senha é ocultada de
        // novo (pra carregar a edição feita enquanto estava visível). Usar
        // _senhaDetalhePlain como as duas coisas ao mesmo tempo fazia uma edição
        // revelada-editada-ocultada virar a nova "baseline", escondendo a alteração
        // de DetalhesTemAlteracoesNaoSalvas.
        private string _senhaDetalheOriginal = "";
        // Captura DataAtualizacao no momento em que o painel abre, pra Salvar poder
        // detectar se uma sincronização automática silenciosa (que roda em segundo
        // plano e nunca toca no painel de detalhes aberto) alterou este mesmo item
        // por trás do usuário enquanto ele editava — sem isto, Salvar sobrescreveria
        // em silêncio o que acabou de chegar de outro dispositivo.
        private DateTime _senhaDetalheDataAtualizacaoAoAbrir;
        private bool _senhaDetalheVisivel;
        // internal só pra teste simular "uma operação já está em andamento" sem
        // depender de flagrar uma corrida real — ver App.Testes (InternalsVisibleTo).
        internal bool _detalhesOperacaoEmAndamento;
        private (string Servico, string Usuario, string Url, string Notas, string Etiquetas, int Categoria)? _snapshotDetalhes;
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

            PainelLista.FabricaLinha = CriarLinhaSenha;
            PainelLista.FabricaLixeira = CriarLinhaLixeira;

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

            _timerBusca.Tick += (s, e) =>
            {
                _timerBusca.Stop();
                FiltrarSenhas();
            };

            // VerificarBackupAgendadoAsync só rodava uma vez, na abertura da janela —
            // numa sessão longa (o app suporta ficar minimizado na bandeja por dias),
            // "diário"/"semanal" nunca disparava de novo depois disso, mesmo com o
            // agendamento genuinamente vencido havia muito tempo. Reavaliar de hora em
            // hora é barato (AgendaBackup.Devido só decide algo quando já venceu) e
            // fecha essa lacuna sem depender do usuário bloquear/desbloquear o cofre.
            _timerBackupAgendado = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
            _timerBackupAgendado.Tick += async (s, e) => await VerificarBackupAgendadoAsync();
            _timerBackupAgendado.Start();

            Closed += (s, e) =>
            {
                _monitor.Encerrar();
                _timerSincronizacao.Stop();
                _timerBusca.Stop();
                _timerBackupAgendado.Stop();
                _timerFeedbackSenhaDetalhes?.Stop();
                _timerFeedbackUsuarioDetalhes?.Stop();
                Idioma.Alterado -= IdiomaGlobal_Alterado;
                Acessibilidade.Alterado -= Acessibilidade_Alterado;
                FecharDetalhes();
                foreach (var linha in _linhasSenha)
                    linha.EsconderSenhaSeRevelada();
                _cachePlain.Clear();
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
            if (_bloqueadoAteReiniciar)
                return;

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

        private async void Privacidade_Click(object? sender, RoutedEventArgs e)
        {
            var vaiAtivar = !_modoPrivacidade;
            if (vaiAtivar && !await ConfirmarDescarteDetalhesAsync())
                return;

            _modoPrivacidade = vaiAtivar;

            if (_modoPrivacidade)
                FecharDetalhes();

            foreach (var linha in _linhasSenha)
                linha.DefinirModoPrivacidade(_modoPrivacidade);

            if (_naLixeira)
                AtualizarListaLixeira();

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
                AutomationProperties.SetName(BtnMaximizar, Idioma.Texto(maximizada ? "Access.Restore" : "Access.Maximize"));
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

        private async void IdiomaGlobal_Alterado(object? sender, EventArgs e)
        {
            AtualizarBotaoPrivacidade();
            MarcarIdiomaSelecionado();
            AtualizarMenuBiometria();
            AtualizarFiltroOrganizacao();
            AtualizarEstadoConexao(_descricaoConexaoAtual, _falhaReconexaoAtual);
            ConfigurarAcessibilidadeLeitorTela();

            if (_naLixeira)
                await CarregarLixeiraAsync();
            else
                FiltrarSenhas();

            if (!_atualizando)
            {
                LblBtnAtualizarAgora.Text = Idioma.Texto("Update.Now");
                AutomationProperties.SetName(BtnAtualizarAgora, LblBtnAtualizarAgora.Text);
            }
            if (_versaoDisponivel != null)
                LblAtualizacaoDisponivel.Text = Idioma.Formatar("Update.Available", _versaoDisponivel);
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

            var atualizacao = await ServicoAtualizacao.VerificarNovaVersaoAsync();
            if (atualizacao is not { } info ||
                string.Equals(info.Tag, Preferencias.VersaoDispensada, StringComparison.OrdinalIgnoreCase))
                return;

            ExibirAtualizacaoDisponivel(info);
        }

        // internal só pra teste popular o painel de atualização sem depender da
        // chamada de rede de verdade que VerificarAtualizacaoAsync faz pra API do
        // GitHub — ver App.Testes (InternalsVisibleTo).
        internal void ExibirAtualizacaoDisponivel(AtualizacaoDisponivel info)
        {
            _versaoDisponivel = info.Tag;
            _notasVersaoDisponivel = info.NotasVersao;
            LblAtualizacaoDisponivel.Text = Idioma.Formatar("Update.Available", info.Tag);
            AutomationProperties.SetName(LblAtualizacaoDisponivel, Idioma.Formatar("Update.Available", info.Tag));
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
                () => SincronizarAsync(silencioso: false),
                () => _sincronizando,
                janela => AbrirDialogoAsync<bool>(janela),
                AjustarTimerSincronizacao);

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

                var locais = await ConstruirListaExportavelAsync();

                var remotas = await _servicoSincronizacao.LerAsync(caminho);
                var mescladas = ServicoSincronizacao.MesclarListas(locais, remotas);

                // O ciclo automático roda de tempos em tempos e quase sempre não acha
                // nada novo. Sem estas guardas, cada passagem re-cifrava o cofre inteiro,
                // reescrevia o arquivo e reconstruía a lista (pulando a rolagem do
                // usuário pro topo) à toa.
                bool localMudou = !MesmoConteudoSync(locais, mescladas);
                bool remotoMudou = !MesmoConteudoSync(remotas, mescladas);

                if (localMudou)
                {
                    foreach (var item in mescladas)
                        await _servicoSenha.AplicarSincronizadoAsync(item);
                    await _servicoSenha.PersistirAsync();
                }

                if (remotoMudou)
                {
                    var salt = Convert.FromBase64String(perfil.Salt);
                    await _servicoSincronizacao.EscreverAsync(caminho, salt, perfil.Kdf, perfil.Iteracoes,
                        perfil.MemoriaKb, perfil.Paralelismo, mescladas);
                }

                perfil.UltimaSincronizacao = DateTime.UtcNow;
                Preferencias.Salvar();

                if (localMudou)
                    await CarregarSenhasAsync(silencioso);
                return true;
            }
            catch (Exception ex)
            {
                // Sem isto, uma pasta de sincronização com dado que quebra o merge
                // (ex.: sincronizacao.dat corrompido por outra versão do app) falha do
                // mesmo jeito silenciosamente em toda tentativa futura, sem nenhum
                // rastro pra investigar — silencioso=true (sync automática) já não
                // mostra diálogo nenhum ao usuário.
                Diagnostico.Registrar(ex, "Sincronizar");

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

        private static readonly JsonSerializerOptions _opcoesAssinaturaSync = new();

        private static bool MesmoConteudoSync(List<SenhaExportada> a, List<SenhaExportada> b)
        {
            if (a.Count != b.Count)
                return false;

            using var ea = a.OrderBy(s => s.Id).GetEnumerator();
            using var eb = b.OrderBy(s => s.Id).GetEnumerator();
            while (ea.MoveNext() && eb.MoveNext())
            {
                if (ea.Current.Id != eb.Current.Id ||
                    JsonSerializer.Serialize(ea.Current, _opcoesAssinaturaSync) !=
                    JsonSerializer.Serialize(eb.Current, _opcoesAssinaturaSync))
                    return false;
            }
            return true;
        }

        private async Task<List<SenhaExportada>> ConstruirListaExportavelAsync()
        {
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
            return locais;
        }

        private async void AtualizarAgora_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_versaoDisponivel) || _atualizando)
                return;

            // Sem isto, "Atualizar agora" já disparava o download e a instalação
            // silenciosa direto — o usuário nunca via qual versão ia entrar nem o que
            // mudava nela antes de o app se fechar sozinho pra aplicar a atualização.
            var confirmou = await CaixaMensagem.ConfirmarComListaAsync(this,
                Idioma.Formatar("Update.ConfirmMessage", _versaoDisponivel),
                Idioma.Formatar("Update.ConfirmTitle", _versaoDisponivel),
                QuebrarNotasDaVersao(_notasVersaoDisponivel),
                TipoMensagem.Info);
            if (!confirmou)
                return;

            _atualizando = true;
            BtnAtualizarAgora.IsEnabled = false;
            LblBtnAtualizarAgora.Text = Idioma.Texto("Update.Downloading");
            AutomationProperties.SetName(BtnAtualizarAgora, LblBtnAtualizarAgora.Text);
            try
            {
                var resultado = await ServicoAtualizacao.AtualizarAgoraAsync(_versaoDisponivel);
                switch (resultado.Tipo)
                {
                    case ResultadoAtualizacaoTipo.Sucesso:
                        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                        return;
                    case ResultadoAtualizacaoTipo.Falha:
                        var mensagemFalha = string.IsNullOrEmpty(resultado.Mensagem)
                            ? Idioma.Texto("Update.Failed")
                            : Idioma.Texto("Update.Failed") + "\n\n" + resultado.Mensagem;
                        await CaixaMensagem.MostrarAsync(this, mensagemFalha, Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                        AbrirPaginaReleases();
                        break;
                    case ResultadoAtualizacaoTipo.NaoSuportado:
                        AbrirPaginaReleases();
                        break;
                }
            }
            finally
            {
                _atualizando = false;
                BtnAtualizarAgora.IsEnabled = true;
                LblBtnAtualizarAgora.Text = Idioma.Texto("Update.Now");
                AutomationProperties.SetName(BtnAtualizarAgora, LblBtnAtualizarAgora.Text);
            }
        }

        private static void AbrirPaginaReleases()
        {
            try { Process.Start(new ProcessStartInfo(ServicoAtualizacao.UrlPaginaReleases) { UseShellExecute = true }); }
            catch { }
        }

        // As notas vêm em markdown puro da API do GitHub — sem um renderizador de
        // markdown no app, mostra linha a linha (títulos com # e itens com - ainda
        // saem legíveis como texto simples) em vez de tentar interpretar a formatação.
        private static List<string> QuebrarNotasDaVersao(string? notas)
        {
            if (string.IsNullOrWhiteSpace(notas))
                return new List<string> { Idioma.Texto("Update.NoReleaseNotes") };

            return notas.Replace("\r\n", "\n").Split('\n')
                .Select(linha => linha.Trim())
                .Where(linha => linha.Length > 0)
                .ToList();
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
            _notasVersaoDisponivel = null;
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
            _monitor.Vincular(dialogo);
            try
            {
                return await dialogo.ShowDialog<T>(this);
            }
            finally
            {
                _monitor.Desvincular(dialogo);
                Scrim.Ocultar(this);
            }
        }

        private async void Gerador_SolicitouSalvar(object? sender, string senha)
        {
            if (_naLixeira) return;

            var dlg = new JanelaCriarSenha(_servicoSenha, senha);
            if (await AbrirDialogoAsync<bool>(dlg))
            {
                FecharGerador();
                await CarregarSenhasAsync();
            }
        }

        private async void NovaSenha_Click(object? sender, RoutedEventArgs e)
        {
            if (_naLixeira) return;

            var dlg = new JanelaCriarSenha(_servicoSenha);
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            if (dlg.SenhaCriada is { } nova && !_senhasAtuais.Any(s => s.Id == nova.Id))
            {
                _senhasAtuais.Add(nova);
                AtualizarFiltroOrganizacao();
                FiltrarSenhas();
            }
            else
            {
                await CarregarSenhasAsync();
            }
        }

        // internal só pra teste poder forçar um refresh de _senhasAtuais sem precisar
        // passar por um fluxo de UI completo — ver App.Testes (InternalsVisibleTo).
        internal async Task CarregarSenhasAsync(bool silencioso = false)
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
                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Formatar("Message.LoadError", ErrosUi.MensagemAmigavel(ex)),
                        Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void AtualizarLista(List<Senha> lista)
        {
            // Uma edição inline de nome de serviço ainda não confirmada sobrevive ao
            // rebuild da lista e à virtualização: LinhaSenha reporta o rascunho a cada
            // tecla (RascunhoServicoAlterado -> _edicaoServicoPendente) e CriarLinhaSenha
            // reabre a edição quando a linha volta a ser realizada. Sem isto, favoritar/
            // fixar outra linha, o sync em segundo plano, ou só rolar a lista descartava
            // em silêncio o que o usuário tinha acabado de digitar.
            _linhasSenha.Clear();
            _linhaFocada = null;

            // IntersectWith em vez de Clear: uma busca/filtro roda a cada tecla digitada
            // (Busca_Alterada -> FiltrarSenhas -> aqui), então um Clear incondicional
            // derrubava a seleção em lote inteira no meio do trabalho — bastava o
            // usuário digitar um caractere na busca enquanto tinha itens marcados pra
            // favoritar/etiquetar/mover pra lixeira. Preserva só o que ainda está na
            // lista filtrada; o que saiu de vista sai da seleção também.
            _selecionados.IntersectWith(lista.Select(s => s.Id));
            AtualizarPainelAcoesLote();

            LblVazio.IsVisible = lista.Count == 0;
            TxtVazioMensagem.Text = Idioma.Texto("Vault.Empty");
            BtnVazioNovaSenha.IsVisible = true;
            var estadoLista = lista.Count == 0
                ? Idioma.Texto("A11y.EmptyList")
                : Idioma.Plural(lista.Count, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural");
            AutomationProperties.SetName(PainelLista, $"{Idioma.Texto("A11y.ResultsList")}: {estadoLista}");
            AutomationProperties.SetItemStatus(PainelLista, estadoLista);
            AutomationProperties.SetName(LblVazio, Idioma.Texto("Vault.Empty"));

            PainelLista.ModoLixeira = false;
            PainelLista.ItemsSource = lista;
        }

        private LinhaSenha CriarLinhaSenha(Senha senha)
        {
            var linha = new LinhaSenha(senha, ObterSenhaPlain, ObterTotpPlain, FavoritarToggle, FixarToggle, EditarSenha,
                ExcluirSenhaAsync, RenomearServicoAsync, RegistrarCopiaLinhaAsync);
            linha.SolicitouDetalhes += Linha_SolicitouDetalhes;
            linha.SelecaoAlterada += Linha_SelecaoAlterada;
            linha.GotFocus += (s, e) => _linhaFocada = linha;

            linha.RascunhoServicoAlterado += (s, texto) => _edicaoServicoPendente = (linha.Senha.Id, texto);
            linha.EdicaoServicoFinalizada += (s, e) =>
            {
                if (_edicaoServicoPendente is { } p && p.Id == linha.Senha.Id)
                    _edicaoServicoPendente = null;
            };
            linha.EstadoExternoNecessario += (s, e) => AplicarEstadoLinha(linha);
            linha.AttachedToVisualTree += (s, e) =>
            {
                if (!_linhasSenha.Contains(linha))
                    _linhasSenha.Add(linha);
                if (_edicaoServicoPendente is { } pendente && pendente.Id == linha.Senha.Id && !linha.EmEdicaoDeServico)
                    linha.IniciarEdicaoServico(pendente.Texto);
            };
            linha.DetachedFromVisualTree += (s, e) =>
            {
                _linhasSenha.Remove(linha);
                if (ReferenceEquals(_linhaFocada, linha))
                    _linhaFocada = null;
            };

            return linha;
        }

        // Empurra pra linha (recém-criada ou reciclada) o estado que a janela guarda
        // por Id: larguras de coluna, modo privacidade, seleção em lote, nível de
        // força, achados de auditoria, contagem de vazamentos e um rascunho de edição
        // pendente. LinhaSenha.Vincular chama isto via EstadoExternoNecessario a cada
        // reciclagem.
        private void AplicarEstadoLinha(LinhaSenha linha)
        {
            var senha = linha.Senha;
            linha.DefinirLargurasColunas(_larguraServico, _larguraUsuario, _larguraCategoria, _larguraData, _larguraAcoes);
            linha.DefinirModoPrivacidade(_modoPrivacidade);
            linha.DefinirSelecionada(_selecionados.Contains(senha.Id));

            var forca = NivelForcaDe(senha);
            linha.NivelForca = forca >= 0 ? forca : -1;
            linha.DefinirAuditoria(_itensAuditoria.TryGetValue(senha.Id, out var itemAuditoria) ? itemAuditoria : null);
            linha.Vazamentos = _vazamentosPorId.TryGetValue(senha.Id, out var vazamentos) ? vazamentos : -1;

            if (_edicaoServicoPendente is { } pendente && pendente.Id == senha.Id && !linha.EmEdicaoDeServico)
                linha.IniciarEdicaoServico(pendente.Texto);
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
            _linhasSenha.Clear();
            _linhaFocada = null;

            var lista = _itensLixeira
                .OrderByDescending(s => s.DataExclusao)
                .ToList();

            LblContadorHeader.Text = Idioma.Plural(lista.Count, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural");

            LblVazio.IsVisible = lista.Count == 0;
            TxtVazioMensagem.Text = Idioma.Texto("Trash.Empty");
            BtnVazioNovaSenha.IsVisible = false;
            AutomationProperties.SetName(LblVazio, Idioma.Texto("Trash.Empty"));

            PainelLista.ModoLixeira = true;
            PainelLista.ItemsSource = lista;
        }

        private Control CriarLinhaLixeira(Senha senha)
        {
            // Mesma máscara que LinhaSenha aplica na lista principal — sem isto, entrar
            // na lixeira com o modo privacidade ativo mostrava serviço e usuário reais
            // de todo item excluído, driblando o próprio modo que o usuário acabou de
            // ligar.
            var nomeExibido = _modoPrivacidade ? LinhaSenha.MascaraPrivacidade : senha.NomeServico;
            var usuarioExibido = _modoPrivacidade ? LinhaSenha.MascaraPrivacidade : senha.Usuario;

            var avatar = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            var avatarTexto = new TextBlock
            {
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            avatar.Child = avatarTexto;
            if (_modoPrivacidade)
            {
                avatar.Background = Tema.Pincel(Tema.TrailInactive);
                avatarTexto.Text = "•";
                avatarTexto.Foreground = Tema.Pincel(Tema.TextSecondary);
            }
            else
            {
                var icone = IconesServico.Obter(senha.NomeServico, senha.Url);
                avatar.Background = Tema.Pincel(icone.Fundo);
                avatarTexto.Text = icone.Texto;
                avatarTexto.Foreground = Tema.Pincel(icone.Frente);
            }

            var lblServico = new TextBlock
            {
                Text = nomeExibido,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Tema.Pincel(Tema.TextPrimary)
            };
            var lblUsuario = new TextBlock
            {
                Text = usuarioExibido,
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
            AutomationProperties.SetName(btnRestaurar, Idioma.Formatar("Trash.Restore") + " " + nomeExibido);
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
            AutomationProperties.SetName(btnExcluir, Idioma.Texto("Trash.DeleteForever") + " " + nomeExibido);
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
                var nomeExibido = _modoPrivacidade ? LinhaSenha.MascaraPrivacidade : senha.NomeServico;
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Restored", nomeExibido));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Trash.RestoreError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task ExcluirDefinitivamenteAsync(Senha senha)
        {
            var nomeExibido = _modoPrivacidade ? LinhaSenha.MascaraPrivacidade : senha.NomeServico;
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Trash.DeleteForeverConfirm", nomeExibido),
                Idioma.Texto("Trash.DeleteForever"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                await _servicoSenha.RemoverDefinitivamenteAsync(senha.Id);
                await _servicoSenha.PersistirAsync();
                _cachePlain.Remove(senha.Id);
                _servicoAnexos?.RemoverTodos(senha);
                await PublicarTumbasNaPastaDeSincronizacaoAsync(new[] { senha.Id });
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
                var itensParaLimparAnexos = _itensLixeira.ToList();

                await _servicoSenha.EsvaziarLixeiraAsync();
                await _servicoSenha.PersistirAsync();

                foreach (var item in itensParaLimparAnexos)
                    _cachePlain.Remove(item.Id);

                // Sobrecarga em lote: limpa UltimosAvisos uma vez só e acumula os
                // avisos de todos os itens, em vez de um loop de chamadas individuais
                // (que perderia o aviso de cada item anterior a cada nova chamada).
                _servicoAnexos?.RemoverTodos(itensParaLimparAnexos);

                await PublicarTumbasNaPastaDeSincronizacaoAsync(itensParaLimparAnexos.Select(s => s.Id));
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Trash.EmptyError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        // Diferente do caminho de banco (RepositorioSenhaBanco.EsvaziarLinhaAsync
        // grava uma linha em branco persistente), a pasta de sincronização não tem
        // nenhum armazenamento próprio — só o snapshot que cada sync escreve. Uma vez
        // que RemoverDefinitivamenteAsync roda, o item não deixa nenhum rastro local
        // (RepositorioSenha.RemoverDefinitivamenteAsync é um DELETE de verdade), então
        // se nada avisasse o sincronizacao.dat agora, o próximo ciclo de sync veria o
        // item "só no remoto" (a cópia de antes da exclusão) e o ressuscitaria aqui —
        // exatamente o oposto do que "excluir definitivamente" promete. Isto publica a
        // tumba direto no arquivo compartilhado, sem esperar o próximo ciclo.
        //
        // internal só pra teste chamar direto sem precisar navegar até a lixeira na
        // UI — ver App.Testes (InternalsVisibleTo), mesmo padrão já usado em
        // RepublicarAposTrocaDeSenhaMestraAsync.
        internal async Task PublicarTumbasNaPastaDeSincronizacaoAsync(IEnumerable<Guid> idsExcluidos)
        {
            if (_servicoSincronizacao == null || Preferencias.Sincronizacao is not { } perfil)
                return;

            try
            {
                var caminho = Path.Combine(perfil.Pasta, ServicoSincronizacao.NomeArquivo);
                var remotas = await _servicoSincronizacao.LerAsync(caminho);
                var agora = DateTime.UtcNow;

                foreach (var id in idsExcluidos)
                {
                    remotas.RemoveAll(s => s.Id == id);
                    remotas.Add(new SenhaExportada
                    {
                        Id = id,
                        NomeServico = "",
                        Usuario = "",
                        Senha = "",
                        NaLixeira = true,
                        DataExclusao = agora,
                        DataCriacao = agora,
                        DataAtualizacao = agora
                    });
                }

                var salt = Convert.FromBase64String(perfil.Salt);
                await _servicoSincronizacao.EscreverAsync(caminho, salt, perfil.Kdf, perfil.Iteracoes,
                    perfil.MemoriaKb, perfil.Paralelismo, remotas);
            }
            catch
            {
                // Melhor esforço: se a publicação da tumba falhar, o item já foi
                // excluído localmente de qualquer forma; o pior caso é o próximo sync
                // ainda ressuscitar o item aqui — o mesmo comportamento de antes desta
                // correção, não uma perda pior do que já existia.
            }
        }

        // internal só pra teste chamar direto sem precisar abrir o MenuFlyout de
        // configurações (os itens dele só existem na árvore visual depois de aberto de
        // verdade) — ver App.Testes (InternalsVisibleTo).
        internal async void LimparCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
                return;

            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Vault.ClearTitle"), TipoMensagem.Aviso);
                return;
            }

            // Mesma reautenticação que Excluir Cofre já exige — sem isto, "Limpar
            // cofre" bastava um clique de confirmação, sem senha nenhuma, pra esvaziar
            // o cofre inteiro pra lixeira numa sessão já desbloqueada e sem vigilância.
            var dlgSenha = new JanelaConfirmarSenhaMestra(
                Idioma.Texto("Vault.ClearTitle"),
                Idioma.Texto("Vault.DeleteReauthInstruction"),
                Idioma.Texto("Vault.DeleteReauthButton"));
            if (!await AbrirDialogoAsync<bool>(dlgSenha))
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

                var authPadrao = new AutenticacaoMestra();
                authPadrao.ExcluirSenhaMestra();
                new ControleTentativasLogin(authPadrao.PastaApp).Limpar();
                HistoricoPontuacaoSeguranca.Limpar();

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
            if (_criptografia == null)
                return null;

            if (_cachePlain.TryGetValue(s.Id, out var entrada) && entrada.Cifra == s.SenhaHash)
                return entrada.Plain;

            try
            {
                var plain = _criptografia.Descriptografar(s.SenhaHash);
                _cachePlain[s.Id] = (s.SenhaHash, plain, ForcaSenha.Calcular(plain));
                return plain;
            }
            catch
            {
                _cachePlain.Remove(s.Id);
                return null;
            }
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

        // Decifra cada campo cifrado de s com a chave atual (_criptografia) e recifra
        // com criptografiaNova, preservando estrutura e Ids (diferente dos Obter*Plain
        // acima, que convertem para a forma exportada/plana usada na pasta de
        // sincronização e por isso descartam o Id de CodigoRecuperacao). Retorna null
        // se a própria senha estiver corrompida — melhor pular o item do que abortar a
        // republicação inteira por causa de um registro só.
        private Senha? RecifrarComNovaChave(Senha origem, ServicoCriptografia criptografiaNova)
        {
            if (_criptografia == null)
                return null;

            string senhaPlana;
            try { senhaPlana = _criptografia.Descriptografar(origem.SenhaHash); }
            catch { return null; }

            string? totpPlano = null;
            if (!string.IsNullOrEmpty(origem.TotpSegredo))
            {
                try { totpPlano = _criptografia.Descriptografar(origem.TotpSegredo); }
                catch { }
            }

            var camposExtras = new Dictionary<string, string>();
            foreach (var (chave, valorCifrado) in origem.CamposExtras)
            {
                try { camposExtras[chave] = criptografiaNova.Criptografar(_criptografia.Descriptografar(valorCifrado)); }
                catch { }
            }

            var historico = new List<HistoricoSenha>();
            foreach (var item in origem.Historico)
            {
                try
                {
                    historico.Add(new HistoricoSenha
                    {
                        SenhaHash = criptografiaNova.Criptografar(_criptografia.Descriptografar(item.SenhaHash)),
                        DataAlteracao = item.DataAlteracao
                    });
                }
                catch { }
            }

            var codigosRecuperacao = new List<CodigoRecuperacao>();
            foreach (var item in origem.CodigosRecuperacao)
            {
                try
                {
                    codigosRecuperacao.Add(new CodigoRecuperacao
                    {
                        Id = item.Id,
                        Codigo = criptografiaNova.Criptografar(_criptografia.Descriptografar(item.Codigo)),
                        Usado = item.Usado
                    });
                }
                catch { }
            }

            return new Senha
            {
                Id = origem.Id,
                NomeServico = origem.NomeServico,
                Usuario = origem.Usuario,
                SenhaHash = criptografiaNova.Criptografar(senhaPlana),
                Url = origem.Url,
                Categoria = origem.Categoria,
                Etiquetas = origem.Etiquetas.ToList(),
                Notas = origem.Notas,
                Tipo = origem.Tipo,
                CamposExtras = camposExtras,
                TotpSegredo = totpPlano == null ? null : criptografiaNova.Criptografar(totpPlano),
                Historico = historico,
                CodigosRecuperacao = codigosRecuperacao,
                Favorito = origem.Favorito,
                Fixado = origem.Fixado,
                NaLixeira = origem.NaLixeira,
                DataExclusao = origem.DataExclusao,
                DataCriacao = origem.DataCriacao,
                DataAtualizacao = origem.DataAtualizacao,
                DataUltimaCopiaSenha = origem.DataUltimaCopiaSenha,
                DataUltimaCopiaUsuario = origem.DataUltimaCopiaUsuario,
                DataUltimaCopiaTotp = origem.DataUltimaCopiaTotp
            };
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

        private void Busca_Alterada(object? sender, TextChangedEventArgs e)
        {
            _timerBusca.Stop();
            _timerBusca.Start();
        }

        private void FiltrarSenhas()
        {
            if (PainelLista == null) return;

            _timerBusca.Stop();

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

            // Uma passada de List.Sort em vez de 2-3 OrderBy().ToList() encadeados —
            // fixadas no topo, favoritas logo abaixo (sempre alfabéticas entre si), o
            // resto na ordenação da coluna ativa. Desempate final por Id pra a lista
            // não "pular" quando reordenada com itens de chave igual.
            filtradas.Sort(CompararLinha);

            _senhasFiltradasAtuais = filtradas;
            AtualizarLista(filtradas);
            AtualizarContador();
        }

        private int CompararLinha(Senha a, Senha b)
        {
            int c = b.Fixado.CompareTo(a.Fixado);
            if (c != 0) return c;

            c = b.Favorito.CompareTo(a.Favorito);
            if (c != 0) return c;

            if (a.Favorito)
            {
                c = string.Compare(a.NomeServico, b.NomeServico, StringComparison.CurrentCultureIgnoreCase);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            }

            c = CompararOrdenacaoAtiva(a, b);
            return c != 0 ? c : a.Id.CompareTo(b.Id);
        }

        private int CompararOrdenacaoAtiva(Senha a, Senha b)
        {
            if (_somenteRecentes)
            {
                int c = b.DataAtualizacao.CompareTo(a.DataAtualizacao);
                return c != 0 ? c : b.DataCriacao.CompareTo(a.DataCriacao);
            }

            int r = _colunaOrdenacao switch
            {
                ColunaOrdenacao.Usuario => string.Compare(a.Usuario, b.Usuario, StringComparison.CurrentCultureIgnoreCase),
                ColunaOrdenacao.Categoria => string.Compare(RotuloOrdenacaoCategoria(a), RotuloOrdenacaoCategoria(b), StringComparison.CurrentCultureIgnoreCase),
                ColunaOrdenacao.Forca => NivelForcaDe(a).CompareTo(NivelForcaDe(b)),
                _ => string.Compare(a.NomeServico, b.NomeServico, StringComparison.CurrentCultureIgnoreCase)
            };
            return _ordenacaoDescendente ? -r : r;
        }

        private static string RotuloOrdenacaoCategoria(Senha s) =>
            s.Categoria == Categoria.Other && s.Etiquetas.Count > 0
                ? s.Etiquetas[0]
                : CategoriasUI.Rotulo(s.Categoria);

        private int NivelForcaDe(Senha s)
        {
            var plain = ObterSenhaPlain(s);
            if (string.IsNullOrEmpty(plain))
                return -1;
            return _cachePlain.TryGetValue(s.Id, out var entrada) ? entrada.Forca : ForcaSenha.Calcular(plain);
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

        private string? _culturaCombosFiltro;
        private string? _assinaturaEtiquetasCombo;

        private void AtualizarFiltroOrganizacao()
        {
            if (CmbCategoria == null || CmbEtiqueta == null)
                return;

            // Reatribuir ItemsSource dispara SelectionChanged -> Filtro_Alterado ->
            // FiltrarSenhas (duas vezes por combo). As categorias só mudam de rótulo
            // ao trocar o idioma; as etiquetas, quando o conjunto muda. Sem estas
            // guardas, todo reload rodava FiltrarSenhas várias vezes à toa.
            if (_culturaCombosFiltro != Idioma.Atual.Codigo)
            {
                _culturaCombosFiltro = Idioma.Atual.Codigo;
                AtualizarComboFiltro(CmbCategoria, ConstruirFiltrosCategoria());
            }

            var etiquetas = ConstruirFiltrosEtiqueta(_senhasAtuais);
            var assinatura = string.Join("\n", etiquetas.Select(f => f.Etiqueta));
            if (assinatura != _assinaturaEtiquetasCombo)
            {
                _assinaturaEtiquetasCombo = assinatura;
                AtualizarComboFiltro(CmbEtiqueta, etiquetas);
            }
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
            LblContadorHeader.Text = Idioma.Plural(_senhasFiltradasAtuais.Count, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural");
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
            // Mesma trava de Salvar/Fechar/Excluir (ver FecharDetalhes_Click) — sem
            // ela, dava pra ir pra Lixeira enquanto um Salvar/Excluir do item aberto
            // ainda estava em voo, e quando a operação terminasse ela reabria o painel
            // de detalhes por cima da lixeira que o usuário já estava vendo.
            if (_detalhesOperacaoEmAndamento)
                return;

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
            if (OrdenarPelaColunaDe(sender))
                e.Handled = true;
        }

        private void OrdenarColuna_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Enter or Key.Space))
                return;

            if (OrdenarPelaColunaDe(sender))
                e.Handled = true;
        }

        private bool OrdenarPelaColunaDe(object? sender)
        {
            if (_naLixeira || sender is not Control { Tag: string tag } ||
                !Enum.TryParse<ColunaOrdenacao>(tag, out var coluna))
                return false;

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
            return true;
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

        private async void Linha_SolicitouDetalhes(object? sender, Senha senha)
        {
            // Mesma trava de Salvar/Fechar/Excluir — sem ela, clicar em outra linha
            // enquanto um Salvar/Excluir do item atual ainda está em voo troca o
            // painel pro novo item sem bloqueio nenhum (a lista continua totalmente
            // interativa durante esse voo, só os botões do próprio painel são
            // desabilitados). Quando a operação original terminar, ela reabre/fecha o
            // painel por cima do que o usuário já estava vendo/editando no novo item.
            if (_detalhesOperacaoEmAndamento)
                return;

            if (!await ConfirmarDescarteDetalhesAsync())
                return;
            AbrirDetalhes(senha);
        }

        // internal só pra teste abrir o painel diretamente sem precisar simular o
        // clique na linha da lista — ver App.Testes (InternalsVisibleTo).
        internal void AbrirDetalhes(Senha senha)
        {
            if (_modoPrivacidade)
                return;

            _senhaDetalhe = senha;
            _senhaDetalhePlain = ObterSenhaPlain(senha) ?? "";
            _senhaDetalheOriginal = _senhaDetalhePlain;
            _senhaDetalheDataAtualizacaoAoAbrir = senha.DataAtualizacao;
            _senhaDetalheVisivel = false;

            TxtDetalheServico.Text = senha.NomeServico;
            TxtDetalheUsuario.Text = senha.Usuario;
            TxtDetalheUrl.Text = senha.Url ?? "";
            TxtDetalheNotas.Text = senha.Notas ?? "";

            LblDetalheUsuario.Text = TemplatesCredencial.RotuloUsuario(senha.Tipo);
            LblDetalheSenha.Text = TemplatesCredencial.RotuloSenha(senha.Tipo);
            AutomationProperties.SetName(TxtDetalheUsuario, LblDetalheUsuario.Text);
            AutomationProperties.SetName(TxtDetalheSenha, LblDetalheSenha.Text);

            TxtDetalheEtiquetas.Text = Etiquetas.Formatar(senha.Etiquetas);
            CmbDetalheCategoria.ItemsSource = CategoriasUI.Rotulos;
            CmbDetalheCategoria.SelectedIndex = (int)senha.Categoria;

            AtualizarDetalheVisual();
            AtualizarHistoricoDetalhes();
            AtualizarSenhaDetalhe();
            AtualizarTotpDetalhes();
            ExibirPainel(PainelDetalhes);

            _snapshotDetalhes = (TxtDetalheServico.Text ?? "", TxtDetalheUsuario.Text ?? "",
                TxtDetalheUrl.Text ?? "", TxtDetalheNotas.Text ?? "", TxtDetalheEtiquetas.Text ?? "",
                CmbDetalheCategoria.SelectedIndex);
        }

        // internal só pra teste checar o resultado direto, sem depender do diálogo de
        // confirmação real — ver App.Testes (InternalsVisibleTo).
        internal bool DetalhesTemAlteracoesNaoSalvas()
        {
            if (_snapshotDetalhes is not { } s)
                return false;

            if (TxtDetalheServico.Text != s.Servico || TxtDetalheUsuario.Text != s.Usuario ||
                TxtDetalheUrl.Text != s.Url || TxtDetalheNotas.Text != s.Notas ||
                TxtDetalheEtiquetas.Text != s.Etiquetas || CmbDetalheCategoria.SelectedIndex != s.Categoria)
                return true;

            // Compara contra a baseline imutável, não contra _senhaDetalhePlain (que
            // RevelarSenhaDetalhes_Click atualiza a cada ocultada) — assim uma edição
            // feita enquanto a senha estava visível continua detectável mesmo depois
            // de ocultá-la de novo, sem precisar que o campo esteja visível agora.
            var senhaAtual = _senhaDetalheVisivel ? (TxtDetalheSenha.Text ?? "") : _senhaDetalhePlain;
            return senhaAtual != _senhaDetalheOriginal;
        }

        // internal só pra teste checar a detecção direto, sem depender do diálogo de
        // confirmação real — mesmo padrão de DetalhesTemAlteracoesNaoSalvas.
        internal bool DetalhesTemAlteracaoConcorrente()
        {
            if (_senhaDetalhe == null)
                return false;

            var atual = _senhasAtuais.Concat(_itensLixeira).FirstOrDefault(s => s.Id == _senhaDetalhe.Id);
            return atual != null && atual.DataAtualizacao != _senhaDetalheDataAtualizacaoAoAbrir;
        }

        private async Task<bool> ConfirmarDescarteDetalhesAsync()
        {
            if (!DetalhesTemAlteracoesNaoSalvas())
                return true;

            return await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Entry.Detail.DiscardChangesConfirm"),
                Idioma.Texto("Entry.Detail.DiscardChangesTitle"), TipoMensagem.Aviso);
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
            AvatarDetalhe.Background = Tema.Pincel(icone.Fundo);
            TxtAvatarDetalhe.Text = icone.Texto;
            TxtAvatarDetalhe.Foreground = Tema.Pincel(icone.Frente);
            ToolTip.SetTip(AvatarDetalhe, TxtDetalheServico.Text ?? _senhaDetalhe.NomeServico);

            var (bg, fg) = Acessibilidade.CoresCategoria(_senhaDetalhe.Categoria);
            BadgeDetalheCategoria.Background = Tema.Pincel(bg);
            TxtDetalheCategoria.Foreground = Tema.Pincel(fg);
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

            await CopiarDetalheAsync(codigo, Idioma.Texto("Row.CopyTotp"), limparDepois: true, campoRegistrado: TipoCampoCopiado.Totp);
        }

        private async void EdicaoCompletaDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null)
                return;

            if (!await ConfirmarDescarteDetalhesAsync())
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

        private async void FecharDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            // Salvar/Fechar/Excluir compartilham esta trava — sem ela, dava pra
            // fechar (ou descartar) o painel enquanto um Salvar do mesmo item ainda
            // estava em voo, e quando o Salvar terminasse ele reabria o painel que o
            // usuário acabou de fechar.
            if (_detalhesOperacaoEmAndamento)
                return;

            _detalhesOperacaoEmAndamento = true;
            try
            {
                if (!await ConfirmarDescarteDetalhesAsync())
                    return;
                FecharDetalhes();
            }
            finally
            {
                _detalhesOperacaoEmAndamento = false;
            }
        }

        private void FecharDetalhes()
        {
            PainelDetalhes.IsVisible = false;
            _senhaDetalhe = null;
            _senhaDetalhePlain = "";
            _senhaDetalheOriginal = "";
            _senhaDetalheVisivel = false;
            _snapshotDetalhes = null;
            TxtDetalheSenha.Text = "";
            _timerTotpDetalhe.Parar();
            PainelDetalheTotp.IsVisible = false;
        }

        private async void ExcluirDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null || _detalhesOperacaoEmAndamento)
                return;

            _detalhesOperacaoEmAndamento = true;
            BtnSalvarDetalhes.IsEnabled = false;
            BtnFecharDetalhes.IsEnabled = false;
            BtnExcluirDetalhes.IsEnabled = false;

            try
            {
                var id = _senhaDetalhe.Id;
                await ExcluirSenhaAsync(_senhaDetalhe);
                if (_senhasAtuais.All(s => s.Id != id))
                    FecharDetalhes();
            }
            finally
            {
                _detalhesOperacaoEmAndamento = false;
                BtnSalvarDetalhes.IsEnabled = true;
                BtnFecharDetalhes.IsEnabled = true;
                BtnExcluirDetalhes.IsEnabled = true;
            }
        }

        private async void SalvarDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null || _detalhesOperacaoEmAndamento)
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

            _detalhesOperacaoEmAndamento = true;
            BtnSalvarDetalhes.IsEnabled = false;
            BtnFecharDetalhes.IsEnabled = false;
            BtnExcluirDetalhes.IsEnabled = false;

            try
            {
                // Sincronização automática silenciosa (timer em segundo plano) pode
                // ter alterado este mesmo item por trás do painel enquanto o usuário
                // editava — CarregarSenhasAsync atualiza _senhasAtuais a cada ciclo,
                // mas nunca toca no painel de detalhes já aberto. Sem esta checagem,
                // Salvar sobrescreveria em silêncio o que acabou de chegar de outro
                // dispositivo, sem qualquer aviso de conflito.
                if (DetalhesTemAlteracaoConcorrente())
                {
                    var continuar = await CaixaMensagem.ConfirmarAsync(this,
                        Idioma.Texto("Entry.Detail.ConcurrentChangeConfirm"),
                        Idioma.Texto("Entry.Detail.ConcurrentChangeTitle"), TipoMensagem.Aviso);
                    if (!continuar)
                        return;
                }

                var id = _senhaDetalhe.Id;
                var (categoria, etiquetas) = CategoriasUI.LerCategoriaEEtiquetas(CmbDetalheCategoria.SelectedIndex, TxtDetalheEtiquetas.Text);
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
            finally
            {
                _detalhesOperacaoEmAndamento = false;
                BtnSalvarDetalhes.IsEnabled = true;
                BtnFecharDetalhes.IsEnabled = true;
                BtnExcluirDetalhes.IsEnabled = true;
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
            // limparDepois:true — mesmo motivo do LinhaSenha.CopiarUsuarioAsync (ver
            // AreaTransferenciaFeedback): sem isto, usuário copiado pelo painel de
            // detalhes (muitas vezes o e-mail da pessoa) ficava esquecido no clipboard
            // pra sempre, enquanto senha e TOTP já eram apagados sozinhos.
            await CopiarDetalheAsync(TxtDetalheUsuario.Text, Idioma.Texto("Row.CopyUser"),
                limparDepois: true, campoRegistrado: TipoCampoCopiado.Usuario, botaoFeedback: BtnCopiarUsuarioDetalhes,
                obterTimer: () => _timerFeedbackUsuarioDetalhes, definirTimer: t => _timerFeedbackUsuarioDetalhes = t,
                chaveMensagemLimpando: "Row.UserCopiedClearing");

        private async void CopiarSenhaDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(_senhaDetalheVisivel ? TxtDetalheSenha.Text : _senhaDetalhePlain,
                Idioma.Texto("Row.CopyPassword"), limparDepois: true, campoRegistrado: TipoCampoCopiado.Senha,
                botaoFeedback: BtnCopiarSenhaDetalhes, obterTimer: () => _timerFeedbackSenhaDetalhes,
                definirTimer: t => _timerFeedbackSenhaDetalhes = t, chaveMensagemLimpando: "Row.PasswordCopiedClearing");

        private async void CopiarUrlDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(TxtDetalheUrl.Text, "URL");

        private async Task CopiarDetalheAsync(string? texto, string rotulo, bool limparDepois = false,
            TipoCampoCopiado? campoRegistrado = null, Button? botaoFeedback = null,
            Func<DispatcherTimer?>? obterTimer = null, Action<DispatcherTimer?>? definirTimer = null,
            string? chaveMensagemLimpando = null)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await AreaTransferenciaSegura.CopiarAsync(clipboard, texto); } catch { }
            }

            int segundos = Preferencias.SegundosLimpezaClipboard;
            if (limparDepois && segundos > 0 && clipboard != null)
            {
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.CopiedWillClear", rotulo, segundos));
                if (botaoFeedback != null && obterTimer != null && definirTimer != null && chaveMensagemLimpando != null)
                    AgendarFeedbackLimpezaDetalhe(botaoFeedback, obterTimer, definirTimer, chaveMensagemLimpando, segundos);
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

        private void AgendarFeedbackLimpezaDetalhe(Button botao, Func<DispatcherTimer?> obterTimer,
            Action<DispatcherTimer?> definirTimer, string chaveMensagem, int segundos)
        {
            var mensagem = Idioma.Formatar(chaveMensagem, segundos);
            ToolTip.SetTip(botao, mensagem);
            AutomationProperties.SetName(botao, mensagem);

            obterTimer()?.Stop();

            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Min(segundos, 3)) };
            t.Tick += (s, e) =>
            {
                botao.ClearValue(ToolTip.TipProperty);
                botao.ClearValue(AutomationProperties.NameProperty);
                t.Stop();
            };
            definirTimer(t);
            t.Start();
        }

        private async Task RegistrarCopiaLinhaAsync(Senha senha, TipoCampoCopiado campo)
        {
            await _servicoSenha.RegistrarCopiaAsync(senha.Id, campo);
            await _servicoSenha.PersistirAsync();

            if (_senhaDetalhe != null && _senhaDetalhe.Id == senha.Id)
                AtualizarHistoricoDetalhes();
        }

        private async Task FavoritarToggle(Senha s)
        {
            try
            {
                if (s.Favorito) await _servicoSenha.RemoverDeFavoritoAsync(s.Id);
                else await _servicoSenha.MarcarComoFavoritoAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                AtualizarFiltroOrganizacao();
                FiltrarSenhas();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.FavoriteError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task FixarToggle(Senha s)
        {
            try
            {
                if (s.Fixado) await _servicoSenha.RemoverFixacaoAsync(s.Id);
                else await _servicoSenha.MarcarComoFixadoAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                AtualizarFiltroOrganizacao();
                FiltrarSenhas();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.PinError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        // internal só pra teste chamar direto e confirmar o bloqueio do modo
        // privacidade sem precisar simular o clique no botão da linha (que nem chega a
        // testar o guard, já que RaiseEvent não respeita IsEnabled) — ver App.Testes
        // (InternalsVisibleTo).
        internal async void EditarSenha(Senha s)
        {
            // Mesma proteção de AbrirDetalhes: o botão da linha já fica desabilitado no
            // modo privacidade, mas checar aqui de novo cobre qualquer outro caminho que
            // chegue a este método sem passar pelo botão.
            if (_modoPrivacidade)
                return;

            var dlg = new JanelaEditarSenha(_servicoSenha, s, _criptografia, _servicoAnexos);
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            // A entrada é o mesmo objeto que está em _senhasAtuais e foi mutada no
            // lugar — só re-filtra/reordena. Descarta o cache e o achado de auditoria
            // desta entrada (podem ter mudado); o resto da auditoria continua válido,
            // diferente do CarregarSenhasAsync que zerava tudo.
            _cachePlain.Remove(s.Id);
            _itensAuditoria.Remove(s.Id);
            _vazamentosPorId.Remove(s.Id);
            AtualizarFiltroOrganizacao();
            FiltrarSenhas();
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
            _cachePlain.Remove(id);
            if (_senhaDetalhe?.Id == id)
                FecharDetalhes();
            AtualizarFiltroOrganizacao();
            FiltrarSenhas();
            AtualizarContador();
        }

        private void Linha_SelecaoAlterada(object? sender, Senha senha)
        {
            if (sender is not LinhaSenha linha)
                return;

            if (linha.Selecionada)
                _selecionados.Add(senha.Id);
            else
                _selecionados.Remove(senha.Id);

            AtualizarPainelAcoesLote();
        }

        private void AtualizarPainelAcoesLote()
        {
            PainelAcoesLote.IsVisible = _selecionados.Count > 0;
            LblContagemSelecao.Text = Idioma.Plural(_selecionados.Count,
                "Batch.CountSingular", "Batch.CountPlural");
            PainelAcoesLoteBotoes.IsVisible = true;
            PainelAcoesLoteEtiqueta.IsVisible = false;
        }

        private void LoteCancelarSelecao_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var linha in _linhasSenha)
                linha.DefinirSelecionada(false);
            _selecionados.Clear();
            AtualizarPainelAcoesLote();
        }

        private async void LoteFavoritar_Click(object? sender, RoutedEventArgs e)
        {
            var ids = _selecionados.ToList();
            try
            {
                foreach (var id in ids)
                    await _servicoSenha.MarcarComoFavoritoAsync(id);
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

        private async void LoteMoverParaLixeira_Click(object? sender, RoutedEventArgs e)
        {
            var ids = _selecionados.ToList();

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Batch.TrashConfirm", ids.Count),
                Idioma.Texto("Message.DeleteTitle"), TipoMensagem.Aviso);
            if (!confirmar)
                return;

            try
            {
                foreach (var id in ids)
                    await _servicoSenha.RemoverSenhaAsync(id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.DeleteError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void LoteAdicionarEtiqueta_Click(object? sender, RoutedEventArgs e)
        {
            PainelAcoesLoteBotoes.IsVisible = false;
            PainelAcoesLoteEtiqueta.IsVisible = true;
            TxtLoteEtiqueta.Text = "";
            TxtLoteEtiqueta.Focus();
        }

        private void LoteCancelarEtiqueta_Click(object? sender, RoutedEventArgs e)
        {
            PainelAcoesLoteBotoes.IsVisible = true;
            PainelAcoesLoteEtiqueta.IsVisible = false;
        }

        private async void LoteAplicarEtiqueta_Click(object? sender, RoutedEventArgs e)
        {
            var etiqueta = (TxtLoteEtiqueta.Text ?? "").Trim();
            if (string.IsNullOrEmpty(etiqueta))
                return;

            var itens = _senhasAtuais.Where(s => _selecionados.Contains(s.Id)).ToList();
            try
            {
                foreach (var item in itens)
                {
                    var plain = ObterSenhaPlain(item);
                    if (plain == null)
                        continue;

                    var etiquetas = new List<string>(item.Etiquetas);
                    if (!etiquetas.Contains(etiqueta, StringComparer.OrdinalIgnoreCase))
                        etiquetas.Add(etiqueta);

                    await _servicoSenha.AtualizarSenhaAsync(item.Id, item.NomeServico, item.Usuario, plain,
                        item.Categoria, item.Url, item.Notas, etiquetas, item.Tipo, null);
                }
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.UpdateError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
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
            if (_senhasAtuais.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Message.BreachNoPasswords"),
                    Idioma.Texto("Message.BreachTitle"));
                return;
            }

            var conteudoOriginal = BtnVazamentos.Content;
            BtnVazamentos.IsEnabled = false;
            BtnVazamentos.Content = "…";

            try
            {
                var (verificadas, comprometidas) = await VerificarVazamentosDoVaultAsync();

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

            try
            {
                ExecutarAuditoria();
                var relatorio = ServicoRelatorioSeguranca.Gerar(_senhasAtuais, _resultadoAuditoria!, _vazamentosPorId,
                    CertificadoBancoNaoExigido());
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
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.AuditError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task<RelatorioSegurancaCofre> GerarRelatorioAtualizadoAsync()
        {
            await VerificarVazamentosDoVaultAsync();
            return ServicoRelatorioSeguranca.Gerar(_senhasAtuais, _resultadoAuditoria!, _vazamentosPorId,
                CertificadoBancoNaoExigido());
        }

        private static bool CertificadoBancoNaoExigido() =>
            Preferencias.UltimoBanco is { Conectado: true, ExigirCertificadoValido: false };

        private async Task<(int Verificadas, int Comprometidas)> VerificarVazamentosDoVaultAsync()
        {
            int verificadas = 0;
            int comprometidas = 0;

            foreach (var senha in _senhasAtuais)
            {
                var plain = ObterSenhaPlain(senha);
                if (string.IsNullOrEmpty(plain)) continue;

                int contagem = await _servicoVazamento.VerificarAsync(plain);
                _vazamentosPorId[senha.Id] = contagem;
                if (contagem > 0) comprometidas++;
                verificadas++;
            }

            foreach (var linha in _linhasSenha)
                if (_vazamentosPorId.TryGetValue(linha.Senha.Id, out var contagem))
                    linha.Vazamentos = contagem;

            return (verificadas, comprometidas);
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

                var totalFiltrado = _senhasFiltradasAtuais.Count(s => !s.NaLixeira);
                var dlg = new JanelaSenhaExportacao(modoExportar: true, totalGeral: senhas.Count, totalFiltrado: totalFiltrado);
                if (!await AbrirDialogoAsync<bool>(dlg))
                    return;

                if (dlg.ExportarSomenteFiltrados)
                {
                    var idsFiltrados = new HashSet<Guid>(_senhasFiltradasAtuais.Where(s => !s.NaLixeira).Select(s => s.Id));
                    senhas = senhas.Where(s => idsFiltrados.Contains(s.Id)).ToList();
                }

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

                Scrim.Mostrar(this);
                MostrarProgresso("Export.Progress");
                List<SenhaExportada> itens;
                try
                {
                    itens = new List<SenhaExportada>();
                    for (int i = 0; i < senhas.Count; i++)
                    {
                        var s = senhas[i];
                        var plain = ObterSenhaPlain(s);
                        if (plain != null)
                        {
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

                        AtualizarProgresso("Export.Progress", i + 1, senhas.Count);
                    }

                    await _servicoExportacao.ExportarAsync(arquivo.Path.LocalPath, itens, dlg.SenhaInformada);
                }
                finally
                {
                    EsconderProgresso();
                    Scrim.Ocultar(this);
                }

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

        internal async Task<(int adicionadas, int invalidas, int duplicadas)> AplicarImportacaoAsync(
            List<SenhaExportada> itens, Action<int, int>? aoProgredir = null)
        {
            var existentes = await _servicoSenha.ListarTodosAsync();
            var chaves = new HashSet<(string Nome, string Usuario)>(
                existentes.Select(s => ChaveDuplicata(s.NomeServico, s.Usuario)));

            int adicionadas = 0, invalidas = 0, duplicadas = 0, processadas = 0;
            try
            {
                foreach (var item in itens)
                {
                    if (string.IsNullOrWhiteSpace(item.NomeServico) ||
                        string.IsNullOrWhiteSpace(item.Usuario) ||
                        string.IsNullOrWhiteSpace(item.Senha))
                    {
                        invalidas++;
                    }
                    else if (!chaves.Add(ChaveDuplicata(item.NomeServico, item.Usuario)))
                    {
                        duplicadas++;
                    }
                    else
                    {
                        Senha? nova = null;
                        try
                        {
                            var totp = _totp.SegredoValido(item.TotpSegredo) ? item.TotpSegredo : null;
                            nova = await _servicoSenha.CriarSenhaAsync(
                                item.NomeServico, item.Usuario, item.Senha, item.Categoria, item.Url, item.Notas, totp, item.Etiquetas,
                                item.Tipo, item.CamposExtras);
                        }
                        catch (ErroLocalizavel)
                        {
                            chaves.Remove(ChaveDuplicata(item.NomeServico, item.Usuario));
                            invalidas++;
                        }

                        if (nova != null)
                        {
                            if (item.Favorito)
                                await _servicoSenha.MarcarComoFavoritoAsync(nova.Id);
                            RestaurarHistorico(nova, item.Historico);
                            if (item.CodigosRecuperacao is { Count: > 0 })
                                await _servicoSenha.AdicionarCodigosRecuperacaoAsync(nova.Id,
                                    item.CodigosRecuperacao.Select(c => (c.Codigo, c.Usado)));
                            await RestaurarAnexosAsync(nova, item.Anexos);
                            adicionadas++;
                        }
                    }

                    processadas++;
                    aoProgredir?.Invoke(processadas, itens.Count);
                }
            }
            finally
            {
                // Mesmo que um item no meio do lote lance algo além de ErroLocalizavel
                // (ex.: banco de dados conectado caiu na metade), o que já foi
                // adicionado até aqui não pode ficar só na memória, sem persistir e
                // sem refletir na lista — a exceção ainda propaga normalmente depois.
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }

            return (adicionadas, invalidas, duplicadas);
        }

        // Tupla, não concatenação de string ("nome + " " + usuario"): serviço="Banco X",
        // usuario="Contas Correntes" e serviço="Banco X Contas", usuario="Correntes" geravam
        // a mesma chave concatenada e um dos dois era descartado como "duplicata" na
        // importação sem nunca ter sido de fato duplicado.
        private static (string Nome, string Usuario) ChaveDuplicata(string nomeServico, string usuario) =>
            (nomeServico.ToLowerInvariant(), usuario.ToLowerInvariant());

        private void MostrarProgresso(string chaveMensagem)
        {
            BarraProgresso.Value = 0;
            LblProgresso.Text = Idioma.Formatar(chaveMensagem, 0, 0);
            PainelProgresso.IsVisible = true;
        }

        private void AtualizarProgresso(string chaveMensagem, int processadas, int total)
        {
            Dispatcher.UIThread.Post(() =>
            {
                BarraProgresso.Value = total == 0 ? 0 : processadas * 100.0 / total;
                LblProgresso.Text = Idioma.Formatar(chaveMensagem, processadas, total);
            });
        }

        private void EsconderProgresso() => PainelProgresso.IsVisible = false;

        private async Task<(int adicionadas, int invalidas, int duplicadas)> ImportarComProgressoAsync(List<SenhaExportada> itens)
        {
            Scrim.Mostrar(this);
            MostrarProgresso("Import.Progress");
            try
            {
                return await AplicarImportacaoAsync(itens, (processadas, total) => AtualizarProgresso("Import.Progress", processadas, total));
            }
            finally
            {
                EsconderProgresso();
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

            // A senha do servidor de banco (se houver) está cifrada com a chave atual —
            // precisa ser decifrada antes da troca e recifrada com a chave nova depois,
            // senão a reconexão automática passa a falhar silenciosamente no próximo
            // login (MontarConexaoDoPerfil não consegue mais decifrá-la).
            string? senhaServidorPlano = null;
            try
            {
                if (_criptografia != null && !string.IsNullOrEmpty(Preferencias.UltimoBanco?.SenhaCifrada))
                    senhaServidorPlano = _criptografia.Descriptografar(Preferencias.UltimoBanco.SenhaCifrada);
            }
            catch { }

            // Reconcilia com a pasta de sincronização compartilhada usando a chave
            // ainda antiga, antes dela deixar de bater com a senha nova — evita que a
            // republicação feita mais abaixo (já com a chave nova) sobrescreva o que
            // outro dispositivo tenha colocado lá desde a última sincronização.
            await SincronizarAsync(silencioso: true);

            var servico = new ServicoMudancaSenhaMestra();
            byte[] chaveNova;
            try
            {
                chaveNova = await servico.AlterarAsync(dlg.SenhaAtual, dlg.NovaSenha);
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

            // A troca já terminou com sucesso — auth.dat/vault (e a pasta de sync, mais
            // abaixo) já estão na chave nova, mas _servicoSenha/_servicoSincronizacao
            // continuam vinculados à chave ANTIGA até o restart. Qualquer gravação no
            // cofre nesse intervalo — o ciclo de sync automático, ou uma ação manual do
            // usuário enquanto o QrBackup e o aviso de reinício esperam a decisão dele
            // sem prazo — regrava senhas.json.enc com a chave antiga por cima do que
            // acabou de ser salvo com a nova, deixando o cofre com metades em chaves
            // diferentes e ilegível depois do restart. Para o timer e congela a janela
            // até Reiniciar() encerrar o processo; nada disso precisa voltar depois.
            _timerSincronizacao.Stop();
            DesabilitarInteracaoAteReiniciar();

            if (senhaServidorPlano != null && Preferencias.UltimoBanco != null)
            {
                Preferencias.UltimoBanco.SenhaCifrada = new ServicoCriptografia(chaveNova).Criptografar(senhaServidorPlano);
                Preferencias.Salvar();
            }

            var afetaOutrosDispositivos = await RepublicarAposTrocaDeSenhaMestraAsync(chaveNova, dlg.NovaSenha, senhaServidorPlano);

            var biometriaEstavaHabilitada = _biometria.EstaHabilitado;
            await _biometria.DesabilitarAsync();
            await QrBackup.OferecerSalvarAsync(this, dlg.NovaSenha);

            var mensagem = Idioma.Texto("Master.ChangedRestart");
            if (biometriaEstavaHabilitada)
                mensagem += "\n\n" + Idioma.Texto("Biometric.DisabledAfterMasterChange");
            if (servico.UltimosAvisos.Count > 0)
                mensagem += "\n\n" + Idioma.Texto("Master.ItemsDiscardedWarning");
            if (afetaOutrosDispositivos)
                mensagem += "\n\n" + Idioma.Texto("Master.OtherDevicesWarning");

            await CaixaMensagem.MostrarAsync(this,
                mensagem,
                Idioma.Texto("Master.ChangeTitle"));
            Reiniciar();
        }

        // internal (em vez de private) só para expor um seam de teste direto sem
        // precisar dirigir o diálogo JanelaAlterarSenhaMestra nem os efeitos colaterais
        // de UI (QR code, biometria, reinício) que o resto do fluxo dispara — ver
        // App.Testes (InternalsVisibleTo), mesmo padrão já usado em ConectarAsync.
        //
        // Sem isto, o banco conectado e a pasta de sincronização continuam com o
        // conteúdo (e o hmac, no caso do banco) cifrados com a chave antiga — nem este
        // dispositivo consegue lê-los de volta depois de reiniciar com a senha nova, e
        // "Restaurar de um banco de dados" fica travado no salt/verificador antigos pra
        // sempre (RepositorioSenhaEspelhado só concilia dados, nunca a chave de
        // cifragem usada pra gravá-los). Retorna true se o cofre está conectado a um
        // banco ou pasta compartilhada com outros dispositivos, que precisam trocar a
        // senha mestra também para continuar lendo o que este dispositivo publicar.
        internal async Task<bool> RepublicarAposTrocaDeSenhaMestraAsync(byte[] chaveNova, string novaSenhaPlano, string? senhaServidorPlano)
        {
            var afetaOutrosDispositivos = _conectadoAoBanco || Preferencias.Sincronizacao != null;

            if (_conectadoAoBanco && Preferencias.UltimoBanco is { } perfilBanco && _criptografia != null)
            {
                try
                {
                    var criptografiaNova = new ServicoCriptografia(chaveNova);
                    var cfgNova = new ConexaoBanco
                    {
                        Tipo = perfilBanco.Tipo,
                        Host = perfilBanco.Host,
                        Porta = perfilBanco.Porta,
                        Banco = perfilBanco.Banco,
                        Usuario = perfilBanco.Usuario,
                        SenhaServidor = senhaServidorPlano,
                        ExigirCertificadoValido = perfilBanco.ExigirCertificadoValido,
                        ExigirIntegridade = perfilBanco.ExigirIntegridade
                    };

                    var todasAtuais = (await _servicoSenha.ListarTodosAsync()).Concat(await _servicoSenha.ListarLixeiraAsync());
                    var itensRecifrados = todasAtuais
                        .Select(s => RecifrarComNovaChave(s, criptografiaNova))
                        .Where(s => s != null)
                        .Cast<Senha>();

                    var repoBancoNovo = new RepositorioSenhaBanco(cfgNova, criptografiaNova);
                    await repoBancoNovo.GravarVariasPorChaveAsync(itensRecifrados);

                    if (new AutenticacaoMestra().TentarLerParametros(out var salt, out var verificador, out var kdf, out var custo, out var memoriaKb, out var paralelismo))
                        await new ServicoBancoDados().PublicarAuthAsync(cfgNova, new AuthBanco(salt, verificador, kdf, custo, memoriaKb, paralelismo));
                }
                catch
                {
                    // Melhor esforço: se a republicação falhar, o cofre local já está
                    // trocado e funcional; a próxima reconexão manual ou sincronização
                    // tenta de novo, e o usuário já é avisado sobre os outros
                    // dispositivos por quem chama este método.
                }
            }

            if (Preferencias.Sincronizacao is { } perfilSync)
            {
                try
                {
                    var saltSync = Convert.FromBase64String(perfilSync.Salt);
                    var chaveSyncNova = ServicoSincronizacao.DerivarChave(novaSenhaPlano, saltSync, perfilSync.Kdf,
                        perfilSync.Iteracoes, perfilSync.MemoriaKb, perfilSync.Paralelismo);
                    var servicoSyncNovo = new ServicoSincronizacao(new ServicoCriptografia(chaveSyncNova));

                    var caminho = Path.Combine(perfilSync.Pasta, ServicoSincronizacao.NomeArquivo);
                    await servicoSyncNovo.EscreverAsync(caminho, saltSync, perfilSync.Kdf, perfilSync.Iteracoes,
                        perfilSync.MemoriaKb, perfilSync.Paralelismo, await ConstruirListaExportavelAsync());
                }
                catch
                {
                    // Melhor esforço, mesmo raciocínio do bloco do banco acima.
                }
            }

            return afetaOutrosDispositivos;
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

        // internal só pra teste chamar direto sem precisar dirigir a JanelaBackup
        // inteira (lista de backups, clique de restaurar, confirmação) — ver
        // App.Testes (InternalsVisibleTo).
        internal async Task RestaurarBackupAsync(IPersistenciaLocal persistencia, string caminhoBackup)
        {
            try
            {
                var senhasRestauradas = await persistencia.CarregarBackupAsync(caminhoBackup);

                // Salva o estado atual como um backup antes de sobrescrever — sem isto,
                // restaurar um backup mais antigo descarta tudo que mudou depois sem
                // deixar nenhum jeito de desfazer, mesmo a própria janela de restauração
                // já avisando que as alterações mais recentes serão perdidas. Lido o
                // backup de destino ANTES desta chamada de propósito: BackupAutomaticoAsync
                // pode acabar apagando o backup mais antigo pra respeitar o teto — se for
                // justo o que o usuário escolheu restaurar, o conteúdo dele já está a
                // salvo em memória a essa altura.
                try
                {
                    var senhasAtuais = await _servicoSenhaLocal.ListarTodosAsync();
                    if (senhasAtuais.Count > 0)
                        await persistencia.BackupAutomaticoAsync(senhasAtuais, _chaveMestra, Preferencias.MaximoBackups);
                }
                catch
                {
                    // Melhor esforço: falhar na foto de segurança não pode impedir a
                    // restauração que o usuário pediu.
                }

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

        // internal (não private) só pra permitir testar a orquestração de conexão sem
        // precisar dirigir os dois diálogos (JanelaSelecionarBanco/JanelaConexaoBanco)
        // que normalmente ficam na frente dela — ver App.Testes (InternalsVisibleTo).
        internal Task ConectarAsync(ConexaoBanco cfg, bool persistir, bool silencioso)
        {
            var minhaGeracao = ++_geracaoConexao;
            var minhaTarefa = ConectarAposAsync(_tarefaConexaoAtual, cfg, persistir, silencioso, minhaGeracao);
            _tarefaConexaoAtual = minhaTarefa;
            return minhaTarefa;
        }

        private async Task ConectarAposAsync(Task tarefaAnterior, ConexaoBanco cfg, bool persistir, bool silencioso, int minhaGeracao)
        {
            // Uma falha da tentativa anterior (incluindo, em tese, o próprio diálogo de
            // erro dela) não pode propagar aqui — senão essa nova tarefa também fica
            // faltada, e como _tarefaConexaoAtual nunca é resetada, toda tentativa futura
            // de conexão ficaria permanentemente travada reencontrando o erro antigo.
            try { await tarefaAnterior; } catch { }

            try
            {
                var repoBanco = new RepositorioSenhaBanco(cfg, _criptografia);
                var espelho = _repositorioLocal != null
                    ? new RepositorioSenhaEspelhado(_repositorioLocal, repoBanco,
                        reconciliacaoJaRealizada: Preferencias.UltimoBanco?.ReconciliacaoInicialConcluida ?? false)
                    : null;
                IRepositorioSenha repoAtivo = (IRepositorioSenha?)espelho ?? repoBanco;
                var servico = new ServicoSenha(repoAtivo, _criptografia!);

                await servico.ListarTodosAsync();
                await PublicarAuthNoBancoSeNecessarioAsync(cfg);

                // Enquanto os awaits acima estavam em voo, o usuário pode ter clicado
                // "Desconectar" ou iniciado outra tentativa de conexão — essa é a mais
                // recente e deve prevalecer; aplicar o resultado desta reconectaria o
                // cofre contra a vontade mais atual do usuário.
                if (minhaGeracao != _geracaoConexao)
                    return;

                _servicoSenha = servico;
                _repositorioEspelhado = espelho;
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
                        ReconciliacaoInicialConcluida = espelho?.ReconciliacaoRealizadaNestaSessao == true,
                        ExigirCertificadoValido = cfg.ExigirCertificadoValido,
                        ExigirIntegridade = cfg.ExigirIntegridade
                    };
                    Preferencias.Salvar();
                }
                else if (espelho?.ReconciliacaoRealizadaNestaSessao == true && Preferencias.UltimoBanco != null)
                {
                    Preferencias.UltimoBanco.ReconciliacaoInicialConcluida = true;
                    Preferencias.Salvar();
                }

                AtualizarEstadoConexao(cfg.Descricao);

                // Sem isto, um conflito de sincronização (em especial integridade
                // violada — possível adulteração do banco compartilhado) só existia na
                // lista em memória de UltimosConflitos: se o usuário não abrisse a tela
                // de conflitos antes de reconectar ou fechar o app, o registro sumia
                // pra sempre sem deixar rastro nenhum pra revisar depois.
                foreach (var conflito in espelho?.UltimosConflitos ?? Array.Empty<ConflitoSincronizacao>())
                    Diagnostico.Registrar(
                        $"{conflito.Tipo} em \"{conflito.NomeServico}\" (id {conflito.SenhaId})",
                        "ConflitoSincronizacao");

                await CarregarSenhasAsync();

                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Formatar("Db.ConnectedMessage", cfg.Descricao),
                        Idioma.Texto("Db.Database"));
            }
            catch (Exception ex)
            {
                // Mesmo raciocínio do "return" acima: se o usuário já desconectou ou
                // começou outra tentativa, nem o estado nem um diálogo de erro fazem
                // sentido pra uma conexão que ele já abandonou.
                if (minhaGeracao != _geracaoConexao)
                    return;

                _servicoSenha = _servicoSenhaLocal;
                _repositorioEspelhado = null;
                _conectadoAoBanco = false;
                AtualizarEstadoConexao(null, falhaReconexao: silencioso);

                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Formatar("Db.ConnectError", ErrosUi.MensagemAmigavel(ex)),
                        Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private static async Task PublicarAuthNoBancoSeNecessarioAsync(ConexaoBanco cfg)
        {
            try
            {
                var bd = new ServicoBancoDados();
                if (await bd.TabelaAuthExisteAsync(cfg))
                    return;

                if (!new AutenticacaoMestra().TentarLerParametros(out var salt, out var verificador, out var kdf, out var custo, out var memoriaKb, out var paralelismo))
                    return;

                await bd.CriarTabelaAuthAsync(cfg);
                await bd.PublicarAuthAsync(cfg, new AuthBanco(salt, verificador, kdf, custo, memoriaKb, paralelismo));
            }
            catch
            {
                // Melhor esforço: se a publicação falhar, a conexão/espelhamento normal
                // continua funcionando do mesmo jeito de sempre, só a restauração a
                // partir deste banco fica indisponível até uma tentativa futura funcionar.
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
                Usuario = perfil.Usuario,
                ExigirCertificadoValido = perfil.ExigirCertificadoValido,
                ExigirIntegridade = perfil.ExigirIntegridade
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
            // Invalida qualquer tentativa de conexão ainda em voo (ver
            // ConectarAposAsync) — sem isto, uma conexão iniciada antes de
            // "Desconectar" podia terminar depois e reconectar o cofre por cima
            // desta escolha, que é a mais recente.
            _geracaoConexao++;

            _servicoSenha = _servicoSenhaLocal;
            _repositorioEspelhado = null;
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

            LblConexao.Text = TextoBloqueioAutomatico();
            ToolTip.SetTip(LblConexao, conexao);

            AutomationProperties.SetName(LblConexao,
                $"{LblConexao.Text}. {Idioma.Texto("A11y.ConnectionStatus")}: {conexao}");

            BtnConflitosSincronizacao.IsVisible = (_repositorioEspelhado?.UltimosConflitos.Count ?? 0) > 0;
        }

        private async void ConflitosSincronizacao_Click(object? sender, RoutedEventArgs e)
        {
            if (_repositorioEspelhado == null)
                return;

            var dlg = new JanelaConflitosSincronizacao(_repositorioEspelhado.UltimosConflitos);
            await AbrirDialogoAsync<bool>(dlg);
        }

        // Entre a troca de senha mestra gravada em disco e o restart, a janela ainda
        // opera na chave antiga (ver AlterarSenhaMestra_Click). IsEnabled bloqueia a UI
        // de ponteiro — menus, botões, linhas, painel de detalhes; a flag bloqueia os
        // atalhos de teclado, que ainda percorrem a árvore de eventos com a janela
        // desabilitada. Os diálogos que ainda faltam no fluxo (QR de backup, aviso de
        // reinício) são janelas próprias e seguem utilizáveis. internal só para teste.
        internal void DesabilitarInteracaoAteReiniciar()
        {
            _bloqueadoAteReiniciar = true;
            IsEnabled = false;
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
