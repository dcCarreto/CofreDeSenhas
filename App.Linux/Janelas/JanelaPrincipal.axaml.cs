using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AppLinux.Controles;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace AppLinux.Janelas
{
    public partial class JanelaPrincipal : Window
    {
        private readonly IServicoSenha _servicoSenha;
        private readonly IServicoCriptografia? _criptografia;
        private readonly ServicoVazamento _servicoVazamento = new();
        private readonly ServicoExportacao _servicoExportacao = new();

        private List<Senha> _senhasAtuais = new();
        private readonly List<LinhaSenha> _linhasSenha = new();
        private readonly List<string> _senhasGeradas = new();

        private string _senhaGerada = "";
        private bool _mostrarSenha = true;
        private bool _somenteFavoritos;
        private int _nivelForca;

        public JanelaPrincipal(IServicoSenha servicoSenha, IServicoCriptografia? criptografia = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _criptografia = criptografia;

            InitializeComponent();
            Icon = Recursos.IconeApp();

            CmbCategoria.ItemsSource = new[] { "Todas" }.Concat(CategoriasUI.Rotulos).ToArray();
            CmbCategoria.SelectedIndex = 0;

            SliderTamanho.ValueChanged += (s, e) => LblTamanhoValor.Text = SliderTamanho.Value.ToString();
            SliderQuantidade.ValueChanged += (s, e) => LblQuantidadeValor.Text = SliderQuantidade.Value.ToString();

            AtualizarBotaoTema();
            PintarFiltroFavoritos();

            Opened += async (s, e) => await CarregarSenhasAsync();
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

        // ---- janela sem decoração ----

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

        // ---- tema ----

        private void Tema_Click(object? sender, RoutedEventArgs e)
        {
            App.AplicarTema(!Tema.ModoEscuro);
            Preferencias.ModoEscuro = Tema.ModoEscuro;
            Preferencias.Salvar();

            AtualizarBotaoTema();
            AtualizarBarraForca();
            AtualizarListaSenhasGeradas();
            PintarFiltroFavoritos();
            FiltrarSenhas();
        }

        private void AtualizarBotaoTema()
        {
            BtnTema.Content = Tema.ModoEscuro ? "☀" : "🌙";
            ToolTip.SetTip(BtnTema, Tema.ModoEscuro ? "Tema claro" : "Tema escuro");
        }

        // ---- gerador ----

        private async void Gerar_Click(object? sender, RoutedEventArgs e)
        {
            string opcoes = "";
            if (ToggleMaiusculas.Checked) opcoes += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (ToggleMinusculas.Checked) opcoes += "abcdefghijklmnopqrstuvwxyz";
            if (ToggleNumeros.Checked) opcoes += "0123456789";
            if (ToggleEspeciais.Checked) opcoes += "!@#$%^&*()_+-=[]{}|;:,.<>?";

            if (string.IsNullOrEmpty(opcoes))
            {
                await CaixaMensagem.MostrarAsync(this, "Selecione pelo menos uma opção", "Aviso", TipoMensagem.Aviso);
                return;
            }

            int quantidade = SliderQuantidade.Value;
            _senhasGeradas.Clear();
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int n = 0; n < quantidade; n++)
                    _senhasGeradas.Add(GerarUmaSenha(rng, opcoes, SliderTamanho.Value));
            }

            _senhaGerada = _senhasGeradas[0];
            TxtSenhaGerada.Text = _mostrarSenha ? _senhaGerada : new string('•', _senhaGerada.Length);
            AtualizarBarraForca();
            AtualizarListaSenhasGeradas();
        }

        private static string GerarUmaSenha(RandomNumberGenerator rng, string opcoes, int tamanho)
        {
            var senha = new StringBuilder(tamanho);
            byte[] data = new byte[1];
            for (int i = 0; i < tamanho; i++)
            {
                rng.GetBytes(data);
                senha.Append(opcoes[data[0] % opcoes.Length]);
            }
            return senha.ToString();
        }

        private void AtualizarListaSenhasGeradas()
        {
            PainelGeradas.Children.Clear();
            if (_senhasGeradas.Count <= 1)
                return;

            var titulo = new TextBlock
            {
                Text = $"Senhas geradas ({_senhasGeradas.Count})",
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var btnCopiarTodas = new Button
            {
                Content = "Copiar todas",
                Height = 26,
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.AccentPrimary),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            btnCopiarTodas.Classes.Add("plano");
            btnCopiarTodas.Click += async (s, e) =>
            {
                if (Clipboard != null)
                    try { await Clipboard.SetTextAsync(string.Join(Environment.NewLine, _senhasGeradas)); } catch { }
            };

            var header = new Grid { Margin = new Thickness(0, 8, 0, 6) };
            header.Children.Add(titulo);
            header.Children.Add(btnCopiarTodas);
            PainelGeradas.Children.Add(header);

            foreach (var senha in _senhasGeradas)
                PainelGeradas.Children.Add(CriarItemSenhaGerada(senha));
        }

        private Border CriarItemSenhaGerada(string senha)
        {
            var lbl = new TextBlock
            {
                Text = senha,
                FontFamily = (FontFamily)Application.Current!.FindResource("FonteMono")!,
                FontSize = 13,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 6, 0)
            };

            var btnCopiar = new Button { Content = "⧉", Width = 28, Height = 28 };
            btnCopiar.Classes.Add("icone-linha");
            btnCopiar.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            btnCopiar.Margin = new Thickness(0, 0, 5, 0);
            btnCopiar.Click += async (s, e) =>
            {
                if (Clipboard != null)
                    try { await Clipboard.SetTextAsync(senha); } catch { }
                btnCopiar.Content = "✓";
                btnCopiar.Foreground = Tema.Pincel(Tema.StrengthStrong);
                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                t.Tick += (ss, ee) =>
                {
                    btnCopiar.Content = "⧉";
                    btnCopiar.ClearValue(ForegroundProperty);
                    t.Stop();
                };
                t.Start();
            };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(lbl);
            Grid.SetColumn(btnCopiar, 1);
            grid.Children.Add(btnCopiar);

            return new Border
            {
                Height = 38,
                CornerRadius = new CornerRadius(8),
                Background = Tema.Pincel(Tema.InputBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 6),
                Child = grid
            };
        }

        private void AtualizarBarraForca()
        {
            _nivelForca = CalcularForcaSenha(_senhaGerada);
            var (texto, cor) = _nivelForca switch
            {
                1 => ("Fraca", Tema.StrengthWeak),
                2 => ("Média", Tema.StrengthMedium),
                3 or 4 => ("Forte", Tema.StrengthStrong),
                _ => ("—", Tema.TextSecondary)
            };

            LblForca.Text = texto;
            LblForca.Foreground = Tema.Pincel(cor);

            var segmentos = new[] { SegForca1, SegForca2, SegForca3, SegForca4 };
            for (int i = 0; i < segmentos.Length; i++)
                segmentos[i].Background = Tema.Pincel(i < _nivelForca ? cor : Tema.TrailInactive);
        }

        private static int CalcularForcaSenha(string senha)
        {
            int forca = 0;
            if (string.IsNullOrEmpty(senha)) return 0;
            if (senha.Length >= 8) forca++;
            if (senha.Length >= 12) forca++;
            if (System.Text.RegularExpressions.Regex.IsMatch(senha, "[A-Z]") &&
                System.Text.RegularExpressions.Regex.IsMatch(senha, "[a-z]")) forca++;
            if (System.Text.RegularExpressions.Regex.IsMatch(senha, "[0-9]")) forca++;
            return Math.Min(forca, 4);
        }

        private void OlhoGerada_Click(object? sender, RoutedEventArgs e)
        {
            _mostrarSenha = !_mostrarSenha;
            if (!string.IsNullOrEmpty(_senhaGerada))
                TxtSenhaGerada.Text = _mostrarSenha ? _senhaGerada : new string('•', _senhaGerada.Length);
        }

        private async void CopiarGerada_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_senhaGerada) || Clipboard == null)
                return;
            await Clipboard.SetTextAsync(_senhaGerada);
            await CaixaMensagem.MostrarAsync(this, "Senha copiada!", "Sucesso");
        }

        private void Limpar_Click(object? sender, RoutedEventArgs e)
        {
            TxtSenhaGerada.Text = "";
            _senhaGerada = "";
            _mostrarSenha = true;
            _senhasGeradas.Clear();
            AtualizarBarraForca();
            AtualizarListaSenhasGeradas();
        }

        private async void SalvarNoCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_senhaGerada))
            {
                await CaixaMensagem.MostrarAsync(this, "Gere uma senha primeiro", "Aviso", TipoMensagem.Aviso);
                return;
            }

            var dlg = new JanelaCriarSenha(_servicoSenha, _senhaGerada);
            if (await dlg.ShowDialog<bool>(this))
                await CarregarSenhasAsync();
        }

        // ---- cofre ----

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
                    linha.NivelForca = CalcularForcaSenha(plain);

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
            LblStatus.Text = $"{total} {(total == 1 ? "senha" : "senhas")} • {favoritos} {(favoritos == 1 ? "favorita" : "favoritas")}";
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

        // ---- vazamentos ----

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

        // ---- exportar / importar ----

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

        // ---- senha mestra ----

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
