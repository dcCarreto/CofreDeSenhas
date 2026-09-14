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
using Avalonia.Layout;
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
        private bool _janelaFechada;
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
            Acessibilidade.RegistrarToast(this, ToastAcessibilidade, LblToastAcessibilidade);
            ConfigurarAcessibilidadeLeitorTela();

            RestaurarOrdenacao();
            AplicarLayoutDetalhe();

            PainelLista.FabricaLinha = CriarLinhaSenha;
            PainelLista.FabricaLixeira = CriarLinhaLixeira;

            CmbCategoria.ItemsSource = ConstruirFiltrosCategoria();
            CmbCategoria.SelectedIndex = 0;
            CmbEtiqueta.ItemsSource = ConstruirFiltrosEtiqueta(Array.Empty<Senha>());
            CmbEtiqueta.SelectedIndex = 0;

            Gerador.SolicitouSalvar += Gerador_SolicitouSalvar;
            Gerador.ShowHeader = false;

            AtualizarBotaoPrivacidade();
            BtnDigitacaoAutomatica.IsVisible = DigitacaoAutomatica.Suportado;
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            AtualizarContador();
            AtualizarEstadoConexao(null);

            _monitor = new MonitorInatividade(this, () => _aoBloquear?.Invoke());
            _monitor.Ajustar(Preferencias.MinutosBloqueio);
            _monitor.AvisoHabilitado = () => Acessibilidade.AvisarAntesBloqueio;
            _monitor.AoAvisar = AvisarAntesDoBloqueioAsync;
            Idioma.Alterado += IdiomaGlobal_Alterado;
            Acessibilidade.Alterado += Acessibilidade_Alterado;
            AddHandler(KeyDownEvent, Atalho_KeyDown, RoutingStrategies.Tunnel);
            this.AtalhoAjuda("introducao");
            _timerSincronizacao = new DispatcherTimer();
            _timerSincronizacao.Tick += async (s, e) => await SincronizarAsync(silencioso: true);
            AjustarTimerSincronizacao();

            _timerBusca.Tick += (s, e) =>
            {
                _timerBusca.Stop();
                FiltrarSenhas(reordenar: false);
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
                DigitacaoAutomatica.PararDeObservar();
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
                _janelaFechada = true;
                _cachePlain.Clear();
                CryptographicOperations.ZeroMemory(_chaveMestra);
                _criptografia?.ZerarChave();
                _servicoSincronizacao?.ZerarChave();
            };

            Opened += async (s, e) =>
            {
                AjustarLargurasIniciais();
                DigitacaoAutomatica.ComecarAObservar();
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

        private void Ajuda_Click(object? sender, RoutedEventArgs e) => JanelaAjuda.AbrirOuFocar(this);

        private async void Configuracoes_Click(object? sender, RoutedEventArgs e)
        {
            var vazio = new RoutedEventArgs();
            var acoes = new AcoesConfiguracoes
            {
                AlterarSenhaMestra = () => AlterarSenhaMestra_Click(this, vazio),
                RegerarQr = () => RegerarQrCode_Click(this, vazio),
                ChaveRecuperacaoAtivarOuGerar = AtivarOuGerarChaveRecuperacao,
                ChaveRecuperacaoDesativar = DesativarChaveRecuperacao,
                ChaveRecuperacaoAtiva = new ServicoRecuperacao().EstaHabilitada(),
                AlternarWindowsHello = () => Biometria_Click(this, vazio),
                BloquearAgora = () => BloquearAgora_Click(this, vazio),
                Backup = () => Backup_Click(this, vazio),
                Sincronizacao = () => Sincronizacao_Click(this, vazio),
                ImportarCsv = () => ImportarCsv_Click(this, vazio),
                ConectarBanco = () => ConectarBanco_Click(this, vazio),
                DesconectarBanco = () => DesconectarBanco_Click(this, vazio),
                AtalhosTeclado = () => AtalhosTeclado_Click(this, vazio),
                AbrirManual = () => JanelaAjuda.AbrirOuFocar(this),
                LimparCofre = () => LimparCofre_Click(this, vazio),
                ExcluirCofre = () => ExcluirCofre_Click(this, vazio),
                DefinirBloqueioAutomatico = AplicarBloqueioAutomatico,
                DefinirVerificarAtualizacoes = AplicarVerificarAtualizacoes,
                DefinirIconesOnline = AplicarIconesOnline,
                WindowsHelloSuportado = _biometria.SistemaSuportado,
                WindowsHelloAtivo = _biometria.EstaHabilitado,
                BancoConectado = _conectadoAoBanco || _falhaReconexaoAtual
            };

            var dlg = new JanelaConfiguracoes(acoes);
            await AbrirDialogoAsync<bool>(dlg);
            dlg.AcaoPendente?.Invoke();
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
            await AvisarSeCofreForCopiaDeBackupAsync();
            _ = VerificarBackupAgendadoAsync();
        }

        private async Task AvisarSeCofreForCopiaDeBackupAsync()
        {
            try
            {
                if (PersistenciaLocal.CofreEhCopiaDeBackup())
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Vault.RestoredCopyWarning"),
                        Idioma.Texto("Vault.RestoredCopyTitle"),
                        TipoMensagem.Aviso);
            }
            catch
            {
            }
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

        private void Minimizar_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximizar_Click(object? sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();

        private async void IdiomaGlobal_Alterado(object? sender, EventArgs e)
        {
            AtualizarBotaoPrivacidade();
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
                LblAtualizacaoDisponivel.Text = Idioma.Texto("Update.Available");
        }

        internal void AplicarIconesOnline(bool ligado)
        {
            Preferencias.IconesOnline = ligado;
            if (!ligado)
                IconesServico.LimparCache();

            Preferencias.Salvar();
            FiltrarSenhas(reordenar: false);
        }

        private void Acessibilidade_Alterado(object? sender, EventArgs e)
        {
            ConfigurarAcessibilidadeLeitorTela();
            Gerador.AtualizarTema();
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            AplicarLayoutDetalhe();
            if (_largurasIniciaisAplicadas)
                AplicarLargurasColunas();
            AtualizarDetalheVisual();
            AtualizarHistoricoDetalhes();
            FiltrarSenhas(reordenar: false);
        }

        private void ConfigurarAcessibilidadeLeitorTela()
        {
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

        private void AplicarEstadoLinha(LinhaSenha linha)
        {
            var senha = linha.Senha;
            var larguras = LargurasEfetivas();
            linha.DefinirLargurasColunas(larguras.Servico, larguras.Usuario, larguras.Categoria, larguras.Forca, _larguraAcoes);
            linha.DefinirModoPrivacidade(_modoPrivacidade);
            linha.DefinirSelecionada(_selecionados.Contains(senha.Id));

            var forca = NivelForcaDe(senha);
            linha.NivelForca = forca >= 0 ? forca : -1;
            linha.DefinirAuditoria(_itensAuditoria.TryGetValue(senha.Id, out var itemAuditoria) ? itemAuditoria : null);
            linha.Vazamentos = _vazamentosPorId.TryGetValue(senha.Id, out var vazamentos) ? vazamentos : -1;

            if (_edicaoServicoPendente is { } pendente && pendente.Id == senha.Id && !linha.EmEdicaoDeServico)
                linha.IniciarEdicaoServico(pendente.Texto);
        }

        private void Filtro_Alterado(object? sender, SelectionChangedEventArgs e) => FiltrarSenhas(reordenar: false);

        private void Busca_Alterada(object? sender, TextChangedEventArgs e)
        {
            _timerBusca.Stop();
            _timerBusca.Start();
        }

        // reordenar: false quando a chamada não pode mudar a ordem — a busca (roda a
        // cada tecla, depois do debounce) e a troca dos filtros de categoria/etiqueta
        // só escondem itens. Nesses casos, poupa o Sort da lista inteira; a ordem já
        // está boa do último FiltrarSenhas que reordenou.
        private void FiltrarSenhas(bool reordenar = true)
        {
            if (PainelLista == null) return;

            _timerBusca.Stop();

            if (reordenar)
                // CompararLinha desempata por Id: List.Sort é instável e sem isso a lista "pula".
                _senhasAtuais.Sort(CompararLinha);

            var termo = (TxtBusca.Text ?? "").Trim();
            var categoriaFiltro = (CmbCategoria.SelectedItem as FiltroOrganizacao)?.Categoria;
            var etiquetaFiltro = (CmbEtiqueta.SelectedItem as FiltroOrganizacao)?.Etiqueta;

            var filtradas = new List<Senha>(_senhasAtuais.Count);
            foreach (var s in _senhasAtuais)
            {
                if (!string.IsNullOrEmpty(termo) &&
                    !s.NomeServico.Contains(termo, StringComparison.OrdinalIgnoreCase) &&
                    !s.Usuario.Contains(termo, StringComparison.OrdinalIgnoreCase) &&
                    !s.Etiquetas.Any(e => e.Contains(termo, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (categoriaFiltro != null && s.Categoria != categoriaFiltro)
                    continue;
                if (etiquetaFiltro != null &&
                    !s.Etiquetas.Any(e => string.Equals(e, etiquetaFiltro, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (_somenteFavoritos && !s.Favorito)
                    continue;
                if (_filtroSeguranca != null && !SenhaTemProblema(s, _filtroSeguranca.Value))
                    continue;
                filtradas.Add(s);
            }

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
            FiltrarSenhas(reordenar: false);
        }

        private string? _culturaCombosFiltro;
        private string? _assinaturaEtiquetasCombo;

        private void AtualizarFiltroOrganizacao()
        {
            if (CmbCategoria == null || CmbEtiqueta == null)
                return;

            // Reatribuir ItemsSource redispara FiltrarSenhas; só refaz cada combo quando muda de fato.
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
            EspacoFab.Height = _navColapsada ? 56 : 76;
            BtnFabGerador.Width = _navColapsada ? 40 : 60;
            BtnFabGerador.Height = _navColapsada ? 40 : 60;
            BtnFabGerador.CornerRadius = new CornerRadius(_navColapsada ? 20 : 30);
            Canvas.SetLeft(BtnFabGerador, _navColapsada ? 14 : 24);
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
            PersistirOrdenacao();
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private void RestaurarOrdenacao()
        {
            if (Enum.TryParse<ColunaOrdenacao>(Preferencias.OrdenacaoColuna, out var coluna))
                _colunaOrdenacao = coluna;
            _ordenacaoDescendente = Preferencias.OrdenacaoDescendente;
        }

        private void PersistirOrdenacao()
        {
            Preferencias.OrdenacaoColuna = _colunaOrdenacao.ToString();
            Preferencias.OrdenacaoDescendente = _ordenacaoDescendente;
            Preferencias.Salvar();
        }

        private void AplicarLayoutDetalhe()
        {
            bool inferior = Acessibilidade.LayoutDetalhe == LayoutDetalhe.Inferior;

            PainelDetalhes.HorizontalAlignment = inferior ? HorizontalAlignment.Stretch : HorizontalAlignment.Right;
            PainelDetalhes.VerticalAlignment = inferior ? VerticalAlignment.Bottom : VerticalAlignment.Stretch;
            PainelDetalhes.Width = inferior ? double.NaN : 340;
            PainelDetalhes.Height = inferior ? 320 : double.NaN;

            CorpoDetalhe.ColumnDefinitions = new ColumnDefinitions(inferior ? "*,28,*" : "*");
            Grid.SetRow(ColunaDetalheB, inferior ? 0 : 1);
            Grid.SetColumn(ColunaDetalheB, inferior ? 2 : 0);
            ColunaDetalheB.Margin = inferior ? new Thickness(0) : new Thickness(0, 14, 0, 0);
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
            PersistirOrdenacao();

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
