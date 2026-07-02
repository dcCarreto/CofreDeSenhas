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
        private readonly IServicoCriptografia? _criptografia;
        private readonly IRepositorioSenha? _repositorioLocal;
        private readonly ServicoAuditoriaSenha _servicoAuditoria = new();
        private readonly ServicoVazamento _servicoVazamento = new();
        private readonly ServicoExportacao _servicoExportacao = new();
        private readonly Action? _aoBloquear;
        private readonly MonitorInatividade _monitor;
        private bool _conectadoAoBanco;

        private List<Senha> _senhasAtuais = new();
        private readonly List<LinhaSenha> _linhasSenha = new();
        private readonly Dictionary<Guid, ItemAuditoriaSenha> _itensAuditoria = new();
        private ResultadoAuditoriaCofre? _resultadoAuditoria;

        private bool _somenteFavoritos;

        public JanelaPrincipal(IServicoSenha servicoSenha, IServicoCriptografia? criptografia = null,
            IRepositorioSenha? repositorioLocal = null, Action? aoBloquear = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _servicoSenhaLocal = _servicoSenha;
            _criptografia = criptografia;
            _repositorioLocal = repositorioLocal;
            _aoBloquear = aoBloquear;

            InitializeComponent();
            Icon = Recursos.IconeApp();

            CmbCategoria.ItemsSource = new[] { "Todas" }.Concat(CategoriasUI.Rotulos).ToArray();
            CmbCategoria.SelectedIndex = 0;

            Gerador.SolicitouSalvar += Gerador_SolicitouSalvar;

            AtualizarBotaoTema();
            PintarFiltroFavoritos();

            _monitor = new MonitorInatividade(this, () => _aoBloquear?.Invoke());
            _monitor.Ajustar(Preferencias.MinutosBloqueio);
            BtnConfig.Flyout!.Opened += (s, e) => MarcarBloqueioSelecionado(Preferencias.MinutosBloqueio);
            Closed += (s, e) => _monitor.Encerrar();

            Opened += async (s, e) => await IniciarAsync();
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
            ToolTip.SetTip(BtnTema, Tema.ModoEscuro ? "Tema claro" : "Tema escuro");
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
                FiltrarSenhas();
                AtualizarContador();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this, $"Erro ao carregar: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private void AtualizarLista(List<Senha> lista)
        {
            PainelLista.Children.Clear();
            _linhasSenha.Clear();

            LblVazio.IsVisible = lista.Count == 0;

            foreach (var senha in lista)
            {
                var linha = new LinhaSenha(senha, ObterSenhaPlain, FavoritarToggle, EditarSenha);

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

        private void Filtro_Alterado(object? sender, SelectionChangedEventArgs e) => FiltrarSenhas();

        private void Busca_Alterada(object? sender, TextChangedEventArgs e) => FiltrarSenhas();

        private void FiltrarSenhas()
        {
            if (PainelLista == null) return;

            var termo = (TxtBusca.Text ?? "").ToLower();
            Categoria? categoriaFiltro = null;
            if (CmbCategoria.SelectedIndex > 0)
                categoriaFiltro = (Categoria)(CmbCategoria.SelectedIndex - 1);

            var filtradas = _senhasAtuais
                .Where(s => string.IsNullOrEmpty(termo) || s.NomeServico.ToLower().Contains(termo) || s.Usuario.ToLower().Contains(termo))
                .Where(s => categoriaFiltro == null || s.Categoria == categoriaFiltro)
                .Where(s => !_somenteFavoritos || s.Favorito)
                .ToList();

            AtualizarLista(filtradas);
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
            LblContadorHeader.Text = total == 1 ? "1 item" : $"{total} itens";
            var status = $"{total} {(total == 1 ? "senha" : "senhas")} • {favoritos} {(favoritos == 1 ? "favorita" : "favoritas")}";
            if (_resultadoAuditoria is { } auditoria)
            {
                status += auditoria.TotalComAchados == 0
                    ? " • auditoria OK"
                    : $" • {auditoria.TotalComAchados} com alerta";
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
                await CaixaMensagem.MostrarAsync(this, $"Erro ao favoritar: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private async void EditarSenha(Senha s)
        {
            var dlg = new JanelaEditarSenha(_servicoSenha, s, _criptografia);
            if (await dlg.ShowDialog<bool>(this))
                await CarregarSenhasAsync();
        }

        private async void AuditarCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this, "Não há senhas no cofre para auditar.", "Auditoria do cofre");
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

                await CaixaMensagem.MostrarAsync(this, MontarMensagemAuditoria(resultado), "Auditoria do cofre",
                    resultado.TotalComAchados == 0 ? TipoMensagem.Info : TipoMensagem.Aviso);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this, $"Erro ao auditar o cofre: {ex.Message}", "Erro", TipoMensagem.Erro);
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
                var msg = $"Auditoria concluída: nenhuma senha fraca, repetida ou antiga entre {resultado.TotalSenhas} senha(s).";
                if (resultado.NaoAuditadas > 0)
                    msg += $"\n{resultado.NaoAuditadas} senha(s) não puderam ser analisadas por completo.";
                return msg;
            }

            var linhas = new List<string>
            {
                $"Encontradas {resultado.TotalComAchados} entrada(s) com alerta em {resultado.TotalSenhas} senha(s):",
                $"- {resultado.TotalFracas} fraca(s)",
                $"- {resultado.TotalRepetidas} repetida(s)",
                $"- {resultado.TotalAntigas} antiga(s)"
            };

            if (resultado.NaoAuditadas > 0)
                linhas.Add($"- {resultado.NaoAuditadas} não auditada(s) por falha de leitura");

            linhas.Add("");
            linhas.Add("As entradas afetadas foram marcadas com ⚠ na lista.");
            linhas.Add($"Senhas antigas são as sem atualização há {ServicoAuditoriaSenha.DiasSenhaAntigaPadrao} dias ou mais.");

            return string.Join(Environment.NewLine, linhas);
        }

        private async void VerificarVazamentos_Click(object? sender, RoutedEventArgs e)
        {
            if (_linhasSenha.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this, "Não há senhas no cofre para verificar.", "Verificar vazamentos");
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
                    ? $"Boa notícia! Nenhuma das {verificadas} senha(s) verificada(s) foi encontrada em vazamentos conhecidos."
                    : $"Atenção: {comprometidas} de {verificadas} senha(s) aparecem em vazamentos conhecidos. Considere trocá-las (marcadas com ⚠).";

                await CaixaMensagem.MostrarAsync(this, msg, "Verificação concluída",
                    comprometidas == 0 ? TipoMensagem.Info : TipoMensagem.Aviso);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    $"Não foi possível verificar os vazamentos.\nVerifique sua conexão com a internet.\n\nDetalhe: {ex.Message}",
                    "Erro de rede", TipoMensagem.Erro);
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
                    await CaixaMensagem.MostrarAsync(this, "O cofre está vazio. Não há nada para exportar.", "Exportar");
                    return;
                }

                var dlg = new JanelaSenhaExportacao(modoExportar: true);
                if (!await dlg.ShowDialog<bool>(this))
                    return;

                var arquivo = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Exportar senhas",
                    SuggestedFileName = $"cofre-senhas-{DateTime.Now:yyyy-MM-dd}.gsenhas",
                    DefaultExtension = "gsenhas",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Cofre exportado") { Patterns = new[] { "*.gsenhas" } },
                        new FilePickerFileType("Todos os arquivos") { Patterns = new[] { "*" } }
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
                        Notas = s.Notas,
                        Favorito = s.Favorito,
                        DataCriacao = s.DataCriacao,
                        DataAtualizacao = s.DataAtualizacao
                    });
                }

                await _servicoExportacao.ExportarAsync(arquivo.Path.LocalPath, itens, dlg.SenhaInformada);

                await CaixaMensagem.MostrarAsync(this,
                    $"{itens.Count} senha(s) exportada(s) com sucesso.\n\nGuarde bem a senha de exportação — sem ela o arquivo não pode ser aberto.",
                    "Exportar");
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this, $"Erro ao exportar: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private async void Importar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var arquivos = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Importar senhas",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Cofre exportado") { Patterns = new[] { "*.gsenhas" } },
                        new FilePickerFileType("Todos os arquivos") { Patterns = new[] { "*" } }
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
                    await CaixaMensagem.MostrarAsync(this, ex.Message, "Importar", TipoMensagem.Aviso);
                    return;
                }

                if (itens.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this, "Nenhuma senha encontrada no arquivo.", "Importar");
                    return;
                }

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

                    var nova = await _servicoSenha.CriarSenhaAsync(
                        item.NomeServico, item.Usuario, item.Senha, item.Categoria, item.Url, item.Notas);
                    if (item.Favorito)
                        await _servicoSenha.MarcarComoFavoritoAsync(nova.Id);
                    adicionadas++;
                }

                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();

                var msg = $"{adicionadas} senha(s) importada(s) com sucesso.";
                if (ignoradas > 0)
                    msg += $"\n{ignoradas} ignorada(s) (já existiam ou inválidas).";
                await CaixaMensagem.MostrarAsync(this, msg, "Importar");
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this, $"Erro ao importar: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
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
                await CaixaMensagem.MostrarAsync(this, ex.Message, "Alterar senha mestra", TipoMensagem.Aviso);
                return;
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this, $"Erro ao alterar a senha mestra: {ex.Message}", "Erro", TipoMensagem.Erro);
                return;
            }

            await QrBackup.OferecerSalvarAsync(this, dlg.NovaSenha);

            await CaixaMensagem.MostrarAsync(this,
                "Senha mestra alterada com sucesso.\n\nO aplicativo será reiniciado para aplicar a nova senha.",
                "Alterar senha mestra");
            Reiniciar();
        }

        private async void RegerarQrCode_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaConfirmarSenhaMestra();
            if (!await dlg.ShowDialog<bool>(this))
                return;

            await QrBackup.OferecerSalvarAsync(this, dlg.SenhaConfirmada);
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
                    "Recurso indisponível nesta sessão.", "Conectar banco de dados", TipoMensagem.Aviso);
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
                        $"Conectado ao banco de dados e sincronizado com o cofre local.\n\n{cfg.Descricao}",
                        "Banco de dados");
            }
            catch (Exception ex)
            {
                _servicoSenha = _servicoSenhaLocal;
                _conectadoAoBanco = false;
                AtualizarEstadoConexao(null, falhaReconexao: silencioso);

                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        $"Erro ao conectar: {ex.Message}", "Erro", TipoMensagem.Erro);
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
            if (_conectadoAoBanco && descricao != null)
            {
                LblConexao.Text = "Conectado: " + descricao;
                PontoConexao.Fill = new SolidColorBrush(Color.Parse("#3B82F6"));
                MenuDesconectarBanco.IsVisible = true;
            }
            else if (falhaReconexao)
            {
                LblConexao.Text = "Banco indisponível — usando cofre local";
                PontoConexao.Fill = new SolidColorBrush(Color.Parse("#F59E0B"));
                MenuDesconectarBanco.IsVisible = true;
            }
            else
            {
                LblConexao.Text = "Cofre criptografado";
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
    }
}
