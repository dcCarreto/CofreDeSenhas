using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using CofreDeSenhas.Controles;

namespace CofreDeSenhas.Janelas
{
    public sealed class AcoesConfiguracoes
    {
        public required Action AlterarSenhaMestra { get; init; }
        public required Action RegerarQr { get; init; }
        public required Action ChaveRecuperacaoAtivarOuGerar { get; init; }
        public required Action ChaveRecuperacaoDesativar { get; init; }
        public bool ChaveRecuperacaoAtiva { get; init; }
        public required Action AlternarWindowsHello { get; init; }
        public required Action BloquearAgora { get; init; }
        public required Action Backup { get; init; }
        public required Action Sincronizacao { get; init; }
        public required Action ImportarCsv { get; init; }
        public required Action ConectarBanco { get; init; }
        public required Action DesconectarBanco { get; init; }
        public required Action AtalhosTeclado { get; init; }
        public required Action AbrirManual { get; init; }
        public required Action LimparCofre { get; init; }
        public required Action ExcluirCofre { get; init; }
        public required Action<int> DefinirBloqueioAutomatico { get; init; }
        public required Action<bool> DefinirVerificarAtualizacoes { get; init; }
        public required Action<bool> DefinirIconesOnline { get; init; }
        public bool WindowsHelloSuportado { get; init; }
        public bool WindowsHelloAtivo { get; init; }
        public bool BancoConectado { get; init; }
    }

    public partial class JanelaConfiguracoes : Window
    {
        private readonly AcoesConfiguracoes _acoes;
        private readonly List<(Button Botao, Control Painel)> _abas = new();
        private bool _montando;
        private int _abaAtual;
        private bool _editandoPerfil;
        private string _rascunhoPerfil = "";
        private Task? _baixandoVoz;
        private double _progressoVoz;
        private bool _erroVoz;

        public Action? AcaoPendente { get; private set; }

        public JanelaConfiguracoes(AcoesConfiguracoes acoes)
        {
            _acoes = acoes;
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            this.FecharComEsc();
            this.AtalhoAjuda("aparencia-acessibilidade");

            Construir();

            Idioma.Alterado += AoRefazer;
            Acessibilidade.Alterado += AoRefazer;
            Closed += (s, e) =>
            {
                Idioma.Alterado -= AoRefazer;
                Acessibilidade.Alterado -= AoRefazer;
            };
            Opened += (s, e) => (_abas.Count > 0 ? _abas[_abaAtual].Botao : (Control)BtnFechar).Focus();
        }

        private void AoRefazer(object? sender, EventArgs e) => Construir();

        private void Construir()
        {
            _montando = true;
            TrilhaAbas.Children.Clear();
            AreaConteudo.Children.Clear();
            _abas.Clear();

            AdicionarAba("IconeIdioma", "Config.Tab.Appearance", ConstruirAparencia());
            AdicionarAba("IconeConfiguracoes", "Config.Tab.Behavior", ConstruirComportamento());
            AdicionarAba("IconeCadeado", "Config.Tab.Security", ConstruirSeguranca());
            AdicionarAba("IconeSincronizacao", "Config.Tab.BackupSync", ConstruirBackupSync());
            AdicionarAba("IconeAcessibilidade", "Config.Tab.Accessibility", ConstruirAcessibilidade());
            AdicionarAba("IconeAtalhoTeclado", "Config.Tab.Help", ConstruirAjuda());
            AdicionarAba("IconeAviso", "Config.Tab.Danger", ConstruirZonaPerigo());

            _montando = false;
            SelecionarAba(Math.Clamp(_abaAtual, 0, _abas.Count - 1));
        }

        private void AdicionarAba(string iconeChave, string tituloChave, Control painel)
        {
            int indice = _abas.Count;

            var grade = new Grid { ColumnDefinitions = new ColumnDefinitions("26,*") };
            grade.Children.Add(new Icone
            {
                Chave = iconeChave,
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            var titulo = new TextBlock
            {
                Text = Idioma.Texto(tituloChave),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13.5,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(titulo, 1);
            grade.Children.Add(titulo);

            var botao = new Button
            {
                Content = grade,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            botao.Classes.Add("nav-item");
            AutomationProperties.SetName(botao, Idioma.Texto(tituloChave));
            botao.Click += (s, e) => SelecionarAba(indice);

            painel.IsVisible = false;

            TrilhaAbas.Children.Add(botao);
            AreaConteudo.Children.Add(painel);
            _abas.Add((botao, painel));
        }

        private void SelecionarAba(int indice)
        {
            _abaAtual = indice;
            for (int i = 0; i < _abas.Count; i++)
            {
                bool ativo = i == indice;
                _abas[i].Botao.Classes.Set("ativo", ativo);
                _abas[i].Painel.IsVisible = ativo;
            }
            RolagemConteudo.Offset = default;
        }

        private StackPanel NovoPainel(string introChave)
        {
            var painel = new StackPanel { Spacing = 12 };
            painel.Children.Add(new TextBlock
            {
                Text = Idioma.Texto(introChave),
                Foreground = Tema.Pincel(Tema.TextSecondary),
                FontSize = 12.5,
                Margin = new Thickness(2, 0, 2, 6),
                TextWrapping = TextWrapping.Wrap
            });
            return painel;
        }

        private void Secao(StackPanel painel, string chave)
        {
            painel.Children.Add(new TextBlock
            {
                Text = Idioma.Texto(chave).ToUpperInvariant(),
                Foreground = Tema.Pincel(Tema.TextTertiary),
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(2, painel.Children.Count > 1 ? 14 : 0, 2, 2)
            });
        }

        private Control ConstruirAparencia()
        {
            var painel = NovoPainel("Config.Appearance.Intro");

            ConstruirSecaoPerfil(painel);

            string[] modos = { "Sistema", "Claro", "Escuro" };
            string[] rotulosModo =
            {
                Idioma.Texto("Config.Theme.System"),
                Idioma.Texto("Config.Theme.Light"),
                Idioma.Texto("Config.Theme.Dark")
            };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Config.Theme"), null, rotulosModo,
                Math.Max(0, Array.IndexOf(modos, Acessibilidade.Modo.ToString())),
                indice => Acessibilidade.SelecionarModoTema(modos[indice])));

            string[] destaques = { "Ambar", "Azul", "Verde", "Ameixa", "Terracota", "Grafite" };
            string[] rotulosDestaque =
            {
                Idioma.Texto("Config.Accent.Ambar"),
                Idioma.Texto("Config.Accent.Azul"),
                Idioma.Texto("Config.Accent.Verde"),
                Idioma.Texto("Config.Accent.Ameixa"),
                Idioma.Texto("Config.Accent.Terracota"),
                Idioma.Texto("Config.Accent.Grafite")
            };
            var linhaDestaque = LinhaCombo(
                Idioma.Texto("Config.Accent"),
                Acessibilidade.DestaqueDisponivel ? null : Idioma.Texto("Config.Accent.LockedByColorblind"),
                rotulosDestaque,
                Math.Max(0, Array.IndexOf(destaques, Acessibilidade.Destaque.ToString())),
                indice => Acessibilidade.SelecionarCorDestaque(destaques[indice]));
            linhaDestaque.IsEnabled = Acessibilidade.DestaqueDisponivel;
            painel.Children.Add(linhaDestaque);

            string[] densidades = { "Confortavel", "Compacto" };
            string[] rotulosDensidade = { Idioma.Texto("Config.Density.Comfortable"), Idioma.Texto("Config.Density.Compact") };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Config.Density"), null, rotulosDensidade,
                Math.Max(0, Array.IndexOf(densidades, Acessibilidade.Densidade.ToString())),
                indice => Acessibilidade.SelecionarDensidade(densidades[indice])));

            string[] layouts = { "Lateral", "Inferior" };
            string[] rotulosLayout = { Idioma.Texto("Config.DetailLayout.Side"), Idioma.Texto("Config.DetailLayout.Bottom") };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Config.DetailLayout"), null, rotulosLayout,
                Math.Max(0, Array.IndexOf(layouts, Acessibilidade.LayoutDetalhe.ToString())),
                indice => Acessibilidade.SelecionarLayoutDetalhe(layouts[indice])));

            painel.Children.Add(LinhaCheck(Idioma.Texto("Config.Columns.User"),
                Idioma.Texto("Config.Columns.Intro"),
                Acessibilidade.ColunasLista.HasFlag(ColunasLista.Usuario),
                v => Acessibilidade.SelecionarColunaLista(ColunasLista.Usuario, v)));
            painel.Children.Add(LinhaCheck(Idioma.Texto("Config.Columns.Category"), null,
                Acessibilidade.ColunasLista.HasFlag(ColunasLista.Categoria),
                v => Acessibilidade.SelecionarColunaLista(ColunasLista.Categoria, v)));
            painel.Children.Add(LinhaCheck(Idioma.Texto("Config.Columns.Strength"), null,
                Acessibilidade.ColunasLista.HasFlag(ColunasLista.Forca),
                v => Acessibilidade.SelecionarColunaLista(ColunasLista.Forca, v)));

            var idiomas = Idioma.Idiomas;

            int atual = 0;
            for (int i = 0; i < idiomas.Count; i++)
                if (string.Equals(idiomas[i].Codigo, Idioma.Atual.Codigo, StringComparison.OrdinalIgnoreCase))
                    atual = i;

            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Settings.Language"), null,
                idiomas.Select(i => i.NomeNativo).ToArray(), atual,
                indice =>
                {
                    Idioma.Definir(idiomas[indice].Codigo);
                    Preferencias.Idioma = Idioma.Atual.Codigo;
                    Preferencias.Salvar();
                }));

            return painel;
        }

        private void ConstruirSecaoPerfil(StackPanel painel)
        {
            var rotulos = new List<string>();
            var perfis = new List<PerfilAparencia?>();

            foreach (var (id, perfil) in Aparencia.Presets)
            {
                rotulos.Add(Idioma.Texto("Config.Profile." + id));
                perfis.Add(perfil);
            }
            foreach (var salvo in Aparencia.Salvos)
            {
                rotulos.Add(salvo.Nome);
                perfis.Add(salvo);
            }
            rotulos.Add(Idioma.Texto("Config.Profile.Custom"));
            perfis.Add(null);

            var identificacao = Aparencia.IdentificacaoAtual();
            int selecionado = perfis.Count - 1;
            for (int i = 0; i < perfis.Count - 1; i++)
            {
                var referencia = i < Aparencia.Presets.Count ? Aparencia.Presets[i].Id : Aparencia.Salvos[i - Aparencia.Presets.Count].Nome;
                if (string.Equals(referencia, identificacao, StringComparison.OrdinalIgnoreCase))
                    selecionado = i;
            }

            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Config.Profile"), null, rotulos.ToArray(), selecionado,
                indice =>
                {
                    if (perfis[indice] is { } escolhido)
                        Aparencia.Aplicar(escolhido);
                }));

            bool salvoAtivo = identificacao != null &&
                Aparencia.Salvos.Any(p => string.Equals(p.Nome, identificacao, StringComparison.OrdinalIgnoreCase));

            painel.Children.Add(_editandoPerfil ? LinhaSalvarPerfil() : LinhaAcoesPerfil(identificacao, salvoAtivo));
        }

        private Control LinhaAcoesPerfil(string? identificacao, bool salvoAtivo)
        {
            var linha = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(2, 0, 0, 4) };

            var salvar = new Button { Content = Idioma.Texto("Config.Profile.Save"), Height = 34, FontSize = 12.5 };
            salvar.Classes.Add("secundario");
            salvar.Click += (s, e) =>
            {
                _editandoPerfil = true;
                _rascunhoPerfil = "";
                Construir();
            };
            linha.Children.Add(salvar);

            if (salvoAtivo)
            {
                var excluir = new Button { Content = Idioma.Texto("Config.Profile.Delete"), Height = 34, FontSize = 12.5 };
                excluir.Classes.Add("secundario");
                excluir.Click += (s, e) =>
                {
                    Aparencia.Excluir(identificacao!);
                    Construir();
                };
                linha.Children.Add(excluir);
            }

            return linha;
        }

        private Control LinhaSalvarPerfil()
        {
            var nome = new TextBox
            {
                Watermark = Idioma.Texto("Config.Profile.NamePlaceholder"),
                Width = 220,
                Height = 34,
                Text = _rascunhoPerfil
            };
            nome.Classes.Add("campo");
            nome.TextChanged += (s, e) => _rascunhoPerfil = nome.Text ?? "";

            var confirmar = new Button { Content = Idioma.Texto("Common.Save"), Height = 34, FontSize = 12.5 };
            confirmar.Classes.Add("primario");
            confirmar.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(_rascunhoPerfil))
                    Aparencia.Salvar(_rascunhoPerfil);
                _editandoPerfil = false;
                Construir();
            };

            var cancelar = new Button { Content = Idioma.Texto("Common.Cancel"), Height = 34, FontSize = 12.5 };
            cancelar.Classes.Add("secundario");
            cancelar.Click += (s, e) =>
            {
                _editandoPerfil = false;
                Construir();
            };

            var linha = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(2, 0, 0, 4) };
            linha.Children.Add(nome);
            linha.Children.Add(confirmar);
            linha.Children.Add(cancelar);
            return linha;
        }

        private Control ConstruirComportamento()
        {
            var painel = NovoPainel("Config.Behavior.Intro");

            int[] minutos = { 0, 1, 5, 15, 30 };
            string[] rotulosBloqueio =
            {
                Idioma.Texto("Settings.Disabled"),
                Idioma.Texto("Settings.AfterOneMinute"),
                Idioma.Texto("Settings.AfterMinutes5"),
                Idioma.Texto("Settings.AfterMinutes15"),
                Idioma.Texto("Settings.AfterMinutes30")
            };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Settings.AutoLock"), null, rotulosBloqueio,
                Math.Max(0, Array.IndexOf(minutos, Preferencias.MinutosBloqueio)),
                indice => _acoes.DefinirBloqueioAutomatico(minutos[indice])));

            int[] segundos = { 0, 15, 30, 60 };
            string[] rotulosClipboard =
            {
                Idioma.Texto("Settings.Disabled"),
                Idioma.Texto("Settings.ClipboardClear.After15"),
                Idioma.Texto("Settings.ClipboardClear.After30"),
                Idioma.Texto("Settings.ClipboardClear.After60")
            };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Settings.ClipboardClear"), null, rotulosClipboard,
                Math.Max(0, Array.IndexOf(segundos, Preferencias.SegundosLimpezaClipboard)),
                indice =>
                {
                    Preferencias.SegundosLimpezaClipboard = segundos[indice];
                    Preferencias.Salvar();
                }));

            painel.Children.Add(LinhaCheck(
                Idioma.Texto("Settings.TrackUsageHistory"), Idioma.Texto("Settings.TrackUsageHistoryTooltip"),
                Preferencias.RegistrarHistoricoUso,
                valor =>
                {
                    Preferencias.RegistrarHistoricoUso = valor;
                    Preferencias.Salvar();
                }));

            painel.Children.Add(LinhaCheck(
                Idioma.Texto("Settings.CheckUpdates"), Idioma.Texto("Settings.CheckUpdatesTooltip"),
                Preferencias.VerificarAtualizacoes,
                valor => _acoes.DefinirVerificarAtualizacoes(valor)));

            painel.Children.Add(LinhaCheck(
                Idioma.Texto("Settings.OnlineIcons"), Idioma.Texto("Settings.OnlineIconsTooltip"),
                Preferencias.IconesOnline,
                IconesOnlineMudou));

            return painel;
        }

        private Control ConstruirSeguranca()
        {
            var painel = NovoPainel("Config.Security.Intro");
            painel.Children.Add(LinhaAcao("IconeCofre", Idioma.Texto("Settings.ChangeMasterPassword"), null, _acoes.AlterarSenhaMestra));
            painel.Children.Add(LinhaAcao("IconeQrCode", Idioma.Texto("Settings.RegenerateQr"), null, _acoes.RegerarQr));
            painel.Children.Add(LinhaAcao("IconeCadeado", Idioma.Texto("Recovery.SettingsRow"),
                Idioma.Texto(_acoes.ChaveRecuperacaoAtiva ? "Recovery.SettingsHelpOn" : "Recovery.SettingsHelpOff"),
                _acoes.ChaveRecuperacaoAtivarOuGerar));
            if (_acoes.ChaveRecuperacaoAtiva)
                painel.Children.Add(LinhaAcao("IconeAviso", Idioma.Texto("Recovery.Disable"), null, _acoes.ChaveRecuperacaoDesativar, perigoso: true));
            if (_acoes.WindowsHelloSuportado)
                painel.Children.Add(LinhaAcao("IconeWindowsHello",
                    Idioma.Texto(_acoes.WindowsHelloAtivo ? "Settings.DisableWindowsHello" : "Settings.EnableWindowsHello"),
                    null, _acoes.AlternarWindowsHello));
            painel.Children.Add(LinhaAcao("IconeBloquearAgora", Idioma.Texto("Settings.LockNow"), null, _acoes.BloquearAgora));
            return painel;
        }

        private Control ConstruirBackupSync()
        {
            var painel = NovoPainel("Config.BackupSync.Intro");
            painel.Children.Add(LinhaAcao("IconeExportar", Idioma.Texto("Settings.Backup"), null, _acoes.Backup));
            painel.Children.Add(LinhaAcao("IconeSincronizacao", Idioma.Texto("Settings.Sync"), null, _acoes.Sincronizacao));
            painel.Children.Add(LinhaAcao("IconeImportarCsv", Idioma.Texto("Settings.ImportCsv"), null, _acoes.ImportarCsv));
            painel.Children.Add(LinhaAcao("IconeBanco", Idioma.Texto("Settings.ConnectDatabase"),
                Idioma.Texto("Settings.ConnectDatabaseTooltip"), _acoes.ConectarBanco));
            if (_acoes.BancoConectado)
                painel.Children.Add(LinhaAcao("IconeBancoDesconectado", Idioma.Texto("Settings.DisconnectDatabase"), null, _acoes.DesconectarBanco));
            return painel;
        }

        private Control ConstruirAcessibilidade()
        {
            var painel = NovoPainel("Config.Accessibility.Intro");

            Secao(painel, "Access.Section.Color");

            string[] tiposDaltonismo = { "Nenhum", "Protanopia", "Deuteranopia", "Tritanopia", "Monocromacia" };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Access.Colorblind"), null,
                new[]
                {
                    Idioma.Texto("Access.None"), Idioma.Texto("Access.Protanopia"), Idioma.Texto("Access.Deuteranopia"),
                    Idioma.Texto("Access.Tritanopia"), Idioma.Texto("Access.Monochromacy")
                },
                Math.Max(0, Array.IndexOf(tiposDaltonismo, Acessibilidade.Daltonismo.ToString())),
                indice => Acessibilidade.SelecionarDaltonismo(tiposDaltonismo[indice])));

            string[] contrastes = { "Automatico", "Padrao", "Medio", "Alto" };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Access.Contrast"), null,
                new[]
                {
                    Idioma.Texto("Access.Contrast.Auto"), Idioma.Texto("Access.Contrast.Standard"),
                    Idioma.Texto("Access.Contrast.Medium"), Idioma.Texto("Access.Contrast.High")
                },
                Math.Max(0, Array.IndexOf(contrastes, Acessibilidade.Contraste.ToString())),
                indice => Acessibilidade.SelecionarContraste(contrastes[indice])));

            painel.Children.Add(LinhaCheck(Idioma.Texto("Access.FocusRing"), null,
                Acessibilidade.FocoReforcado, Acessibilidade.SelecionarFocoReforcado));

            Secao(painel, "Access.Section.Text");

            var escalas = Acessibilidade.EscalasDisponiveis;
            var rotulosEscala = new string[escalas.Length + 1];
            rotulosEscala[0] = Idioma.Texto("Access.TextAuto");
            for (int i = 0; i < escalas.Length; i++)
                rotulosEscala[i + 1] = ((int)Math.Round(escalas[i] * 100)) + "%";
            int selEscala = 0;
            if (!Acessibilidade.EscalaAutomatica)
                for (int i = 0; i < escalas.Length; i++)
                    if (Math.Abs(escalas[i] - Acessibilidade.Escala) < 0.001)
                        selEscala = i + 1;
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Access.TextSize"), null, rotulosEscala, selEscala,
                indice => Acessibilidade.SelecionarEscala(indice == 0
                    ? "auto"
                    : escalas[indice - 1].ToString(CultureInfo.InvariantCulture))));

            string[] fontes = { "Padrao", "Legivel", "Serifada", "Monoespacada" };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Access.Font"), Idioma.Texto("Access.Font.Desc"),
                new[]
                {
                    Idioma.Texto("Access.Font.Default"), Idioma.Texto("Access.Font.Readable"),
                    Idioma.Texto("Access.Font.Serif"), Idioma.Texto("Access.Font.Mono")
                },
                Math.Max(0, Array.IndexOf(fontes, Acessibilidade.Fonte.ToString())),
                indice => Acessibilidade.SelecionarFonte(fontes[indice])));

            string[] espacamentos = { "Normal", "Medio", "Amplo" };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Access.LetterSpacing"), null,
                new[]
                {
                    Idioma.Texto("Access.LetterSpacing.Normal"), Idioma.Texto("Access.LetterSpacing.Medium"),
                    Idioma.Texto("Access.LetterSpacing.Wide")
                },
                Math.Max(0, Array.IndexOf(espacamentos, Acessibilidade.Espacamento.ToString())),
                indice => Acessibilidade.SelecionarEspacamento(espacamentos[indice])));

            painel.Children.Add(LinhaCheck(Idioma.Texto("Access.UnderlineLinks"), null,
                Acessibilidade.SublinharLinks, Acessibilidade.SelecionarSublinharLinks));

            Secao(painel, "Access.Section.Motion");

            string[] movimentos = { "Automatico", "Completo", "Reduzido", "SemAnimacao" };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Access.Motion"), Idioma.Texto("Access.Motion.Desc"),
                new[]
                {
                    Idioma.Texto("Access.Motion.Auto"), Idioma.Texto("Access.Motion.Full"),
                    Idioma.Texto("Access.Motion.Reduced"), Idioma.Texto("Access.Motion.None")
                },
                Math.Max(0, Array.IndexOf(movimentos, Acessibilidade.Movimento.ToString())),
                indice => Acessibilidade.SelecionarMovimento(movimentos[indice])));

            painel.Children.Add(LinhaCheck(Idioma.Texto("Access.WarnBeforeLock"), Idioma.Texto("Access.WarnBeforeLock.Desc"),
                Acessibilidade.AvisarAntesBloqueio, Acessibilidade.SelecionarAvisarAntesBloqueio));

            Secao(painel, "Access.Section.Reader");

            painel.Children.Add(LinhaCheck(Idioma.Texto("Access.ScreenReader"), Idioma.Texto("Access.ScreenReaderHelp"),
                Acessibilidade.LeitorTela, Acessibilidade.SelecionarLeitorTela));

            string[] verbosidades = { "Discreta", "Normal", "Detalhada" };
            painel.Children.Add(LinhaCombo(
                Idioma.Texto("Access.Verbosity"), null,
                new[]
                {
                    Idioma.Texto("Access.Verbosity.Low"), Idioma.Texto("Access.Verbosity.Normal"),
                    Idioma.Texto("Access.Verbosity.High")
                },
                Math.Max(0, Array.IndexOf(verbosidades, Acessibilidade.Verbosidade.ToString())),
                indice => Acessibilidade.SelecionarVerbosidade(verbosidades[indice])));

            painel.Children.Add(LinhaCheck(Idioma.Texto("Access.AnnounceActions"), Idioma.Texto("Access.AnnounceActions.Desc"),
                Acessibilidade.AnunciarAcoes, Acessibilidade.SelecionarAnunciarAcoes));

            if (Acessibilidade.AnunciarAcoes || Acessibilidade.LeitorTela)
                painel.Children.Add(LinhaVozNeural());

            return painel;
        }

        private Control LinhaVozNeural()
        {
            var codigo = GerenciadorVozes.Curto(Idioma.Atual.Codigo);
            var nomeIdioma = Idioma.Atual.NomeNativo;
            var pilha = new StackPanel { Spacing = 8 };
            var texto = new TextBlock
            {
                Foreground = Tema.Pincel(Tema.TextSecondary),
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap
            };
            pilha.Children.Add(texto);

            if (!GerenciadorVozes.Suportado(codigo))
            {
                texto.Text = Idioma.Formatar("Access.Voice.Unsupported", nomeIdioma);
                return Envelope(pilha, Tema.TextTertiary);
            }

            if (GerenciadorVozes.Instalada(codigo))
            {
                texto.Text = Idioma.Formatar("Access.Voice.Ready", nomeIdioma);
                return Envelope(pilha, Tema.StrengthStrong);
            }

            texto.Text = _erroVoz
                ? Idioma.Texto("Access.Voice.Failed")
                : Idioma.Formatar("Access.Voice.NeuralMissing", nomeIdioma, GerenciadorVozes.TamanhoMb(codigo));

            var botao = new Button { Height = 32, FontSize = 12.5, HorizontalAlignment = HorizontalAlignment.Left };
            botao.Classes.Add("secundario");

            if (_baixandoVoz != null)
            {
                botao.IsEnabled = false;
                botao.Content = Idioma.Formatar("Access.Voice.Downloading", (int)Math.Round(_progressoVoz * 100));
            }
            else
            {
                botao.Content = Idioma.Texto("Access.Voice.Download");
                botao.Click += (s, e) => BaixarVoz(codigo);
            }

            pilha.Children.Add(botao);
            return Envelope(pilha, Tema.StrengthMedium);
        }

        private Border Envelope(Control conteudo, Color cor) => new()
        {
            Padding = new Thickness(16, 14),
            CornerRadius = new CornerRadius(12),
            Background = Tema.Pincel(Tema.CardBackground),
            BorderBrush = Tema.Pincel(cor),
            BorderThickness = new Thickness(1),
            Child = conteudo
        };

        private async void BaixarVoz(string codigo)
        {
            if (_baixandoVoz != null)
                return;

            _erroVoz = false;
            _progressoVoz = 0;
            var ultimo = -1;
            var progresso = new Progress<double>(p =>
            {
                _progressoVoz = p;
                var pct = (int)Math.Round(p * 100);
                if (pct != ultimo && !_montando)
                {
                    ultimo = pct;
                    Construir();
                }
            });

            _baixandoVoz = GerenciadorVozes.BaixarAsync(codigo, progresso, CancellationToken.None);
            Construir();

            try
            {
                await _baixandoVoz;
                Locucao.RedefinirVoz();
            }
            catch
            {
                _erroVoz = true;
            }
            finally
            {
                _baixandoVoz = null;
                Construir();
            }
        }

        private Control ConstruirAjuda()
        {
            var painel = NovoPainel("Config.Help.Intro");
            painel.Children.Add(LinhaAcao("IconeInfo", Idioma.Texto("Ajuda.Abrir"), null, _acoes.AbrirManual));
            painel.Children.Add(LinhaAcao("IconeAtalhoTeclado", Idioma.Texto("Settings.KeyboardShortcuts"), null, _acoes.AtalhosTeclado));
            return painel;
        }

        private Control ConstruirZonaPerigo()
        {
            var painel = NovoPainel("Config.Danger.Intro");
            painel.Children.Add(LinhaAcao("IconeExcluir", Idioma.Texto("Settings.ClearVault"), null, _acoes.LimparCofre, perigoso: true));
            painel.Children.Add(LinhaAcao("IconeAviso", Idioma.Texto("Settings.DeleteVault"), null, _acoes.ExcluirCofre, perigoso: true));
            return painel;
        }

        private async void IconesOnlineMudou(bool ligado)
        {
            if (_montando)
                return;

            if (ligado)
            {
                var aceitou = await CaixaMensagem.ConfirmarAsync(this,
                    Idioma.Texto("Icons.ConsentMessage"),
                    Idioma.Texto("Settings.OnlineIcons"),
                    TipoMensagem.Info);
                if (!aceitou)
                {
                    Construir();
                    return;
                }
            }

            _acoes.DefinirIconesOnline(ligado);
        }

        private Control LinhaCombo(string rotulo, string? descricao, string[] opcoes, int selecionado, Action<int> aoSelecionar)
        {
            var combo = new ComboBox
            {
                ItemsSource = opcoes,
                SelectedIndex = selecionado,
                MinWidth = 230,
                VerticalAlignment = VerticalAlignment.Center
            };
            combo.Classes.Add("campo");
            combo.SelectionChanged += (s, e) =>
            {
                if (_montando || combo.SelectedIndex < 0)
                    return;
                aoSelecionar(combo.SelectedIndex);
            };
            return Linha(rotulo, descricao, combo);
        }

        private Control LinhaCheck(string rotulo, string? descricao, bool valor, Action<bool> aoMudar)
        {
            var chk = new CheckBox { IsChecked = valor, VerticalAlignment = VerticalAlignment.Center };
            chk.IsCheckedChanged += (s, e) =>
            {
                if (_montando)
                    return;
                aoMudar(chk.IsChecked == true);
            };
            return Linha(rotulo, descricao, chk);
        }

        private Control LinhaAcao(string iconeChave, string rotulo, string? descricao, Action aoClicar, bool perigoso = false)
        {
            var grade = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,14,*") };
            var icone = new Icone
            {
                Chave = iconeChave,
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (perigoso)
                icone.Stroke = Tema.Pincel(Tema.StrengthWeak);
            grade.Children.Add(icone);
            var textos = TextosLinha(rotulo, descricao);
            if (perigoso && textos.Children[0] is TextBlock titulo)
                titulo.Foreground = Tema.Pincel(Tema.StrengthWeak);
            Grid.SetColumn(textos, 2);
            grade.Children.Add(textos);

            var botao = new Button
            {
                Content = grade,
                Padding = new Thickness(16, 14),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            botao.Classes.Add("cartao");
            AutomationProperties.SetName(botao, rotulo);
            botao.Click += (s, e) =>
            {
                AcaoPendente = aoClicar;
                Close(false);
            };
            return botao;
        }

        private Border Linha(string rotulo, string? descricao, Control controle)
        {
            var grade = new Grid { ColumnDefinitions = new ColumnDefinitions("*,16,Auto") };
            grade.Children.Add(TextosLinha(rotulo, descricao));
            Grid.SetColumn(controle, 2);
            grade.Children.Add(controle);

            return new Border
            {
                Padding = new Thickness(16, 14),
                CornerRadius = new CornerRadius(12),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Child = grade
            };
        }

        private static StackPanel TextosLinha(string rotulo, string? descricao)
        {
            var textos = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
            textos.Children.Add(new TextBlock
            {
                Text = rotulo,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                FontSize = 13.5,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrEmpty(descricao))
                textos.Children.Add(new TextBlock
                {
                    Text = descricao,
                    Foreground = Tema.Pincel(Tema.TextSecondary),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
            return textos;
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
