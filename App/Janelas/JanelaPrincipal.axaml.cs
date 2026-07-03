using System.Diagnostics;
using Avalonia;
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

            CmbCategoria.ItemsSource = ConstruirFiltrosOrganizacao(Array.Empty<Senha>());
            CmbCategoria.SelectedIndex = 0;

            Gerador.SolicitouSalvar += Gerador_SolicitouSalvar;

            AtualizarBotaoTema();
            PintarFiltroFavoritos();
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
                AtualizarMenuBiometria();
            };
            Idioma.Alterado += IdiomaGlobal_Alterado;
            Closed += (s, e) =>
            {
                _monitor.Encerrar();
                Idioma.Alterado -= IdiomaGlobal_Alterado;
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
            double fixo = 42 + 44 + 26 + 24;

            _larguraAcoes = Math.Clamp(larguraDisponivel * 0.16, LarguraMinimaAcoes, 186);
            _larguraCategoria = Math.Clamp(larguraDisponivel * 0.12, LarguraMinimaCategoria, 116);
            _larguraData = Math.Clamp(larguraDisponivel * 0.10, LarguraMinimaData, 100);

            double flexivel = Math.Max(
                LarguraMinimaServico + LarguraMinimaUsuario,
                larguraDisponivel - fixo - _larguraCategoria - _larguraData - _larguraAcoes);

            _larguraServico = Math.Clamp(flexivel * 0.34, LarguraMinimaServico, 145);
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

            GridCabecalhoTabela.ColumnDefinitions[3].Width = new GridLength(_larguraServico);
            GridCabecalhoTabela.ColumnDefinitions[5].Width = new GridLength(_larguraUsuario);
            GridCabecalhoTabela.ColumnDefinitions[7].Width = new GridLength(_larguraCategoria);
            GridCabecalhoTabela.ColumnDefinitions[9].Width = new GridLength(_larguraData);
            GridCabecalhoTabela.ColumnDefinitions[11].Width = new GridLength(_larguraAcoes);

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
            FiltrarSenhas();
        }

        private void AtualizarBotaoTema()
        {
            BtnTema.Content = Tema.ModoEscuro ? "☀" : "🌙";
            ToolTip.SetTip(BtnTema, Idioma.Texto(Tema.ModoEscuro ? "Theme.Light" : "Theme.Dark"));
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

        private async void Gerador_SolicitouSalvar(object? sender, string senha)
        {
            var dlg = new JanelaCriarSenha(_servicoSenha, senha);
            if (await dlg.ShowDialog<bool>(this))
                await CarregarSenhasAsync();
        }

        private async void NovaSenha_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaCriarSenha(_servicoSenha);
            if (await dlg.ShowDialog<bool>(this))
                await CarregarSenhasAsync();
        }

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

            foreach (var senha in lista)
            {
                var linha = new LinhaSenha(senha, ObterSenhaPlain, ObterTotpPlain, FavoritarToggle, EditarSenha,
                    ExcluirSenhaAsync, RenomearServicoAsync);
                linha.DefinirLargurasColunas(_larguraServico, _larguraUsuario, _larguraCategoria, _larguraData, _larguraAcoes);

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
            PintarFiltroFavoritos();
            FiltrarSenhas();
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

            LblStatus.Text = status;
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

            if (_conectadoAoBanco && descricao != null)
            {
                LblConexao.Text = Idioma.Formatar("Vault.Connection.Connected", descricao);
                PontoConexao.Fill = new SolidColorBrush(Color.Parse("#3B82F6"));
                MenuDesconectarBanco.IsVisible = true;
            }
            else if (falhaReconexao)
            {
                LblConexao.Text = Idioma.Texto("Vault.Connection.DatabaseUnavailable");
                PontoConexao.Fill = new SolidColorBrush(Color.Parse("#F59E0B"));
                MenuDesconectarBanco.IsVisible = true;
            }
            else
            {
                LblConexao.Text = Idioma.Texto("Vault.Connection.Local");
                PontoConexao.Fill = new SolidColorBrush(Color.Parse("#22C55E"));
                MenuDesconectarBanco.IsVisible = false;
            }
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
