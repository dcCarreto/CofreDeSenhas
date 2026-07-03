using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CofreDeSenhas.Controles;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaPrincipal : Window
    {
        private IServicoSenha _servicoSenha;
        private readonly IServicoSenha _servicoSenhaLocal;
        private readonly byte[] _chaveMestra;
        private readonly IServicoCriptografia? _criptografia;
        private readonly IRepositorioSenha? _repositorioLocal;
        private readonly ServicoDesbloqueioBiometrico _biometria = new();
        private readonly ServicoAuditoriaSenha _servicoAuditoria = new();
        private readonly ServicoVazamento _servicoVazamento = new();
        private readonly ServicoExportacao _servicoExportacao = new();
        private readonly ServicoImportacaoCsv _servicoImportacaoCsv = new();
        private readonly ServicoTotp _totp = new();
        private readonly Action? _aoBloquear;
        private readonly MonitorInatividade _monitor;
        private bool _conectadoAoBanco;
        private string? _descricaoConexaoAtual;
        private bool _falhaReconexaoAtual;

        private List<Senha> _senhasAtuais = new();
        private readonly List<LinhaSenha> _linhasSenha = new();
        private readonly Dictionary<Guid, ItemAuditoriaSenha> _itensAuditoria = new();
        private ResultadoAuditoriaCofre? _resultadoAuditoria;

        private bool _somenteFavoritos;
        private bool _somenteRecentes;
        private bool _ordenacaoDescendente;
        private bool _navColapsada;
        private Senha? _senhaDetalhe;
        private string _senhaDetalhePlain = "";
        private bool _senhaDetalheVisivel;
        private double _larguraServico = 140;
        private double _larguraUsuario = 240;
        private double _larguraCategoria = 108;
        private double _larguraData = 92;
        private double _larguraAcoes = 170;
        private string? _colunaEmRedimensionamento;
        private string? _colunaDireitaEmRedimensionamento;
        private double _inicioRedimensionamentoX;
        private double _larguraInicialRedimensionamento;
        private double _larguraDireitaInicialRedimensionamento;
        private bool _largurasIniciaisAplicadas;

        private const double LarguraMinimaServico = 88;
        private const double LarguraMinimaUsuario = 160;
        private const double LarguraMinimaCategoria = 86;
        private const double LarguraMinimaData = 78;
        private const double LarguraMinimaAcoes = 170;

        private const string IconeMaximizar = "M6 6 L18 6 L18 18 L6 18 Z";
        private const string IconeRestaurar = "M8 8 L20 8 L20 20 L8 20 Z M4 4 L16 4 L16 6 M4 4 L4 16 L6 16";
        private const string IconeLua = "M21 12.8 C19.8 13.4 18.5 13.8 17.1 13.8 C12.2 13.8 8.2 9.8 8.2 4.9 C8.2 3.5 8.6 2.2 9.2 1 C5.1 2.2 2 6 2 10.5 C2 16.3 6.7 21 12.5 21 C17 21 20.8 17.9 21 12.8 Z";
        private const string IconeSol = "M12 4 L12 1 M12 23 L12 20 M4.9 4.9 L2.8 2.8 M21.2 21.2 L19.1 19.1 M4 12 L1 12 M23 12 L20 12 M4.9 19.1 L2.8 21.2 M21.2 2.8 L19.1 4.9 M12 8 A4 4 0 1 0 12 16 A4 4 0 1 0 12 8";
        private const string IconeOlho = "M2.5 12 C4.8 7.5 8.1 5.5 12 5.5 C15.9 5.5 19.2 7.5 21.5 12 C19.2 16.5 15.9 18.5 12 18.5 C8.1 18.5 4.8 16.5 2.5 12 Z M12 15.5 C13.9 15.5 15.5 13.9 15.5 12 C15.5 10.1 13.9 8.5 12 8.5 C10.1 8.5 8.5 10.1 8.5 12 C8.5 13.9 10.1 15.5 12 15.5 Z";
        private const string IconeOlhoFechado = "M4 4 L20 20 M6.2 6.9 C4.7 8 3.5 9.7 2.5 12 C4.8 16.5 8.1 18.5 12 18.5 C13.2 18.5 14.3 18.3 15.3 17.8 M9.1 9.1 C8.7 9.7 8.5 10.8 8.5 12 C8.5 13.9 10.1 15.5 12 15.5 C13.2 15.5 14.2 14.9 14.8 14 M10.1 5.7 C10.7 5.6 11.3 5.5 12 5.5 C15.9 5.5 19.2 7.5 21.5 12 C20.8 13.4 19.9 14.6 18.9 15.6";

        public JanelaPrincipal(IServicoSenha servicoSenha, byte[] chaveMestra, IServicoCriptografia? criptografia = null,
            IRepositorioSenha? repositorioLocal = null, Action? aoBloquear = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _servicoSenhaLocal = _servicoSenha;
            _chaveMestra = chaveMestra?.ToArray() ?? throw new ArgumentNullException(nameof(chaveMestra));
            _criptografia = criptografia;
            _repositorioLocal = repositorioLocal;
            _aoBloquear = aoBloquear;

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            Acessibilidade.RegistrarAnunciador(this, LblAnuncioLeitorTela);
            ConfigurarAcessibilidadeLeitorTela();

            CmbCategoria.ItemsSource = ConstruirFiltrosOrganizacao(Array.Empty<Senha>());
            CmbCategoria.SelectedIndex = 0;

            Gerador.SolicitouSalvar += Gerador_SolicitouSalvar;

            AtualizarBotaoTema();
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
                MarcarIdiomaSelecionado();
                MarcarAcessibilidadeSelecionada();
                ConfigurarAcessibilidadeLeitorTela();
                AtualizarMenuBiometria();
            };
            Idioma.Alterado += IdiomaGlobal_Alterado;
            Acessibilidade.Alterado += Acessibilidade_Alterado;
            Closed += (s, e) =>
            {
                _monitor.Encerrar();
                Idioma.Alterado -= IdiomaGlobal_Alterado;
                Acessibilidade.Alterado -= Acessibilidade_Alterado;
            };

            Opened += async (s, e) =>
            {
                AjustarLargurasIniciais();
                await IniciarAsync();
            };
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
                        return;
                }
            }

            await CarregarSenhasAsync();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == WindowStateProperty && Moldura != null)
            {
                bool maximizada = WindowState == WindowState.Maximized;
                Moldura.CornerRadius = new CornerRadius(maximizada ? 0 : 10);
                BtnMaximizar.Content = maximizada ? "❐" : "□";
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
            if (sender is not Border divisor || divisor.Tag is not string coluna)
                return;

            var colunaDireita = ObterColunaDireita(coluna);
            if (colunaDireita == null)
                return;

            _colunaEmRedimensionamento = coluna;
            _colunaDireitaEmRedimensionamento = colunaDireita;
            _inicioRedimensionamentoX = e.GetPosition(GridCabecalhoTabela).X;
            _larguraInicialRedimensionamento = ObterLarguraColuna(coluna);
            _larguraDireitaInicialRedimensionamento = ObterLarguraColuna(colunaDireita);
            e.Pointer.Capture(divisor);
            e.Handled = true;
        }

        private void RedimensionarColuna_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_colunaEmRedimensionamento == null || _colunaDireitaEmRedimensionamento == null)
                return;

            var delta = e.GetPosition(GridCabecalhoTabela).X - _inicioRedimensionamentoX;
            var minimoEsquerda = ObterLarguraMinimaColuna(_colunaEmRedimensionamento);
            var minimoDireita = ObterLarguraMinimaColuna(_colunaDireitaEmRedimensionamento);
            var deltaMinimo = minimoEsquerda - _larguraInicialRedimensionamento;
            var deltaMaximo = _larguraDireitaInicialRedimensionamento - minimoDireita;

            delta = Math.Clamp(delta, deltaMinimo, deltaMaximo);

            DefinirLarguraColuna(_colunaEmRedimensionamento, _larguraInicialRedimensionamento + delta);
            DefinirLarguraColuna(_colunaDireitaEmRedimensionamento, _larguraDireitaInicialRedimensionamento - delta);
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

            _larguraAcoes = Math.Clamp(larguraDisponivel * 0.17, LarguraMinimaAcoes, 190);
            _larguraCategoria = Math.Clamp(larguraDisponivel * 0.13, LarguraMinimaCategoria, 132);
            _larguraData = Math.Clamp(larguraDisponivel * 0.11, LarguraMinimaData, 118);

            double flexivel = Math.Max(
                LarguraMinimaServico + LarguraMinimaUsuario,
                larguraDisponivel - fixo - _larguraCategoria - _larguraData - _larguraAcoes);

            _larguraServico = Math.Clamp(flexivel * 0.44, LarguraMinimaServico, 260);
            _larguraUsuario = Math.Max(LarguraMinimaUsuario, flexivel - _larguraServico);

            AplicarLargurasColunas();
        }

        private double ObterLarguraColuna(string coluna) => coluna switch
        {
            "Servico" => _larguraServico,
            "Usuario" => _larguraUsuario,
            "Categoria" => _larguraCategoria,
            "Data" => _larguraData,
            "Acoes" => _larguraAcoes,
            _ => 0
        };

        private static string? ObterColunaDireita(string coluna) => coluna switch
        {
            "Servico" => "Usuario",
            "Usuario" => "Categoria",
            "Categoria" => "Data",
            "Data" => "Acoes",
            _ => null
        };

        private static double ObterLarguraMinimaColuna(string coluna) => coluna switch
        {
            "Servico" => LarguraMinimaServico,
            "Usuario" => LarguraMinimaUsuario,
            "Categoria" => LarguraMinimaCategoria,
            "Data" => LarguraMinimaData,
            "Acoes" => LarguraMinimaAcoes,
            _ => 0
        };

        private void DefinirLarguraColuna(string coluna, double largura)
        {
            switch (coluna)
            {
                case "Servico":
                    _larguraServico = Math.Max(LarguraMinimaServico, largura);
                    break;
                case "Usuario":
                    _larguraUsuario = Math.Max(LarguraMinimaUsuario, largura);
                    break;
                case "Categoria":
                    _larguraCategoria = Math.Max(LarguraMinimaCategoria, largura);
                    break;
                case "Data":
                    _larguraData = Math.Max(LarguraMinimaData, largura);
                    break;
                case "Acoes":
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

        private void Tema_Click(object? sender, RoutedEventArgs e)
        {
            App.AplicarTema(!Tema.ModoEscuro);
            Preferencias.ModoEscuro = Tema.ModoEscuro;
            Preferencias.Salvar();

            AtualizarBotaoTema();
            Gerador.AtualizarTema();
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private void AtualizarBotaoTema()
        {
            BtnTema.Content = Tema.ModoEscuro ? "☀" : "☾";
            var dica = Idioma.Texto(Tema.ModoEscuro ? "Theme.Light" : "Theme.Dark");
            ToolTip.SetTip(BtnTema, dica);
            Avalonia.Automation.AutomationProperties.SetName(BtnTema, dica);
        }

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
            AtualizarBotaoTema();
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

        private void Daltonismo_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
                Acessibilidade.SelecionarDaltonismo(item.Tag as string);
        }

        private void Escala_Alterada(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
                Acessibilidade.SelecionarEscala(item.Tag as string);
        }

        private void AltoContraste_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
                Acessibilidade.SelecionarAltoContraste(item.IsChecked);
        }

        private void ReduzirAnimacoes_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
                Acessibilidade.SelecionarReducaoMovimento(item.IsChecked);
        }

        private void LeitorTela_Alterado(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                Acessibilidade.SelecionarLeitorTela(item.IsChecked);
                Acessibilidade.Anunciar(this, Idioma.Texto(Acessibilidade.LeitorTela
                    ? "A11y.ScreenReaderEnabled"
                    : "A11y.ScreenReaderDisabled"), assertivo: true, forcar: true);
            }
        }

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

        private async void Gerador_SolicitouSalvar(object? sender, string senha)
        {
            var dlg = new JanelaCriarSenha(_servicoSenha, senha);
            if (await dlg.ShowDialog<bool>(this))
            {
                FecharGerador();
                await CarregarSenhasAsync();
            }
        }

        private void NovaSenha_Click(object? sender, RoutedEventArgs e) => AbrirGerador();

        private async Task CarregarSenhasAsync()
        {
            try
            {
                LimparAuditoria();
                _senhasAtuais = await _servicoSenha.ListarTodosAsync();
                AtualizarFiltroOrganizacao();
                FiltrarSenhas();
                AtualizarContador();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.LoadError", ex.Message),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void AtualizarLista(List<Senha> lista)
        {
            PainelLista.Children.Clear();
            _linhasSenha.Clear();

            LblVazio.IsVisible = lista.Count == 0;
            var estadoLista = lista.Count == 0
                ? Idioma.Texto("A11y.EmptyList")
                : Idioma.Plural(lista.Count, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural");
            AutomationProperties.SetName(PainelLista, $"{Idioma.Texto("A11y.ResultsList")}: {estadoLista}");
            AutomationProperties.SetItemStatus(PainelLista, estadoLista);
            AutomationProperties.SetName(LblVazio, Idioma.Texto("Vault.Empty"));

            foreach (var senha in lista)
            {
                var linha = new LinhaSenha(senha, ObterSenhaPlain, ObterTotpPlain, FavoritarToggle, EditarSenha,
                    ExcluirSenhaAsync, RenomearServicoAsync);
                linha.DefinirLargurasColunas(_larguraServico, _larguraUsuario, _larguraCategoria, _larguraData, _larguraAcoes);
                linha.SolicitouDetalhes += Linha_SolicitouDetalhes;

                var plain = ObterSenhaPlain(senha);
                if (!string.IsNullOrEmpty(plain))
                    linha.NivelForca = ForcaSenha.Calcular(plain);
                if (_itensAuditoria.TryGetValue(senha.Id, out var itemAuditoria))
                    linha.DefinirAuditoria(itemAuditoria);

                PainelLista.Children.Add(linha);
                _linhasSenha.Add(linha);
            }
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

        private void Filtro_Alterado(object? sender, SelectionChangedEventArgs e) => FiltrarSenhas();

        private void Busca_Alterada(object? sender, TextChangedEventArgs e) => FiltrarSenhas();

        private void FiltrarSenhas()
        {
            if (PainelLista == null) return;

            var termo = (TxtBusca.Text ?? "").Trim();
            var filtro = CmbCategoria.SelectedItem as FiltroOrganizacao;
            var categoriaFiltro = filtro?.Categoria;
            var etiquetaFiltro = filtro?.Etiqueta;

            var filtradas = _senhasAtuais
                .Where(s => string.IsNullOrEmpty(termo) ||
                    s.NomeServico.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    s.Usuario.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    s.Etiquetas.Any(e => e.Contains(termo, StringComparison.OrdinalIgnoreCase)))
                .Where(s => categoriaFiltro == null || s.Categoria == categoriaFiltro)
                .Where(s => etiquetaFiltro == null ||
                    s.Etiquetas.Any(e => string.Equals(e, etiquetaFiltro, StringComparison.OrdinalIgnoreCase)))
                .Where(s => !_somenteFavoritos || s.Favorito)
                .ToList();

            filtradas = _somenteRecentes
                ? filtradas
                    .OrderByDescending(s => s.DataAtualizacao)
                    .ThenByDescending(s => s.DataCriacao)
                    .ToList()
                : (_ordenacaoDescendente
                    ? filtradas.OrderByDescending(s => s.NomeServico, StringComparer.CurrentCultureIgnoreCase).ToList()
                    : filtradas.OrderBy(s => s.NomeServico, StringComparer.CurrentCultureIgnoreCase).ToList());

            AtualizarLista(filtradas);
        }

        private void AtualizarFiltroOrganizacao()
        {
            if (CmbCategoria == null)
                return;

            var selecionado = CmbCategoria.SelectedItem as FiltroOrganizacao;
            var filtros = ConstruirFiltrosOrganizacao(_senhasAtuais);
            CmbCategoria.ItemsSource = filtros;

            if (selecionado != null)
            {
                var indice = filtros.FindIndex(f => f.MesmaSelecao(selecionado));
                if (indice >= 0)
                {
                    CmbCategoria.SelectedIndex = indice;
                    return;
                }
            }

            CmbCategoria.SelectedIndex = 0;
        }

        private static List<FiltroOrganizacao> ConstruirFiltrosOrganizacao(IEnumerable<Senha> senhas)
        {
            var filtros = new List<FiltroOrganizacao> { FiltroOrganizacao.Todas() };
            var rotulos = CategoriasUI.Rotulos;
            for (int i = 0; i < rotulos.Length; i++)
                filtros.Add(FiltroOrganizacao.ParaCategoria(rotulos[i], (Categoria)i));

            var rotulosCategorias = new HashSet<string>(rotulos, StringComparer.OrdinalIgnoreCase);
            var categoriasPersonalizadas = senhas.Where(s => s.Categoria == Categoria.Other);
            foreach (var etiqueta in Etiquetas.Distintas(categoriasPersonalizadas))
                if (!rotulosCategorias.Contains(etiqueta))
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

        private static AvaloniaPath CriarIcone(string data, double tamanho, IBrush? stroke = null) => new()
        {
            Data = StreamGeometry.Parse(data),
            Width = tamanho,
            Height = tamanho,
            Stretch = Stretch.Uniform,
            Stroke = stroke ?? Tema.Pincel(Tema.TextSecondary),
            StrokeThickness = 2,
            StrokeLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent
        };

        private static string TextoBloqueioAutomatico()
        {
            int minutos = Preferencias.MinutosBloqueio;
            return minutos <= 0
                ? "Bloqueio automático desativado"
                : minutos == 1
                    ? "1 min até bloqueio automático"
                    : $"{minutos} min até bloqueio automático";
        }

        private void ToggleGerador_Click(object? sender, RoutedEventArgs e)
        {
            PainelGeradorFlutuante.IsVisible = !PainelGeradorFlutuante.IsVisible;
            AtualizarFabGerador();
        }

        private void FecharGerador_Click(object? sender, RoutedEventArgs e) => FecharGerador();

        private void AbrirGerador()
        {
            PainelGeradorFlutuante.IsVisible = true;
            AtualizarFabGerador();
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
            NavRail.Width = _navColapsada ? 64 : 224;

            foreach (var texto in TextosNav())
                texto.IsVisible = !_navColapsada;

            LblCategoriasNav.IsVisible = !_navColapsada;
        }

        private IEnumerable<TextBlock> TextosNav()
        {
            yield return LblNavCofre;
            yield return LblNavFavoritas;
            yield return LblNavRecentes;
            yield return LblCatJogos;
            yield return LblCatRedes;
            yield return LblCatEmail;
            yield return LblCatFinanceiro;
            yield return LblCatOutro;
        }

        private void NavCofre_Click(object? sender, RoutedEventArgs e)
        {
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
            _somenteFavoritos = true;
            _somenteRecentes = false;
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private void NavRecentes_Click(object? sender, RoutedEventArgs e)
        {
            _somenteFavoritos = false;
            _somenteRecentes = true;
            PintarFiltroFavoritos();
            AtualizarNavegacao();
            FiltrarSenhas();
        }

        private void NavCategoria_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string tag } || !Enum.TryParse<Categoria>(tag, out var categoria))
                return;

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

        private void AtualizarNavegacao()
        {
            DefinirNavAtivo(BtnNavCofre, !_somenteFavoritos && !_somenteRecentes);
            DefinirNavAtivo(BtnNavFavoritas, _somenteFavoritos);
            DefinirNavAtivo(BtnNavRecentes, _somenteRecentes);
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
            AtualizarDetalheVisual();
            AtualizarSenhaDetalhe();
            PainelDetalhes.IsVisible = true;
        }

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
            BtnDetalheRevelar.Content = CriarIcone(_senhaDetalheVisivel ? IconeOlhoFechado : IconeOlho, 14);
            ToolTip.SetTip(BtnDetalheRevelar, Idioma.Texto(_senhaDetalheVisivel ? "Row.HidePassword" : "Row.RevealPassword"));
        }

        private void FecharDetalhes_Click(object? sender, RoutedEventArgs e) => FecharDetalhes();

        private void FecharDetalhes()
        {
            PainelDetalhes.IsVisible = false;
            _senhaDetalhe = null;
            _senhaDetalhePlain = "";
            _senhaDetalheVisivel = false;
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
                await _servicoSenha.AtualizarSenhaAsync(
                    id,
                    servico,
                    usuario,
                    senhaPlain,
                    _senhaDetalhe.Categoria,
                    string.IsNullOrWhiteSpace(TxtDetalheUrl.Text) ? null : TxtDetalheUrl.Text,
                    string.IsNullOrWhiteSpace(TxtDetalheNotas.Text) ? null : TxtDetalheNotas.Text,
                    _senhaDetalhe.Etiquetas);

                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();

                var atualizada = _senhasAtuais.FirstOrDefault(s => s.Id == id);
                if (atualizada != null)
                    AbrirDetalhes(atualizada);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.UpdateError", ex.Message), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
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
            await CopiarDetalheAsync(TxtDetalheUsuario.Text, Idioma.Texto("Row.CopyUser"));

        private async void CopiarSenhaDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(_senhaDetalheVisivel ? TxtDetalheSenha.Text : _senhaDetalhePlain, Idioma.Texto("Row.CopyPassword"));

        private async void CopiarUrlDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(TxtDetalheUrl.Text, "URL");

        private async Task CopiarDetalheAsync(string? texto, string rotulo)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(texto); } catch { }
            }

            Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", rotulo));
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
                    Idioma.Formatar("Message.FavoriteError", ex.Message),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void EditarSenha(Senha s)
        {
            var dlg = new JanelaEditarSenha(_servicoSenha, s, _criptografia);
            if (await dlg.ShowDialog<bool>(this))
                await CarregarSenhasAsync();
        }

        private async Task ExcluirSenhaAsync(Senha s)
        {
            var complemento = _conectadoAoBanco
                ? Idioma.Texto("Message.DeleteFromDatabase")
                : Idioma.Texto("Message.DeleteFromVault");
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Message.DeletePrompt", s.NomeServico, complemento),
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
                    Idioma.Formatar("Message.DeleteError", ex.Message),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void RemoverSenhaDaLista(Guid id)
        {
            _senhasAtuais.RemoveAll(s => s.Id == id);
            _itensAuditoria.Remove(id);
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
                    Idioma.Formatar("Message.RenameError", ex.Message),
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
                var resultado = _servicoAuditoria.Auditar(_senhasAtuais, ObterSenhaPlain);
                _resultadoAuditoria = resultado;
                _itensAuditoria.Clear();
                foreach (var item in resultado.Itens)
                    _itensAuditoria[item.Senha.Id] = item;

                FiltrarSenhas();
                AtualizarContador();

                await CaixaMensagem.MostrarAsync(this, MontarMensagemAuditoria(resultado), Idioma.Texto("Message.AuditTitle"),
                    resultado.TotalComAchados == 0 ? TipoMensagem.Info : TipoMensagem.Aviso);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.AuditError", ex.Message),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
            finally
            {
                BtnAuditoria.Content = conteudoOriginal;
                BtnAuditoria.IsEnabled = true;
            }
        }

        private void LimparAuditoria()
        {
            _resultadoAuditoria = null;
            _itensAuditoria.Clear();
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
                    Idioma.Formatar("Message.BreachNetworkError", ex.Message),
                    Idioma.Texto("Message.NetworkErrorTitle"), TipoMensagem.Erro);
            }
            finally
            {
                BtnVazamentos.Content = conteudoOriginal;
                BtnVazamentos.IsEnabled = true;
            }
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
                if (!await dlg.ShowDialog<bool>(this))
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
                        TotpSegredo = ObterTotpPlain(s),
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
                    Idioma.Formatar("Message.ExportError", ex.Message),
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
                if (!await dlg.ShowDialog<bool>(this))
                    return;

                List<SenhaExportada> itens;
                try
                {
                    itens = await _servicoExportacao.ImportarAsync(arquivos[0].Path.LocalPath, dlg.SenhaInformada);
                }
                catch (InvalidOperationException ex)
                {
                    await CaixaMensagem.MostrarAsync(this, ex.Message, Idioma.Texto("Common.Import"), TipoMensagem.Aviso);
                    return;
                }

                if (itens.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Message.ImportEmpty"),
                        Idioma.Texto("Common.Import"));
                    return;
                }

                var (adicionadas, ignoradas) = await AplicarImportacaoAsync(itens);

                var msg = Idioma.Formatar("Message.ImportSuccess", adicionadas);
                if (ignoradas > 0)
                    msg += "\n" + Idioma.Formatar("Message.ImportIgnored", ignoradas);
                await CaixaMensagem.MostrarAsync(this, msg, Idioma.Texto("Common.Import"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ImportError", ex.Message),
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
                catch (InvalidOperationException ex)
                {
                    await CaixaMensagem.MostrarAsync(this, ex.Message, Idioma.Texto("Settings.ImportCsv"), TipoMensagem.Aviso);
                    return;
                }

                if (resultado.Itens.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Message.CsvEmpty"),
                        Idioma.Texto("Settings.ImportCsv"));
                    return;
                }

                var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                    Idioma.Formatar("Message.CsvConfirm", resultado.FormatoDetectado, resultado.Itens.Count),
                    Idioma.Texto("Settings.ImportCsv"));
                if (!confirmar)
                    return;

                var (adicionadas, ignoradas) = await AplicarImportacaoAsync(resultado.Itens);
                ignoradas += resultado.LinhasIgnoradas;

                var msg = Idioma.Formatar("Message.ImportSuccess", adicionadas);
                if (ignoradas > 0)
                    msg += "\n" + Idioma.Formatar("Message.CsvIgnored", ignoradas);
                msg += "\n\n" + Idioma.Texto("Message.CsvSecurity");
                await CaixaMensagem.MostrarAsync(this, msg, Idioma.Texto("Settings.ImportCsv"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ImportError", ex.Message),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task<(int adicionadas, int ignoradas)> AplicarImportacaoAsync(List<SenhaExportada> itens)
        {
            var existentes = await _servicoSenha.ListarTodosAsync();
            var chaves = new HashSet<string>(
                existentes.Select(s => s.NomeServico + " " + s.Usuario),
                StringComparer.OrdinalIgnoreCase);

            int adicionadas = 0, ignoradas = 0;
            foreach (var item in itens)
            {
                if (string.IsNullOrWhiteSpace(item.NomeServico) ||
                    string.IsNullOrWhiteSpace(item.Usuario) ||
                    string.IsNullOrWhiteSpace(item.Senha) ||
                    !chaves.Add(item.NomeServico + " " + item.Usuario))
                {
                    ignoradas++;
                    continue;
                }

                var totp = _totp.SegredoValido(item.TotpSegredo) ? item.TotpSegredo : null;
                var nova = await _servicoSenha.CriarSenhaAsync(
                    item.NomeServico, item.Usuario, item.Senha, item.Categoria, item.Url, item.Notas, totp, item.Etiquetas);
                if (item.Favorito)
                    await _servicoSenha.MarcarComoFavoritoAsync(nova.Id);
                adicionadas++;
            }

            await _servicoSenha.PersistirAsync();
            await CarregarSenhasAsync();

            return (adicionadas, ignoradas);
        }

        private async void AlterarSenhaMestra_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaAlterarSenhaMestra();
            if (!await dlg.ShowDialog<bool>(this))
                return;

            try
            {
                var servico = new ServicoMudancaSenhaMestra();
                await servico.AlterarAsync(dlg.SenhaAtual, dlg.NovaSenha);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                await CaixaMensagem.MostrarAsync(this, ex.Message, Idioma.Texto("Master.ChangeTitle"), TipoMensagem.Aviso);
                return;
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Master.ChangeError", ex.Message),
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
            if (!await dlg.ShowDialog<bool>(this))
                return;

            await QrBackup.OferecerSalvarAsync(this, dlg.SenhaConfirmada);
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

        private async void ConectarBanco_Click(object? sender, RoutedEventArgs e)
        {
            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Db.SelectTitle"), TipoMensagem.Aviso);
                return;
            }

            var seletor = new JanelaSelecionarBanco();
            if (!await seletor.ShowDialog<bool>(this) || seletor.Selecionado is not { } tipo)
                return;

            var dlg = new JanelaConexaoBanco(tipo);
            if (!await dlg.ShowDialog<bool>(this) || dlg.Conexao is not { } cfg)
                return;

            await ConectarAsync(cfg, persistir: true, silencioso: false);
        }

        private async Task ConectarAsync(ConexaoBanco cfg, bool persistir, bool silencioso)
        {
            try
            {
                var repoBanco = new RepositorioSenhaBanco(cfg);
                IRepositorioSenha repoAtivo = _repositorioLocal != null
                    ? new RepositorioSenhaEspelhado(_repositorioLocal, repoBanco)
                    : repoBanco;
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
                        Conectado = true
                    };
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
                        Idioma.Formatar("Db.ConnectError", ex.Message),
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
