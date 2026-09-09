using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
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

    public enum ModoTema
    {
        Sistema,
        Claro,
        Escuro
    }

    public enum CorDestaque
    {
        Ambar,
        Azul,
        Verde,
        Ameixa,
        Terracota,
        Grafite
    }

    public enum Densidade
    {
        Confortavel,
        Compacto
    }

    public enum LayoutDetalhe
    {
        Lateral,
        Inferior
    }

    public enum NivelContraste
    {
        Automatico,
        Padrao,
        Medio,
        Alto
    }

    public enum NivelMovimento
    {
        Automatico,
        Completo,
        Reduzido,
        SemAnimacao
    }

    public enum FonteLeitura
    {
        Padrao,
        Legivel,
        Serifada,
        Monoespacada
    }

    public enum EspacamentoTexto
    {
        Normal,
        Medio,
        Amplo
    }

    public enum VerbosidadeLeitor
    {
        Discreta,
        Normal,
        Detalhada
    }

    [Flags]
    public enum ColunasLista
    {
        Nenhuma = 0,
        Usuario = 1,
        Categoria = 2,
        Forca = 4,
        Todas = Usuario | Categoria | Forca
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
        public const double EscalaMaxima = 2.0;

        public static readonly double[] EscalasDisponiveis = { 1.0, 1.15, 1.3, 1.5, 1.75, 2.0 };

        public static TipoDaltonismo Daltonismo { get; private set; } = TipoDaltonismo.Nenhum;
        public static ModoTema Modo { get; private set; } = ModoTema.Escuro;
        public static CorDestaque Destaque { get; private set; } = CorDestaque.Ambar;
        public static Densidade Densidade { get; private set; } = Densidade.Confortavel;
        public static LayoutDetalhe LayoutDetalhe { get; private set; } = LayoutDetalhe.Lateral;
        public static ColunasLista ColunasLista { get; private set; } = ColunasLista.Todas;

        public static bool DestaqueDisponivel => Daltonismo == TipoDaltonismo.Nenhum;

        public static double AlturaLinhaLista => Densidade == Densidade.Compacto ? 46 : 52;

        public static NivelContraste Contraste { get; private set; } = NivelContraste.Padrao;
        public static NivelMovimento Movimento { get; private set; } = NivelMovimento.Completo;
        public static FonteLeitura Fonte { get; private set; } = FonteLeitura.Padrao;
        public static EspacamentoTexto Espacamento { get; private set; } = EspacamentoTexto.Normal;
        public static VerbosidadeLeitor Verbosidade { get; private set; } = VerbosidadeLeitor.Normal;
        public static bool EscalaAutomatica { get; private set; }
        public static bool SublinharLinks { get; private set; }
        public static bool FocoReforcado { get; private set; }
        public static bool AnunciarAcoes { get; private set; }
        public static bool AvisarAntesBloqueio { get; private set; }

        public static double Escala { get; private set; } = EscalaNormal;
        public static bool LeitorTela { get; private set; }

        public static NivelContraste ContrasteEfetivo => Contraste == NivelContraste.Automatico
            ? (AcessibilidadeSistema.AltoContraste ? NivelContraste.Alto : NivelContraste.Padrao)
            : Contraste;

        public static bool AltoContraste => ContrasteEfetivo == NivelContraste.Alto;
        public static bool ContrasteMedio => ContrasteEfetivo == NivelContraste.Medio;

        public static NivelMovimento MovimentoEfetivo => Movimento == NivelMovimento.Automatico
            ? (AcessibilidadeSistema.ReduzirMovimento ? NivelMovimento.Reduzido : NivelMovimento.Completo)
            : Movimento;

        public static bool ReduzirAnimacoes => MovimentoEfetivo != NivelMovimento.Completo;
        public static bool SemAnimacao => MovimentoEfetivo == NivelMovimento.SemAnimacao;

        public static double EscalaEfetiva => EscalaAutomatica
            ? NormalizarEscala(AcessibilidadeSistema.EscalaTextoOs)
            : Escala;

        public static bool TemaClaroEfetivo => Modo switch
        {
            ModoTema.Claro => true,
            ModoTema.Escuro => false,
            _ => Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Light
        };

        public static event EventHandler? Alterado;

        private static readonly ConditionalWeakTable<Window, EscalaJanela> _escalas = new();
        private static readonly ConditionalWeakTable<Window, TextBlock> _anunciadores = new();
        private static readonly ConditionalWeakTable<Window, ToastAcessibilidade> _toasts = new();

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

        private static readonly IReadOnlyDictionary<CorVisual, uint> PadraoClaro = D(
            (CorVisual.WorkspaceBackground, 0xFFF7F4EF),
            (CorVisual.CardBackground, 0xFFFFFFFF),
            (CorVisual.CardBorder, 0xFFE6DFD1),
            (CorVisual.TitleBar, 0xFFF1ECE2),
            (CorVisual.TitleBarBorder, 0xFFE0D8C7),
            (CorVisual.InputBackground, 0xFFFBF8F2),
            (CorVisual.InputBorder, 0xFFD8CDB8),
            (CorVisual.RowHover, 0xFFF1EADD),
            (CorVisual.Separator, 0xFFEAE2D3),
            (CorVisual.Footer, 0xFFF1ECE2),
            (CorVisual.AccentPrimary, 0xFF9C7322),
            (CorVisual.AccentHover, 0xFF7F5C16),
            (CorVisual.AccentLight, 0xFFF3E8CD),
            (CorVisual.AccentText, 0xFF785713),
            (CorVisual.TextPrimary, 0xFF2A2317),
            (CorVisual.TextSecondary, 0xFF6A6051),
            (CorVisual.TextTertiary, 0xFF938878),
            (CorVisual.TextHeader, 0xFF938878),
            (CorVisual.TrailInactive, 0xFFDDD3C1),
            (CorVisual.ToggleOff, 0xFFCFC4AF),
            (CorVisual.HoverBackground, 0xFFF1EADD),
            (CorVisual.IconHoverBackground, 0xFFEFE6D3),
            (CorVisual.FavoriteColor, 0xFFD5901F),
            (CorVisual.FavoriteBorderColor, 0xFFC9BB9F),
            (CorVisual.StrengthWeak, 0xFFC0392B),
            (CorVisual.StrengthMedium, 0xFFB07714),
            (CorVisual.StrengthStrong, 0xFF4F7942),
            (CorVisual.StrengthExcellent, 0xFF3A6FA0),
            (CorVisual.CloseButtonHover, 0xFFE5484D),
            (CorVisual.CloseButtonPressed, 0xFFC93A3E),
            (CorVisual.StatusLocal, 0xFF4F7942),
            (CorVisual.StatusWarning, 0xFFB07714),
            (CorVisual.StatusConnected, 0xFF3A6FA0));

        private static readonly IReadOnlyDictionary<TipoDaltonismo, IReadOnlyDictionary<CorVisual, uint>> AjustesDaltonismoClaro =
            new Dictionary<TipoDaltonismo, IReadOnlyDictionary<CorVisual, uint>>
            {
                [TipoDaltonismo.Protanopia] = D(
                    (CorVisual.AccentPrimary, 0xFF006398), (CorVisual.AccentHover, 0xFF004E78),
                    (CorVisual.AccentLight, 0xFFDCEBF4), (CorVisual.AccentText, 0xFF00527D),
                    (CorVisual.FavoriteColor, 0xFFB57E00), (CorVisual.FavoriteBorderColor, 0xFFAEB7C1),
                    (CorVisual.StrengthWeak, 0xFFB3540A), (CorVisual.StrengthMedium, 0xFFB57E00),
                    (CorVisual.StrengthStrong, 0xFF007A5A), (CorVisual.StrengthExcellent, 0xFF006398),
                    (CorVisual.StatusLocal, 0xFF007A5A), (CorVisual.StatusWarning, 0xFFB57E00),
                    (CorVisual.StatusConnected, 0xFF006398)),
                [TipoDaltonismo.Deuteranopia] = D(
                    (CorVisual.AccentPrimary, 0xFF006398), (CorVisual.AccentHover, 0xFF004E78),
                    (CorVisual.AccentLight, 0xFFDCEBF4), (CorVisual.AccentText, 0xFF00527D),
                    (CorVisual.FavoriteColor, 0xFFB57E00), (CorVisual.FavoriteBorderColor, 0xFFAEB7C1),
                    (CorVisual.StrengthWeak, 0xFFB3540A), (CorVisual.StrengthMedium, 0xFFB57E00),
                    (CorVisual.StrengthStrong, 0xFF006398), (CorVisual.StrengthExcellent, 0xFF9A377E),
                    (CorVisual.StatusLocal, 0xFF006398), (CorVisual.StatusWarning, 0xFFB57E00),
                    (CorVisual.StatusConnected, 0xFF006398)),
                [TipoDaltonismo.Tritanopia] = D(
                    (CorVisual.AccentPrimary, 0xFFB0166F), (CorVisual.AccentHover, 0xFF8C1258),
                    (CorVisual.AccentLight, 0xFFFBE4F0), (CorVisual.AccentText, 0xFF96105E),
                    (CorVisual.FavoriteColor, 0xFF2E7D32), (CorVisual.FavoriteBorderColor, 0xFFB6A0B4),
                    (CorVisual.StrengthWeak, 0xFFC0392B), (CorVisual.StrengthMedium, 0xFFC05A00),
                    (CorVisual.StrengthStrong, 0xFF2E7D32), (CorVisual.StrengthExcellent, 0xFF8E24AA),
                    (CorVisual.StatusLocal, 0xFF2E7D32), (CorVisual.StatusWarning, 0xFFC05A00),
                    (CorVisual.StatusConnected, 0xFFB0166F)),
                [TipoDaltonismo.Monocromacia] = D(
                    (CorVisual.AccentPrimary, 0xFF3B3B3B), (CorVisual.AccentHover, 0xFF222222),
                    (CorVisual.AccentLight, 0xFFE4E4E4), (CorVisual.AccentText, 0xFF333333),
                    (CorVisual.FavoriteColor, 0xFF4D4D4D), (CorVisual.FavoriteBorderColor, 0xFFBDBDBD),
                    (CorVisual.StrengthWeak, 0xFF1A1A1A), (CorVisual.StrengthMedium, 0xFF595959),
                    (CorVisual.StrengthStrong, 0xFF8C8C8C), (CorVisual.StrengthExcellent, 0xFFBFBFBF),
                    (CorVisual.StatusLocal, 0xFF595959), (CorVisual.StatusWarning, 0xFF8C8C8C),
                    (CorVisual.StatusConnected, 0xFF3B3B3B))
            };

        private static readonly Dictionary<TipoDaltonismo, IReadOnlyDictionary<CorVisual, uint>> _claroMesclado = new();

        private static readonly IReadOnlyDictionary<CorDestaque, (uint[] Escuro, uint[] Claro)> Destaques =
            new Dictionary<CorDestaque, (uint[], uint[])>
            {
                [CorDestaque.Azul] = (
                    new uint[] { 0xFF5B9BD5, 0xFF4A82B4, 0xFF1E2E3D, 0xFF9FC7E8 },
                    new uint[] { 0xFF2F6FB0, 0xFF245A93, 0xFFDCEAF6, 0xFF1F5486 }),
                [CorDestaque.Verde] = (
                    new uint[] { 0xFF6FB07A, 0xFF5A9166, 0xFF1F3222, 0xFFA7D3AE },
                    new uint[] { 0xFF3B7D48, 0xFF2E6339, 0xFFDCEFDF, 0xFF2A5C34 }),
                [CorDestaque.Ameixa] = (
                    new uint[] { 0xFFA986C9, 0xFF8C6BAB, 0xFF2E2438, 0xFFCBB0DE },
                    new uint[] { 0xFF7B4EA8, 0xFF63407F, 0xFFEEE3F5, 0xFF5B3A7E }),
                [CorDestaque.Terracota] = (
                    new uint[] { 0xFFCC7A5C, 0xFFA8624A, 0xFF3A241C, 0xFFE4A98F },
                    new uint[] { 0xFFB0553A, 0xFF8C4530, 0xFFF6E4DC, 0xFF96432E }),
                [CorDestaque.Grafite] = (
                    new uint[] { 0xFF9AA3AE, 0xFF7E8792, 0xFF2A2E33, 0xFFC2C9D1 },
                    new uint[] { 0xFF5B6470, 0xFF48505A, 0xFFE6E8EB, 0xFF444B54 })
            };

        private static readonly CorVisual[] ChavesDestaque =
            { CorVisual.AccentPrimary, CorVisual.AccentHover, CorVisual.AccentLight, CorVisual.AccentText };

        private static readonly IReadOnlyDictionary<CorDestaque, IReadOnlyDictionary<CorVisual, uint>> SuperficiesEscuras =
            new Dictionary<CorDestaque, IReadOnlyDictionary<CorVisual, uint>>
            {
                [CorDestaque.Azul] = D(
                    (CorVisual.WorkspaceBackground, 0xFF121319),
                    (CorVisual.CardBackground, 0xFF1B1D25),
                    (CorVisual.CardBorder, 0xFF2E323F),
                    (CorVisual.TitleBar, 0xFF16171E),
                    (CorVisual.TitleBarBorder, 0xFF282C37),
                    (CorVisual.InputBackground, 0xFF1F222B),
                    (CorVisual.InputBorder, 0xFF353A49),
                    (CorVisual.RowHover, 0xFF1F222B),
                    (CorVisual.Separator, 0xFF262A35),
                    (CorVisual.Footer, 0xFF14161B),
                    (CorVisual.TrailInactive, 0xFF2E3340),
                    (CorVisual.ToggleOff, 0xFF393E4E),
                    (CorVisual.HoverBackground, 0xFF21242D),
                    (CorVisual.IconHoverBackground, 0xFF252934)),
                [CorDestaque.Verde] = D(
                    (CorVisual.WorkspaceBackground, 0xFF111513),
                    (CorVisual.CardBackground, 0xFF1A201C),
                    (CorVisual.CardBorder, 0xFF2D3630),
                    (CorVisual.TitleBar, 0xFF151A17),
                    (CorVisual.TitleBarBorder, 0xFF27302A),
                    (CorVisual.InputBackground, 0xFF1F2521),
                    (CorVisual.InputBorder, 0xFF343F38),
                    (CorVisual.RowHover, 0xFF1E2521),
                    (CorVisual.Separator, 0xFF252D28),
                    (CorVisual.Footer, 0xFF131715),
                    (CorVisual.TrailInactive, 0xFF2D3730),
                    (CorVisual.ToggleOff, 0xFF37433B),
                    (CorVisual.HoverBackground, 0xFF202722),
                    (CorVisual.IconHoverBackground, 0xFF252C27)),
                [CorDestaque.Ameixa] = D(
                    (CorVisual.WorkspaceBackground, 0xFF161218),
                    (CorVisual.CardBackground, 0xFF211B24),
                    (CorVisual.CardBorder, 0xFF392E3D),
                    (CorVisual.TitleBar, 0xFF1B161D),
                    (CorVisual.TitleBarBorder, 0xFF322836),
                    (CorVisual.InputBackground, 0xFF271F2A),
                    (CorVisual.InputBorder, 0xFF423547),
                    (CorVisual.RowHover, 0xFF271F2A),
                    (CorVisual.Separator, 0xFF2F2633),
                    (CorVisual.Footer, 0xFF19141A),
                    (CorVisual.TrailInactive, 0xFF3A2E3E),
                    (CorVisual.ToggleOff, 0xFF47394C),
                    (CorVisual.HoverBackground, 0xFF29212C),
                    (CorVisual.IconHoverBackground, 0xFF2F2532)),
                [CorDestaque.Terracota] = D(
                    (CorVisual.WorkspaceBackground, 0xFF18130E),
                    (CorVisual.CardBackground, 0xFF241D15),
                    (CorVisual.CardBorder, 0xFF3E3224),
                    (CorVisual.TitleBar, 0xFF1D1711),
                    (CorVisual.TitleBarBorder, 0xFF362C1F),
                    (CorVisual.InputBackground, 0xFF2A2218),
                    (CorVisual.InputBorder, 0xFF483A29),
                    (CorVisual.RowHover, 0xFF2A2218),
                    (CorVisual.Separator, 0xFF342A1E),
                    (CorVisual.Footer, 0xFF1B160F),
                    (CorVisual.TrailInactive, 0xFF3F3324),
                    (CorVisual.ToggleOff, 0xFF4D3E2C),
                    (CorVisual.HoverBackground, 0xFF2D241A),
                    (CorVisual.IconHoverBackground, 0xFF33291D)),
                [CorDestaque.Grafite] = D(
                    (CorVisual.WorkspaceBackground, 0xFF141414),
                    (CorVisual.CardBackground, 0xFF1E1E1F),
                    (CorVisual.CardBorder, 0xFF333334),
                    (CorVisual.TitleBar, 0xFF181819),
                    (CorVisual.TitleBarBorder, 0xFF2D2D2E),
                    (CorVisual.InputBackground, 0xFF232324),
                    (CorVisual.InputBorder, 0xFF3B3B3D),
                    (CorVisual.RowHover, 0xFF232324),
                    (CorVisual.Separator, 0xFF2A2A2C),
                    (CorVisual.Footer, 0xFF161617),
                    (CorVisual.TrailInactive, 0xFF343435),
                    (CorVisual.ToggleOff, 0xFF3F3F41),
                    (CorVisual.HoverBackground, 0xFF252526),
                    (CorVisual.IconHoverBackground, 0xFF2A2A2B))
            };

        private static readonly Dictionary<(CorDestaque, bool), IReadOnlyDictionary<CorVisual, uint>> _comDestaque = new();

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

        public static void Hidratar(TipoDaltonismo daltonismo, ModoTema modo, CorDestaque destaque, Densidade densidade,
            LayoutDetalhe layoutDetalhe, bool altoContraste, double escala, bool reduzirAnimacoes, bool leitorTela)
        {
            Daltonismo = daltonismo;
            Modo = modo;
            Destaque = destaque;
            Densidade = densidade;
            LayoutDetalhe = layoutDetalhe;
            Contraste = altoContraste ? NivelContraste.Alto : NivelContraste.Padrao;
            Escala = NormalizarEscala(escala);
            Movimento = reduzirAnimacoes ? NivelMovimento.Reduzido : NivelMovimento.Completo;
            LeitorTela = leitorTela;
        }

        public static void HidratarExpert(NivelContraste contraste, NivelMovimento movimento, FonteLeitura fonte,
            EspacamentoTexto espacamento, VerbosidadeLeitor verbosidade, bool escalaAutomatica, bool sublinharLinks,
            bool focoReforcado, bool anunciarAcoes, bool avisarAntesBloqueio)
        {
            Contraste = contraste;
            Movimento = movimento;
            Fonte = fonte;
            Espacamento = espacamento;
            Verbosidade = verbosidade;
            EscalaAutomatica = escalaAutomatica;
            SublinharLinks = sublinharLinks;
            FocoReforcado = focoReforcado;
            AnunciarAcoes = anunciarAcoes;
            AvisarAntesBloqueio = avisarAntesBloqueio;
        }

        public static void DefinirModoTema(ModoTema modo)
        {
            if (Modo == modo)
                return;

            Modo = modo;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void AplicarPerfil(ModoTema modo, CorDestaque destaque, Densidade densidade, LayoutDetalhe layout,
            TipoDaltonismo daltonismo, bool altoContraste, double escala, bool reduzirAnimacoes)
        {
            Modo = modo;
            Destaque = destaque;
            Densidade = densidade;
            LayoutDetalhe = layout;
            Daltonismo = daltonismo;
            Contraste = altoContraste ? NivelContraste.Alto : NivelContraste.Padrao;
            Escala = NormalizarEscala(escala);
            Movimento = reduzirAnimacoes ? NivelMovimento.Reduzido : NivelMovimento.Completo;

            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirCorDestaque(CorDestaque destaque)
        {
            if (Destaque == destaque)
                return;

            Destaque = destaque;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirDensidade(Densidade densidade)
        {
            if (Densidade == densidade)
                return;

            Densidade = densidade;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirLayoutDetalhe(LayoutDetalhe layout)
        {
            if (LayoutDetalhe == layout)
                return;

            LayoutDetalhe = layout;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void HidratarColunas(int valor) => ColunasLista = (ColunasLista)valor;

        public static void SelecionarColunaLista(ColunasLista coluna, bool visivel)
        {
            var novo = visivel ? ColunasLista | coluna : ColunasLista & ~coluna;
            if (ColunasLista == novo)
                return;

            ColunasLista = novo;
            Preferencias.ColunasLista = (int)novo;
            Preferencias.Salvar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void ReavaliarTemaDoSistema()
        {
            if (Modo != ModoTema.Sistema)
                return;

            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirDaltonismo(TipoDaltonismo tipo)
        {
            if (Daltonismo == tipo)
                return;

            Daltonismo = tipo;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirAltoContraste(bool ligado) =>
            DefinirContraste(ligado ? NivelContraste.Alto : NivelContraste.Padrao);

        public static void DefinirContraste(NivelContraste nivel)
        {
            if (Contraste == nivel)
                return;

            Contraste = nivel;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirEscala(double escala)
        {
            var nova = NormalizarEscala(escala);
            if (Escala == nova && !EscalaAutomatica)
                return;

            Escala = nova;
            EscalaAutomatica = false;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirEscalaAutomatica(bool ligado)
        {
            if (EscalaAutomatica == ligado)
                return;

            EscalaAutomatica = ligado;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirReducaoMovimento(bool ligado) =>
            DefinirMovimento(ligado ? NivelMovimento.Reduzido : NivelMovimento.Completo);

        public static void DefinirMovimento(NivelMovimento nivel)
        {
            if (Movimento == nivel)
                return;

            Movimento = nivel;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirFonte(FonteLeitura fonte)
        {
            if (Fonte == fonte)
                return;

            Fonte = fonte;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirEspacamento(EspacamentoTexto espacamento)
        {
            if (Espacamento == espacamento)
                return;

            Espacamento = espacamento;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirVerbosidade(VerbosidadeLeitor verbosidade)
        {
            if (Verbosidade == verbosidade)
                return;

            Verbosidade = verbosidade;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirSublinharLinks(bool ligado)
        {
            if (SublinharLinks == ligado)
                return;

            SublinharLinks = ligado;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirFocoReforcado(bool ligado)
        {
            if (FocoReforcado == ligado)
                return;

            FocoReforcado = ligado;
            Aplicar();
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirAnunciarAcoes(bool ligado)
        {
            if (AnunciarAcoes == ligado)
                return;

            AnunciarAcoes = ligado;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirAvisarAntesBloqueio(bool ligado)
        {
            if (AvisarAntesBloqueio == ligado)
                return;

            AvisarAntesBloqueio = ligado;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void DefinirLeitorTela(bool ligado)
        {
            if (LeitorTela == ligado)
                return;

            LeitorTela = ligado;
            Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static void ReavaliarSistema()
        {
            bool dependeDoSistema = Contraste == NivelContraste.Automatico ||
                Movimento == NivelMovimento.Automatico || EscalaAutomatica;
            if (!dependeDoSistema)
                return;

            Aplicar();
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

        public static void SelecionarModoTema(string? tag)
        {
            if (!Enum.TryParse<ModoTema>(tag, ignoreCase: true, out var modo))
                return;

            DefinirModoTema(modo);
            Preferencias.ModoTema = modo.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarCorDestaque(string? tag)
        {
            if (!Enum.TryParse<CorDestaque>(tag, ignoreCase: true, out var destaque))
                return;

            DefinirCorDestaque(destaque);
            Preferencias.CorDestaque = destaque.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarDensidade(string? tag)
        {
            if (!Enum.TryParse<Densidade>(tag, ignoreCase: true, out var densidade))
                return;

            DefinirDensidade(densidade);
            Preferencias.Densidade = densidade.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarLayoutDetalhe(string? tag)
        {
            if (!Enum.TryParse<LayoutDetalhe>(tag, ignoreCase: true, out var layout))
                return;

            DefinirLayoutDetalhe(layout);
            Preferencias.LayoutDetalhe = layout.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarEscala(string? tag)
        {
            if (string.Equals(tag, "auto", StringComparison.OrdinalIgnoreCase))
            {
                DefinirEscalaAutomatica(true);
                Preferencias.EscalaAutomatica = true;
                Preferencias.Salvar();
                return;
            }

            if (!double.TryParse(tag, NumberStyles.Any, CultureInfo.InvariantCulture, out var escala))
                return;

            DefinirEscala(escala);
            Preferencias.EscalaInterface = Escala;
            Preferencias.EscalaAutomatica = false;
            Preferencias.Salvar();
        }

        public static void SelecionarAltoContraste(bool ligado)
        {
            DefinirAltoContraste(ligado);
            Preferencias.NivelContraste = Contraste.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarContraste(string? tag)
        {
            if (!Enum.TryParse<NivelContraste>(tag, ignoreCase: true, out var nivel))
                return;

            DefinirContraste(nivel);
            Preferencias.NivelContraste = nivel.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarReducaoMovimento(bool ligado)
        {
            DefinirReducaoMovimento(ligado);
            Preferencias.NivelMovimento = Movimento.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarMovimento(string? tag)
        {
            if (!Enum.TryParse<NivelMovimento>(tag, ignoreCase: true, out var nivel))
                return;

            DefinirMovimento(nivel);
            Preferencias.NivelMovimento = nivel.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarFonte(string? tag)
        {
            if (!Enum.TryParse<FonteLeitura>(tag, ignoreCase: true, out var fonte))
                return;

            DefinirFonte(fonte);
            Preferencias.FonteLeitura = fonte.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarEspacamento(string? tag)
        {
            if (!Enum.TryParse<EspacamentoTexto>(tag, ignoreCase: true, out var espacamento))
                return;

            DefinirEspacamento(espacamento);
            Preferencias.EspacamentoTexto = espacamento.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarVerbosidade(string? tag)
        {
            if (!Enum.TryParse<VerbosidadeLeitor>(tag, ignoreCase: true, out var verbosidade))
                return;

            DefinirVerbosidade(verbosidade);
            Preferencias.VerbosidadeLeitor = verbosidade.ToString();
            Preferencias.Salvar();
        }

        public static void SelecionarSublinharLinks(bool ligado)
        {
            DefinirSublinharLinks(ligado);
            Preferencias.SublinharLinks = SublinharLinks;
            Preferencias.Salvar();
        }

        public static void SelecionarFocoReforcado(bool ligado)
        {
            DefinirFocoReforcado(ligado);
            Preferencias.FocoReforcado = FocoReforcado;
            Preferencias.Salvar();
        }

        public static void SelecionarAnunciarAcoes(bool ligado)
        {
            DefinirAnunciarAcoes(ligado);
            Preferencias.AnunciarAcoes = AnunciarAcoes;
            Preferencias.Salvar();
        }

        public static void SelecionarAvisarAntesBloqueio(bool ligado)
        {
            DefinirAvisarAntesBloqueio(ligado);
            Preferencias.AvisarAntesBloqueio = AvisarAntesBloqueio;
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

        public static Color TextoPrincipal() => Color.FromUInt32(TemaClaroEfetivo ? 0xFF000000 : 0xFFFFFFFF);
        public static Color TextoSecundario() => Color.FromUInt32(TemaClaroEfetivo ? 0xFF1B1B1B : 0xFFE4E4E9);
        public static Color TextoTerciario() => Color.FromUInt32(TemaClaroEfetivo ? 0xFF333333 : 0xFFCFCFD6);
        public static Color Borda() => Color.FromUInt32(TemaClaroEfetivo ? 0xFF000000 : 0xFFFFFFFF);

        internal static Color Cor(CorVisual cor)
        {
            var nivel = ContrasteEfetivo;
            if (nivel >= NivelContraste.Medio && TentarCorContraste(cor, nivel, out var reforcada))
                return reforcada;

            var valores = ValoresTema();
            if (!valores.TryGetValue(cor, out var argb))
                argb = (TemaClaroEfetivo ? PadraoClaro : PadraoEscuro)[cor];

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

            app.RequestedThemeVariant = TemaClaroEfetivo ? ThemeVariant.Light : ThemeVariant.Dark;

            foreach (var (chave, cor) in RecursosTema)
                app.Resources[chave] = new SolidColorBrush(Cor(cor));

            var brilho = Cor(CorVisual.AccentPrimary);
            app.Resources["FabShadow"] = BoxShadows.Parse($"0 3 16 -2 #4D{brilho.R:X2}{brilho.G:X2}{brilho.B:X2}");

            bool compacto = Densidade == Densidade.Compacto;
            app.Resources["AlturaCampo"] = compacto ? 38.0 : 44.0;
            app.Resources["AlturaNavItem"] = compacto ? 38.0 : 44.0;

            var (fontePadrao, fonteDisplay) = Fonte switch
            {
                FonteLeitura.Legivel => (FonteAtkinson, FonteAtkinson),
                FonteLeitura.Serifada => (FonteSerifada, FonteSerifada),
                FonteLeitura.Monoespacada => (FonteMonoStack, FonteMonoStack),
                _ => (FontePadraoStack, FonteDisplayStack)
            };
            app.Resources["FontePadrao"] = FontFamily.Parse(fontePadrao);
            app.Resources["FonteDisplay"] = FontFamily.Parse(fonteDisplay);

            app.Resources["EspacamentoTexto"] = Espacamento switch
            {
                EspacamentoTexto.Amplo => 1.1,
                EspacamentoTexto.Medio => 0.55,
                _ => 0.0
            };

            app.Resources["EspessuraFoco"] = FocoReforcado ? new Thickness(3) : new Thickness(2);
            app.Resources["MargemFoco"] = FocoReforcado ? new Thickness(-5) : new Thickness(-4);
            app.Resources["CorFoco"] = new SolidColorBrush(FocoReforcado
                ? Color.FromUInt32(TemaClaroEfetivo ? 0xFF000000 : 0xFFFFFFFF)
                : Cor(CorVisual.AccentPrimary));

            app.Resources["SublinhadoLink"] = SublinharLinks
                ? Avalonia.Media.TextDecorations.Underline
                : null;
        }

        private const string FontePadraoStack = "avares://CofreDeSenhas/Ativos/Fontes/PlusJakartaSans#Plus Jakarta Sans, Inter";
        private const string FonteDisplayStack = "Georgia, Constantia, Cambria, Noto Serif, DejaVu Serif, Liberation Serif, serif";
        private const string FonteAtkinson = "avares://CofreDeSenhas/Ativos/Fontes/AtkinsonHyperlegible#Atkinson Hyperlegible, Plus Jakarta Sans, Inter";
        private const string FonteSerifada = "Georgia, Constantia, Cambria, Noto Serif, DejaVu Serif, Liberation Serif, serif";
        private const string FonteMonoStack = "DejaVu Sans Mono, Liberation Mono, Noto Sans Mono, Ubuntu Mono, Consolas, monospace";

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

        public static void RegistrarToast(Window janela, Border host, TextBlock texto)
        {
            _toasts.Remove(janela);
            _toasts.Add(janela, new ToastAcessibilidade(host, texto));
            janela.Closed += (s, e) => _toasts.Remove(janela);
        }

        public static void Anunciar(Control origem, string mensagem, bool assertivo = false, bool forcar = false)
        {
            if (!forcar && !LeitorTela && !AnunciarAcoes)
                return;

            if (!forcar && Verbosidade == VerbosidadeLeitor.Discreta && !assertivo)
                return;

            if (string.IsNullOrWhiteSpace(mensagem))
                return;

            if (TopLevel.GetTopLevel(origem) is not Window janela)
                return;

            if (AnunciarAcoes || forcar || (LeitorTela && assertivo))
                Locucao.Falar(mensagem, assertivo || forcar);

            if (_toasts.TryGetValue(janela, out var toast))
                Dispatcher.UIThread.Post(() => toast.Mostrar(mensagem), DispatcherPriority.Background);

            if (!_anunciadores.TryGetValue(janela, out var anunciador))
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

        private sealed class ToastAcessibilidade
        {
            private readonly Border _host;
            private readonly TextBlock _texto;
            private readonly DispatcherTimer _timer;

            public ToastAcessibilidade(Border host, TextBlock texto)
            {
                _host = host;
                _texto = texto;
                _host.IsVisible = false;
                _host.Opacity = 0;
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.8) };
                _timer.Tick += (s, e) =>
                {
                    _timer.Stop();
                    _host.Opacity = 0;
                    _host.IsVisible = false;
                };
            }

            public void Mostrar(string mensagem)
            {
                _texto.Text = mensagem;
                _host.IsVisible = true;
                _host.Opacity = 1;
                _timer.Stop();
                _timer.Start();
            }
        }

        private static void AplicarEscala(Window janela)
        {
            var estado = _escalas.GetOrCreateValue(janela);
            double anterior = estado.Valor;
            double alvo = EscalaEfetiva;

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
            if (double.IsNaN(escala) || escala <= EscalaNormal)
                return EscalaNormal;

            var alvo = EscalaNormal;
            foreach (var passo in EscalasDisponiveis)
                if (escala >= passo - 0.001)
                    alvo = passo;
            return alvo;
        }

        private static IReadOnlyDictionary<CorVisual, uint> ValoresTema()
        {
            var baseTema = TemaClaroEfetivo
                ? ClaroPara(Daltonismo)
                : Daltonismo switch
                {
                    TipoDaltonismo.Protanopia => ProtanopiaEscuro,
                    TipoDaltonismo.Deuteranopia => DeuteranopiaEscuro,
                    TipoDaltonismo.Tritanopia => TritanopiaEscuro,
                    TipoDaltonismo.Monocromacia => MonocromaciaEscuro,
                    _ => PadraoEscuro
                };

            if (Daltonismo != TipoDaltonismo.Nenhum || Destaque == CorDestaque.Ambar || !Destaques.ContainsKey(Destaque))
                return baseTema;

            var chave = (Destaque, TemaClaroEfetivo);
            if (_comDestaque.TryGetValue(chave, out var pronto))
                return pronto;

            var (escuro, claro) = Destaques[Destaque];
            var overrides = TemaClaroEfetivo ? claro : escuro;

            var combinado = new Dictionary<CorVisual, uint>(baseTema);

            if (!TemaClaroEfetivo && SuperficiesEscuras.TryGetValue(Destaque, out var superficies))
                foreach (var (nome, valor) in superficies)
                    combinado[nome] = valor;

            for (int i = 0; i < ChavesDestaque.Length; i++)
                combinado[ChavesDestaque[i]] = overrides[i];

            _comDestaque[chave] = combinado;
            return combinado;
        }

        private static IReadOnlyDictionary<CorVisual, uint> ClaroPara(TipoDaltonismo daltonismo)
        {
            if (daltonismo == TipoDaltonismo.Nenhum || !AjustesDaltonismoClaro.TryGetValue(daltonismo, out var ajustes))
                return PadraoClaro;

            if (_claroMesclado.TryGetValue(daltonismo, out var pronto))
                return pronto;

            var combinado = new Dictionary<CorVisual, uint>(PadraoClaro);
            foreach (var (chave, valor) in ajustes)
                combinado[chave] = valor;

            _claroMesclado[daltonismo] = combinado;
            return combinado;
        }

        private static bool TentarCorContraste(CorVisual cor, NivelContraste nivel, out Color resultado)
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
                    if (nivel < NivelContraste.Alto)
                        return false;
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
