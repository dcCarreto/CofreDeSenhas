using Avalonia.Media;

namespace AppLinux
{
    // Espelho em C# das cores definidas em App.axaml, para os controles desenhados via código.
    // Os valores são os mesmos do Theme do app Windows.
    public static class Tema
    {
        public static bool ModoEscuro { get; private set; }

        public static void DefinirModo(bool escuro) => ModoEscuro = escuro;

        private static Color C(uint claro, uint escuro) => Color.FromUInt32(ModoEscuro ? escuro : claro);

        public static Color WorkspaceBackground => C(0xFFEAEBF1, 0xFF111218);
        public static Color CardBackground => C(0xFFFFFFFF, 0xFF1F212A);
        public static Color CardBorder => C(0xFFECECF0, 0xFF30323D);

        public static Color InputBackground => C(0xFFF7F7FB, 0xFF282A35);
        public static Color InputBorder => C(0xFFDFDFE9, 0xFF3A3C49);

        public static Color RowHover => C(0xFFFAF9FE, 0xFF282A37);
        public static Color Separator => C(0xFFF5F5F7, 0xFF2C2E39);

        public static Color AccentPrimary => C(0xFF7C3AED, 0xFF9569F4);
        public static Color AccentHover => C(0xFF6D28D9, 0xFF7C3AED);
        public static Color AccentLight => C(0xFFF3EEFE, 0xFF372F54);

        public static Color TextPrimary => C(0xFF20232B, 0xFFEDEEF3);
        public static Color TextSecondary => C(0xFF8D909A, 0xFF9698A4);
        public static Color TextTertiary => C(0xFFA0A0A9, 0xFF787A86);

        public static readonly Color StrengthWeak = Color.FromUInt32(0xFFEF4444);
        public static readonly Color StrengthMedium = Color.FromUInt32(0xFFF59E0B);
        public static readonly Color StrengthStrong = Color.FromUInt32(0xFF16A34A);

        public static Color TrailInactive => C(0xFFE6E6EC, 0xFF383A46);
        public static Color ToggleOff => C(0xFFD6D6DD, 0xFF444654);
        public static Color IconHoverBackground => C(0xFFEFEFF4, 0xFF323440);
        public static readonly Color FavoriteColor = Color.FromUInt32(0xFFF5A623);
        public static Color FavoriteBorderColor => C(0xFFCDCDD4, 0xFF585A67);

        public static IBrush Pincel(Color cor) => new SolidColorBrush(cor);
    }
}
