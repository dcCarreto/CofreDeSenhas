using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas
{
    public enum TipoDaltonismo
    {
        Nenhum,
        Protanopia,
        Deuteranopia,
        Tritanopia,
        Monocromacia
    }

    internal enum CorVisual
    {
        WorkspaceBackground,
        CardBackground,
        CardBorder,
        TitleBar,
        TitleBarBorder,
        InputBackground,
        InputBorder,
        RowHover,
        Separator,
        Footer,
        AccentPrimary,
        AccentHover,
        AccentLight,
        AccentText,
        TextPrimary,
        TextSecondary,
        TextTertiary,
        TextHeader,
        TrailInactive,
        ToggleOff,
        HoverBackground,
        IconHoverBackground,
        FavoriteColor,
        FavoriteBorderColor,
        StrengthWeak,
        StrengthMedium,
        StrengthStrong,
        StrengthExcellent,
        CloseButtonHover,
        CloseButtonPressed,
        StatusLocal,
        StatusWarning,
        StatusConnected
    }

    public static class Acessibilidade
    {
        public const double EscalaNormal = 1.0;
        public const double EscalaGrande = 1.15;
        public const double EscalaMaior = 1.30;

        public static TipoDaltonismo Daltonismo { get; private set; } = TipoDaltonismo.Nenhum;
        public static bool AltoContraste { get; private set; }
        public static double Escala { get; private set; } = EscalaNormal;
        public static bool ReduzirAnimacoes { get; private set; }
        public static bool LeitorTela { get; private set; }

        public static event EventHandler? Alterado;

        private static readonly ConditionalWeakTable<Window, EscalaJanela> _escalas = new();
        private static readonly ConditionalWeakTable<Window, TextBlock> _anunciadores = new();

        private static readonly (string Chave, CorVisual Cor)[] RecursosTema =
        {
            ("WorkspaceBackground", CorVisual.WorkspaceBackground),
            ("CardBackground", CorVisual.CardBackground),
            ("CardBorder", CorVisual.CardBorder),
            ("TitleBar", CorVisual.TitleBar),
            ("TitleBarBorder", CorVisual.TitleBarBorder),
            ("InputBackground", CorVisual.InputBackground),
            ("InputBorder", CorVisual.InputBorder),
            ("RowHover", CorVisual.RowHover),
            ("Separator", CorVisual.Separator),
            ("Footer", CorVisual.Footer),
            ("AccentPrimary", CorVisual.AccentPrimary),
            ("AccentHover", CorVisual.AccentHover),
            ("AccentLight", CorVisual.AccentLight),
            ("AccentText", CorVisual.AccentText),
            ("TextPrimary", CorVisual.TextPrimary),
            ("TextSecondary", CorVisual.TextSecondary),
            ("TextTertiary", CorVisual.TextTertiary),
            ("TextHeader", CorVisual.TextHeader),
            ("TrailInactive", CorVisual.TrailInactive),
            ("ToggleOff", CorVisual.ToggleOff),
            ("HoverBackground", CorVisual.HoverBackground),
            ("IconHoverBackground", CorVisual.IconHoverBackground),
            ("FavoriteColor", CorVisual.FavoriteColor),
            ("FavoriteBorderColor", CorVisual.FavoriteBorderColor),
            ("StrengthWeak", CorVisual.StrengthWeak),
            ("StrengthMedium", CorVisual.StrengthMedium),
            ("StrengthStrong", CorVisual.StrengthStrong),
            ("StrengthExcellent", CorVisual.StrengthExcellent),
            ("CloseButtonHover", CorVisual.CloseButtonHover),
            ("CloseButtonPressed", CorVisual.CloseButtonPressed),
            ("StatusLocal", CorVisual.StatusLocal),
            ("StatusWarning", CorVisual.StatusWarning),
            ("StatusConnected", CorVisual.StatusConnected)
        };

        private static readonly IReadOnlyDictionary<CorVisual, uint> PadraoEscuro = D(
            (CorVisual.WorkspaceBackground, 0xFF17130F),
            (CorVisual.CardBackground, 0xFF241C15),
            (CorVisual.CardBorder, 0xFF3D3120),
            (CorVisual.TitleBar, 0xFF1C1712),
            (CorVisual.TitleBarBorder, 0xFF362B1C),
            (CorVisual.InputBackground, 0xFF2B2116),
            (CorVisual.InputBorder, 0xFF46392A),
            (CorVisual.RowHover, 0xFF2A2117),
            (CorVisual.Separator, 0xFF33291A),
            (CorVisual.Footer, 0xFF1A1510),
            (CorVisual.AccentPrimary, 0xFFC8A24C),
            (CorVisual.AccentHover, 0xFFA8853D),
            (CorVisual.AccentLight, 0xFF3A2E1C),
            (CorVisual.AccentText, 0xFFE4C374),
            (CorVisual.TextPrimary, 0xFFF3EADC),
            (CorVisual.TextSecondary, 0xFFB7A791),
            (CorVisual.TextTertiary, 0xFF8B7C68),
            (CorVisual.TextHeader, 0xFF8B7C68),
            (CorVisual.TrailInactive, 0xFF3C3225),
            (CorVisual.ToggleOff, 0xFF4A3D2C),
            (CorVisual.HoverBackground, 0xFF2C2319),
            (CorVisual.IconHoverBackground, 0xFF332818),
            (CorVisual.FavoriteColor, 0xFFE8A23D),
            (CorVisual.FavoriteBorderColor, 0xFF5C4F3D),
            (CorVisual.StrengthWeak, 0xFFC96B58),
            (CorVisual.StrengthMedium, 0xFFC98A3E),
            (CorVisual.StrengthStrong, 0xFF7E9B6C),
            (CorVisual.StrengthExcellent, 0xFF6FA0C9),
            (CorVisual.CloseButtonHover, 0xFFE5484D),
            (CorVisual.CloseButtonPressed, 0xFFC93A3E),
            (CorVisual.StatusLocal, 0xFF7E9B6C),
            (CorVisual.StatusWarning, 0xFFC98A3E),
            (CorVisual.StatusConnected, 0xFF6FA0C9));

        private static readonly IReadOnlyDictionary<CorVisual, uint> ProtanopiaEscuro = D(
            (CorVisual.WorkspaceBackground, 0xFF0F151A),
            (CorVisual.CardBackground, 0xFF1B252C),
            (CorVisual.CardBorder, 0xFF33434D),
            (CorVisual.TitleBar, 0xFF141D23),
            (CorVisual.TitleBarBorder, 0xFF31414B),
            (CorVisual.InputBackground, 0xFF24313A),
            (CorVisual.InputBorder, 0xFF3E515D),
            (CorVisual.RowHover, 0xFF24323C),
            (CorVisual.Separator, 0xFF31414B),
            (CorVisual.Footer, 0xFF172127),
            (CorVisual.AccentPrimary, 0xFF56B4E9),
            (CorVisual.AccentHover, 0xFF0072B2),
            (CorVisual.AccentLight, 0xFF193B4E),
            (CorVisual.TextPrimary, 0xFFF2F7FA),
            (CorVisual.TextSecondary, 0xFFA9B7C1),
            (CorVisual.TextTertiary, 0xFF8D9DA9),
            (CorVisual.TextHeader, 0xFF90A2AE),
            (CorVisual.TrailInactive, 0xFF3A4C57),
            (CorVisual.ToggleOff, 0xFF465B68),
            (CorVisual.HoverBackground, 0xFF2A3A44),
            (CorVisual.IconHoverBackground, 0xFF304552),
            (CorVisual.FavoriteColor, 0xFFE69F00),
            (CorVisual.FavoriteBorderColor, 0xFF8EA1AD),
            (CorVisual.StrengthWeak, 0xFFD55E00),
            (CorVisual.StrengthMedium, 0xFFE69F00),
            (CorVisual.StrengthStrong, 0xFF009E73),
            (CorVisual.StrengthExcellent, 0xFF56B4E9),
            (CorVisual.CloseButtonHover, 0xFFD55E00),
            (CorVisual.CloseButtonPressed, 0xFFA64200),
            (CorVisual.StatusLocal, 0xFF009E73),
            (CorVisual.StatusWarning, 0xFFE69F00),
            (CorVisual.StatusConnected, 0xFF56B4E9));

        private static readonly IReadOnlyDictionary<CorVisual, uint> DeuteranopiaEscuro = D(
            (CorVisual.WorkspaceBackground, 0xFF10151D),
            (CorVisual.CardBackground, 0xFF1B2430),
            (CorVisual.CardBorder, 0xFF334257),
            (CorVisual.TitleBar, 0xFF151D27),
            (CorVisual.TitleBarBorder, 0xFF314055),
            (CorVisual.InputBackground, 0xFF242F3D),
            (CorVisual.InputBorder, 0xFF3E5067),
            (CorVisual.RowHover, 0xFF253140),
            (CorVisual.Separator, 0xFF314055),
            (CorVisual.Footer, 0xFF171F2A),
            (CorVisual.AccentPrimary, 0xFF56B4E9),
            (CorVisual.AccentHover, 0xFF0072B2),
            (CorVisual.AccentLight, 0xFF193A55),
            (CorVisual.TextPrimary, 0xFFF2F6FB),
            (CorVisual.TextSecondary, 0xFFAAB6C5),
            (CorVisual.TextTertiary, 0xFF8F9CAB),
            (CorVisual.TextHeader, 0xFF94A2B2),
            (CorVisual.TrailInactive, 0xFF3B4B60),
            (CorVisual.ToggleOff, 0xFF46586F),
            (CorVisual.HoverBackground, 0xFF2B394A),
            (CorVisual.IconHoverBackground, 0xFF30445C),
            (CorVisual.FavoriteColor, 0xFFE69F00),
            (CorVisual.FavoriteBorderColor, 0xFF91A0B0),
            (CorVisual.StrengthWeak, 0xFFD55E00),
            (CorVisual.StrengthMedium, 0xFFE69F00),
            (CorVisual.StrengthStrong, 0xFF56B4E9),
            (CorVisual.StrengthExcellent, 0xFFCC79A7),
            (CorVisual.CloseButtonHover, 0xFFD55E00),
            (CorVisual.CloseButtonPressed, 0xFFA64200),
            (CorVisual.StatusLocal, 0xFF56B4E9),
            (CorVisual.StatusWarning, 0xFFE69F00),
            (CorVisual.StatusConnected, 0xFF56B4E9));

        private static readonly IReadOnlyDictionary<CorVisual, uint> TritanopiaEscuro = D(
            (CorVisual.WorkspaceBackground, 0xFF171118),
            (CorVisual.CardBackground, 0xFF271E28),
            (CorVisual.CardBorder, 0xFF463547),
            (CorVisual.TitleBar, 0xFF201820),
            (CorVisual.TitleBarBorder, 0xFF433244),
            (CorVisual.InputBackground, 0xFF312634),
            (CorVisual.InputBorder, 0xFF533F55),
            (CorVisual.RowHover, 0xFF342737),
            (CorVisual.Separator, 0xFF433244),
            (CorVisual.Footer, 0xFF211821),
            (CorVisual.AccentPrimary, 0xFFF06292),
            (CorVisual.AccentHover, 0xFFC2185B),
            (CorVisual.AccentLight, 0xFF522236),
            (CorVisual.TextPrimary, 0xFFF7F0F6),
            (CorVisual.TextSecondary, 0xFFC2AFC0),
            (CorVisual.TextTertiary, 0xFFA68FA3),
            (CorVisual.TextHeader, 0xFFAD96AA),
            (CorVisual.TrailInactive, 0xFF4E3C50),
            (CorVisual.ToggleOff, 0xFF5D4860),
            (CorVisual.HoverBackground, 0xFF3C2D3E),
            (CorVisual.IconHoverBackground, 0xFF4B354E),
            (CorVisual.FavoriteColor, 0xFF81C784),
            (CorVisual.FavoriteBorderColor, 0xFFB59FB3),
            (CorVisual.StrengthWeak, 0xFFE57373),
            (CorVisual.StrengthMedium, 0xFFE67E22),
            (CorVisual.StrengthStrong, 0xFF81C784),
            (CorVisual.StrengthExcellent, 0xFFCE93D8),
            (CorVisual.CloseButtonHover, 0xFFD32F2F),
            (CorVisual.CloseButtonPressed, 0xFFA32121),
            (CorVisual.StatusLocal, 0xFF81C784),
            (CorVisual.StatusWarning, 0xFFE67E22),
            (CorVisual.StatusConnected, 0xFFF06292));

        private static readonly IReadOnlyDictionary<CorVisual, uint> MonocromaciaEscuro = D(
            (CorVisual.WorkspaceBackground, 0xFF101214),
            (CorVisual.CardBackground, 0xFF20242A),
            (CorVisual.CardBorder, 0xFF3B424C),
            (CorVisual.TitleBar, 0xFF181B20),
            (CorVisual.TitleBarBorder, 0xFF353B44),
            (CorVisual.InputBackground, 0xFF2A2F36),
            (CorVisual.InputBorder, 0xFF464D58),
            (CorVisual.RowHover, 0xFF2F343C),
            (CorVisual.Separator, 0xFF353B44),
            (CorVisual.Footer, 0xFF191D22),
            (CorVisual.AccentPrimary, 0xFFD1D5DB),
            (CorVisual.AccentHover, 0xFFE5E7EB),
            (CorVisual.AccentLight, 0xFF3F4650),
            (CorVisual.TextPrimary, 0xFFF3F4F6),
            (CorVisual.TextSecondary, 0xFFB8C0CC),
            (CorVisual.TextTertiary, 0xFF959EAA),
            (CorVisual.TextHeader, 0xFFA1AAB5),
            (CorVisual.TrailInactive, 0xFF454C56),
            (CorVisual.ToggleOff, 0xFF555E69),
            (CorVisual.HoverBackground, 0xFF343A43),
            (CorVisual.IconHoverBackground, 0xFF414852),
            (CorVisual.FavoriteColor, 0xFFE5E7EB),
            (CorVisual.FavoriteBorderColor, 0xFF9CA3AF),
            (CorVisual.StrengthWeak, 0xFFE5E7EB),
            (CorVisual.StrengthMedium, 0xFFC6CBD3),
            (CorVisual.StrengthStrong, 0xFFA5ADBA),
            (CorVisual.StrengthExcellent, 0xFF7C8796),
            (CorVisual.CloseButtonHover, 0xFFE5E7EB),
            (CorVisual.CloseButtonPressed, 0xFFFFFFFF),
            (CorVisual.StatusLocal, 0xFFA5ADBA),
            (CorVisual.StatusWarning, 0xFFC6CBD3),
            (CorVisual.StatusConnected, 0xFFE5E7EB));

        private static readonly IReadOnlyDictionary<Categoria, (uint Bg, uint Fg)> CategoriasPadrao = DCat(
            (Categoria.Personal, 0xFFFBEDE0, 0xFFA0551C),
            (Categoria.Work, 0xFFE7E9F7, 0xFF3B4FA0),
            (Categoria.Finance, 0xFFE1F5E9, 0xFF1E7A4C),
            (Categoria.Social, 0xFFFBE7EE, 0xFFB23C68),
            (Categoria.Other, 0xFFEDEEF1, 0xFF5B5F6B));

        private static readonly IReadOnlyDictionary<Categoria, (uint Bg, uint Fg)> CategoriasVermelhoVerde = DCat(
            (Categoria.Personal, 0xFFE4F2FA, 0xFF0072B2),
            (Categoria.Work, 0xFFF0ECFA, 0xFF6A4C93),
            (Categoria.Finance, 0xFFE6F4EF, 0xFF009E73),
            (Categoria.Social, 0xFFFAEAF3, 0xFFCC79A7),
            (Categoria.Other, 0xFFFFF1D6, 0xFFD55E00));

        private static readonly IReadOnlyDictionary<Categoria, (uint Bg, uint Fg)> CategoriasTritanopia = DCat(
            (Categoria.Personal, 0xFFFCE8F1, 0xFFC2185B),
            (Categoria.Work, 0xFFF3E9F6, 0xFF6A1B9A),
            (Categoria.Finance, 0xFFEAF6EA, 0xFF2E7D32),
            (Categoria.Social, 0xFFFDECEC, 0xFFD32F2F),
            (Categoria.Other, 0xFFFFF1E2, 0xFFE67E22));

        private static readonly IReadOnlyDictionary<Categoria, (uint Bg, uint Fg)> CategoriasMonocromacia = DCat(
            (Categoria.Personal, 0xFFECEFF3, 0xFF334155),
            (Categoria.Work, 0xFFE5E7EB, 0xFF1F2937),
            (Categoria.Finance, 0xFFF3F4F6, 0xFF4B5563),
            (Categoria.Social, 0xFFDDE2EA, 0xFF374151),
            (Categoria.Other, 0xFFF8FAFC, 0xFF6B7280));

        private static readonly uint[] AvataresPadrao =
        {
            0xFF3B4FA0, 0xFFA0551C, 0xFF1E7A4C, 0xFFB23C68, 0xFF5B5F6B
        };

        private static readonly uint[] AvataresVermelhoVerde =
        {
            0xFF0072B2, 0xFF56B4E9, 0xFFE69F00, 0xFFD55E00,
            0xFFCC79A7, 0xFF6A4C93, 0xFF009E73, 0xFF4D4D4D
        };

        private static readonly uint[] AvataresTritanopia =
        {
            0xFFC2185B, 0xFF2E7D32, 0xFFD32F2F, 0xFFE67E22,
            0xFF6A1B9A, 0xFF00897B, 0xFF8E24AA, 0xFF4D4D4D
        };

        private static readonly uint[] AvataresMonocromacia =
        {
            0xFF111827, 0xFF374151, 0xFF4B5563, 0xFF6B7280,
            0xFF7C8796, 0xFF9CA3AF, 0xFF2F3640, 0xFF5B6470
        };

        private sealed class EscalaJanela
        {
            public double Valor = EscalaNormal;
        }

        public static void Hidratar(TipoDaltonismo daltonismo, bool altoContraste, double escala, bool reduzirAnimacoes,
            bool leitorTela)
        {
            Daltonismo = daltonismo;
            AltoContraste = altoContraste;
            Escala = NormalizarEscala(escala);
            ReduzirAnimacoes = reduzirAnimacoes;
            LeitorTela = leitorTela;
        }

        public static void DefinirDaltonismo(TipoDaltonismo tipo)
        {
            if (Daltonismo == tipo)
                return;

            Daltonismo = tipo;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirAltoContraste(bool ligado)
        {
            if (AltoContraste == ligado)
                return;

            AltoContraste = ligado;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirEscala(double escala)
        {
            var nova = NormalizarEscala(escala);
            if (Escala == nova)
                return;

            Escala = nova;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirReducaoMovimento(bool ligado)
        {
            if (ReduzirAnimacoes == ligado)
                return;

            ReduzirAnimacoes = ligado;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirLeitorTela(bool ligado)
        {
            if (LeitorTela == ligado)
                return;

            LeitorTela = ligado;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void SelecionarDaltonismo(string? tag)
        {
            if (!Enum.TryParse<TipoDaltonismo>(tag, out var tipo))
                return;

            DefinirDaltonismo(tipo);
            Preferencias.Daltonismo = tipo.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarEscala(string? tag)
        {
            if (!double.TryParse(tag, NumberStyles.Any, CultureInfo.InvariantCulture, out var escala))
                return;

            DefinirEscala(escala);
            Preferencias.EscalaInterface = Escala;
            Preferencias.Salvar();
        }

        public static void SelecionarAltoContraste(bool ligado)
        {
            DefinirAltoContraste(ligado);
            Preferencias.AltoContraste = AltoContraste;
            Preferencias.Salvar();
        }

        public static void SelecionarReducaoMovimento(bool ligado)
        {
            DefinirReducaoMovimento(ligado);
            Preferencias.ReduzirAnimacoes = ReduzirAnimacoes;
            Preferencias.Salvar();
        }

        public static void SelecionarLeitorTela(bool ligado)
        {
            DefinirLeitorTela(ligado);
            Preferencias.LeitorTela = LeitorTela;
            Preferencias.Salvar();
        }

        public static void TratarClickDaltonismo(object? sender)
        {
            if (sender is MenuItem item)
                SelecionarDaltonismo(item.Tag as string);
        }

        public static void TratarClickEscala(object? sender)
        {
            if (sender is MenuItem item)
                SelecionarEscala(item.Tag as string);
        }

        public static void TratarClickAltoContraste(object? sender)
        {
            if (sender is MenuItem item)
                SelecionarAltoContraste(item.IsChecked);
        }

        public static void TratarClickReducaoMovimento(object? sender)
        {
            if (sender is MenuItem item)
                SelecionarReducaoMovimento(item.IsChecked);
        }

        public static void TratarClickLeitorTela(Control janela, object? sender)
        {
            if (sender is not MenuItem item)
                return;

            SelecionarLeitorTela(item.IsChecked);
            Anunciar(janela, Idioma.Texto(LeitorTela ? "A11y.ScreenReaderEnabled" : "A11y.ScreenReaderDisabled"),
                assertivo: true, forcar: true);
        }

        public static void MarcarMenus(MenuItem? daltonismo, MenuItem? escala, MenuItem? altoContraste,
            MenuItem? reduzirAnimacoes, MenuItem? leitorTela = null)
        {
            if (daltonismo != null)
                foreach (var item in daltonismo.Items.OfType<MenuItem>())
                    item.IsChecked = item.Tag is string tag &&
                        string.Equals(tag, Daltonismo.ToString(), StringComparison.OrdinalIgnoreCase);

            if (escala != null)
                foreach (var item in escala.Items.OfType<MenuItem>())
                    item.IsChecked = item.Tag is string tag &&
                        double.TryParse(tag, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) &&
                        Math.Abs(valor - Escala) < 0.001;

            if (altoContraste != null)
                altoContraste.IsChecked = AltoContraste;

            if (reduzirAnimacoes != null)
                reduzirAnimacoes.IsChecked = ReduzirAnimacoes;

            if (leitorTela != null)
                leitorTela.IsChecked = LeitorTela;
        }

        public static Color TextoPrincipal() => Color.FromUInt32(0xFFFFFFFF);
        public static Color TextoSecundario() => Color.FromUInt32(0xFFE4E4E9);
        public static Color TextoTerciario() => Color.FromUInt32(0xFFCFCFD6);
        public static Color Borda() => Color.FromUInt32(0xFFFFFFFF);

        internal static Color Cor(CorVisual cor)
        {
            if (AltoContraste && TentarCorAltoContraste(cor, out var altoContraste))
                return altoContraste;

            var valores = ValoresTema();
            if (!valores.TryGetValue(cor, out var argb))
                argb = PadraoEscuro[cor];

            return Color.FromUInt32(argb);
        }

        public static Color CorDecorativa(Color original)
        {
            if (Daltonismo == TipoDaltonismo.Nenhum)
                return original;

            if (Daltonismo == TipoDaltonismo.Monocromacia)
                return ParaEscalaCinza(original);

            var paleta = PaletaAvatar();
            int indice = Math.Abs((original.R * 3) + (original.G * 5) + (original.B * 7)) % paleta.Length;
            var adaptada = Color.FromUInt32(paleta[indice]);
            return Color.FromArgb(original.A, adaptada.R, adaptada.G, adaptada.B);
        }

        public static Color CorAvatarFallback(uint indice)
        {
            var paleta = PaletaAvatar();
            return Color.FromUInt32(paleta[(int)(indice % (uint)paleta.Length)]);
        }

        public static Color CorFrenteParaFundo(Color fundo) =>
            Luminancia(fundo) > 0.58 ? Color.FromUInt32(0xFF111827) : Color.FromUInt32(0xFFFFFFFF);

        public static (Color Bg, Color Fg) CoresCategoria(Categoria categoria)
        {
            var cores = Daltonismo switch
            {
                TipoDaltonismo.Protanopia or TipoDaltonismo.Deuteranopia => CategoriasVermelhoVerde,
                TipoDaltonismo.Tritanopia => CategoriasTritanopia,
                TipoDaltonismo.Monocromacia => CategoriasMonocromacia,
                _ => CategoriasPadrao
            };

            if (!cores.TryGetValue(categoria, out var cor))
                cor = cores[Categoria.Other];

            return (Color.FromUInt32(cor.Bg), Color.FromUInt32(cor.Fg));
        }

        public static void Aplicar()
        {
            var app = Application.Current;
            if (app == null)
                return;

            foreach (var (chave, cor) in RecursosTema)
                app.Resources[chave] = new SolidColorBrush(Cor(cor));
        }

        public static void Vincular(Window janela)
        {
            AutomationProperties.SetName(janela, janela.Title ?? Idioma.Texto("App.Title"));
            AplicarEscala(janela);

            void Handler(object? s, EventArgs e) => AplicarEscala(janela);
            Alterado += Handler;
            janela.Closed += (s, e) => Alterado -= Handler;
        }

        public static void RegistrarAnunciador(Window janela, TextBlock anunciador)
        {
            _anunciadores.Remove(janela);
            _anunciadores.Add(janela, anunciador);

            anunciador.Text = "";
            anunciador.Width = 1;
            anunciador.Height = 1;
            anunciador.Opacity = 0;
            anunciador.IsHitTestVisible = false;
            anunciador.Focusable = false;
            AutomationProperties.SetLiveSetting(anunciador, AutomationLiveSetting.Assertive);
            AutomationProperties.SetName(anunciador, "");

            janela.Closed += (s, e) => _anunciadores.Remove(janela);
        }

        public static void Anunciar(Control origem, string mensagem, bool assertivo = false, bool forcar = false)
        {
            if ((!LeitorTela && !forcar) || string.IsNullOrWhiteSpace(mensagem))
                return;

            if (TopLevel.GetTopLevel(origem) is not Window janela ||
                !_anunciadores.TryGetValue(janela, out var anunciador))
                return;

            Dispatcher.UIThread.Post(() =>
            {
                anunciador.Text = "";
                AutomationProperties.SetName(anunciador, "");
                AutomationProperties.SetLiveSetting(anunciador,
                    assertivo ? AutomationLiveSetting.Assertive : AutomationLiveSetting.Polite);

                Dispatcher.UIThread.Post(() =>
                {
                    anunciador.Text = mensagem;
                    AutomationProperties.SetName(anunciador, mensagem);
                }, DispatcherPriority.Background);
            }, DispatcherPriority.Background);
        }

        private static void AplicarEscala(Window janela)
        {
            var estado = _escalas.GetOrCreateValue(janela);
            double anterior = estado.Valor;
            double alvo = Escala;

            if (janela.Content is LayoutTransformControl atual)
            {
                atual.LayoutTransform = new ScaleTransform(alvo, alvo);
            }
            else if (alvo != EscalaNormal && janela.Content is Control conteudo)
            {
                janela.Content = null;
                janela.Content = new LayoutTransformControl
                {
                    Child = conteudo,
                    LayoutTransform = new ScaleTransform(alvo, alvo)
                };
            }

            double razao = alvo / anterior;

            var escala = janela.RenderScaling > 0 ? janela.RenderScaling : 1.0;
            var areaDisponivel = janela.Screens.ScreenFromWindow(janela)?.WorkingArea;

            if (!double.IsNaN(janela.Width) && janela.Width > 0)
            {
                var larguraAlvo = janela.Width * razao;
                if (areaDisponivel is { } areaW)
                    larguraAlvo = Math.Min(larguraAlvo, areaW.Width / escala * 0.9);
                janela.Width = larguraAlvo;
            }
            if (!double.IsNaN(janela.Height) && janela.Height > 0)
            {
                var alturaAlvo = janela.Height * razao;
                if (areaDisponivel is { } areaH)
                    alturaAlvo = Math.Min(alturaAlvo, areaH.Height / escala * 0.9);
                janela.Height = alturaAlvo;
            }

            if (janela.MinWidth > 0)
            {
                var minWidthAlvo = janela.MinWidth * razao;
                if (areaDisponivel is { } area)
                    minWidthAlvo = Math.Min(minWidthAlvo, area.Width / escala * 0.9);
                janela.MinWidth = minWidthAlvo;
            }
            if (janela.MinHeight > 0)
            {
                var minHeightAlvo = janela.MinHeight * razao;
                if (areaDisponivel is { } area)
                    minHeightAlvo = Math.Min(minHeightAlvo, area.Height / escala * 0.9);
                janela.MinHeight = minHeightAlvo;
            }

            estado.Valor = alvo;
        }

        private static double NormalizarEscala(double escala)
        {
            if (escala >= EscalaMaior)
                return EscalaMaior;
            if (escala >= EscalaGrande)
                return EscalaGrande;
            return EscalaNormal;
        }

        private static IReadOnlyDictionary<CorVisual, uint> ValoresTema() => Daltonismo switch
        {
            TipoDaltonismo.Protanopia => ProtanopiaEscuro,
            TipoDaltonismo.Deuteranopia => DeuteranopiaEscuro,
            TipoDaltonismo.Tritanopia => TritanopiaEscuro,
            TipoDaltonismo.Monocromacia => MonocromaciaEscuro,
            _ => PadraoEscuro
        };

        private static bool TentarCorAltoContraste(CorVisual cor, out Color resultado)
        {
            resultado = default;
            switch (cor)
            {
                case CorVisual.TextPrimary:
                    resultado = TextoPrincipal();
                    return true;
                case CorVisual.TextSecondary:
                    resultado = TextoSecundario();
                    return true;
                case CorVisual.TextTertiary:
                case CorVisual.TextHeader:
                    resultado = TextoTerciario();
                    return true;
                case CorVisual.CardBorder:
                case CorVisual.InputBorder:
                case CorVisual.Separator:
                case CorVisual.TitleBarBorder:
                case CorVisual.FavoriteBorderColor:
                    resultado = Borda();
                    return true;
                default:
                    return false;
            }
        }

        private static uint[] PaletaAvatar() => Daltonismo switch
        {
            TipoDaltonismo.Protanopia or TipoDaltonismo.Deuteranopia => AvataresVermelhoVerde,
            TipoDaltonismo.Tritanopia => AvataresTritanopia,
            TipoDaltonismo.Monocromacia => AvataresMonocromacia,
            _ => AvataresPadrao
        };

        private static Color ParaEscalaCinza(Color original)
        {
            byte cinza = (byte)Math.Clamp(
                (int)Math.Round((original.R * 0.299) + (original.G * 0.587) + (original.B * 0.114)),
                0,
                255);
            return Color.FromArgb(original.A, cinza, cinza, cinza);
        }

        private static double Luminancia(Color cor)
        {
            static double Canal(byte valor)
            {
                double v = valor / 255.0;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }

            return (0.2126 * Canal(cor.R)) + (0.7152 * Canal(cor.G)) + (0.0722 * Canal(cor.B));
        }

        private static IReadOnlyDictionary<CorVisual, uint> D(params (CorVisual Cor, uint Valor)[] entradas) =>
            entradas.ToDictionary(e => e.Cor, e => e.Valor);

        private static IReadOnlyDictionary<Categoria, (uint Bg, uint Fg)> DCat(
            params (Categoria Categoria, uint Bg, uint Fg)[] entradas) =>
            entradas.ToDictionary(e => e.Categoria, e => (e.Bg, e.Fg));
    }
}
