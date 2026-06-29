using System.Drawing;

namespace App
{
    public static class Theme
    {
        public static bool ModoEscuro { get; private set; } = false;

        public static void DefinirModo(bool escuro) => ModoEscuro = escuro;

        private static Color C(Color claro, Color escuro) => ModoEscuro ? escuro : claro;

        public static Color WorkspaceBackground => C(Color.FromArgb(234, 235, 241), Color.FromArgb(17, 18, 24));
        public static Color CardBackground => C(Color.White, Color.FromArgb(31, 33, 42));
        public static Color CardBorder => C(Color.FromArgb(236, 236, 240), Color.FromArgb(48, 50, 61));
        public static Color TitleBar => C(Color.White, Color.FromArgb(24, 25, 33));
        public static Color TitleBarBorder => C(Color.FromArgb(238, 238, 241), Color.FromArgb(44, 46, 57));

        public static Color InputBackground => C(Color.FromArgb(247, 247, 251), Color.FromArgb(40, 42, 53));
        public static Color InputBorder => C(Color.FromArgb(223, 223, 233), Color.FromArgb(58, 60, 73));

        public static Color RowHover => C(Color.FromArgb(250, 249, 254), Color.FromArgb(40, 42, 55));
        public static Color Separator => C(Color.FromArgb(245, 245, 247), Color.FromArgb(44, 46, 57));
        public static Color Footer => C(Color.FromArgb(252, 252, 253), Color.FromArgb(27, 28, 37));

        public static Color AccentPrimary => C(Color.FromArgb(124, 58, 237), Color.FromArgb(149, 105, 244));
        public static Color AccentHover => C(Color.FromArgb(109, 40, 217), Color.FromArgb(124, 58, 237));
        public static Color AccentLight => C(Color.FromArgb(243, 238, 254), Color.FromArgb(55, 47, 84));

        public static Color TextPrimary => C(Color.FromArgb(32, 35, 43), Color.FromArgb(237, 238, 243));
        public static Color TextSecondary => C(Color.FromArgb(141, 144, 154), Color.FromArgb(150, 152, 164));
        public static Color TextTertiary => C(Color.FromArgb(160, 160, 169), Color.FromArgb(120, 122, 134));
        public static Color TextHeader => C(Color.FromArgb(160, 160, 169), Color.FromArgb(120, 122, 134));

        public static readonly Color StrengthWeak = Color.FromArgb(239, 68, 68);
        public static readonly Color StrengthMedium = Color.FromArgb(245, 158, 11);
        public static readonly Color StrengthStrong = Color.FromArgb(22, 163, 74);

        public static Color TrailInactive => C(Color.FromArgb(230, 230, 236), Color.FromArgb(56, 58, 70));
        public static Color ToggleOff => C(Color.FromArgb(214, 214, 221), Color.FromArgb(68, 70, 84));
        public static Color HoverBackground => C(Color.FromArgb(241, 241, 244), Color.FromArgb(44, 46, 58));
        public static readonly Color CloseButtonHover = Color.FromArgb(232, 17, 35);
        public static Color IconHoverBackground => C(Color.FromArgb(239, 239, 244), Color.FromArgb(50, 52, 64));
        public static readonly Color FavoriteColor = Color.FromArgb(245, 166, 35);
        public static Color FavoriteBorderColor => C(Color.FromArgb(205, 205, 212), Color.FromArgb(88, 90, 103));

        public static readonly Color CategoryPersonalBg = Color.FromArgb(234, 241, 255);
        public static readonly Color CategoryPersonalFg = Color.FromArgb(37, 99, 235);

        public static readonly Color CategoryWorkBg = Color.FromArgb(241, 236, 254);
        public static readonly Color CategoryWorkFg = Color.FromArgb(124, 58, 237);

        public static readonly Color CategoryFinanceBg = Color.FromArgb(231, 247, 238);
        public static readonly Color CategoryFinanceFg = Color.FromArgb(22, 163, 74);

        public static readonly Color CategoryGamesBg = Color.FromArgb(253, 238, 224);
        public static readonly Color CategoryGamesFg = Color.FromArgb(234, 88, 12);

        public static readonly Color CategoryStreamingBg = Color.FromArgb(253, 234, 243);
        public static readonly Color CategoryStreamingFg = Color.FromArgb(219, 39, 119);

        public static Font GetFont(string family, float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(family, size, style);
        }

        public static Font SegoeUI(float size, FontStyle style = FontStyle.Regular)
        {
            return GetFont("Segoe UI", size, style);
        }

        public static Font Consolas(float size, FontStyle style = FontStyle.Regular)
        {
            return GetFont("Consolas", size, style);
        }
    }
}
