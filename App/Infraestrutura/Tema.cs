using Avalonia.Media;

namespace CofreDeSenhas
{
    public static class Tema
    {
        public static bool ModoEscuro { get; private set; }

        public static void DefinirModo(bool escuro) => ModoEscuro = escuro;

        public static Color WorkspaceBackground => Acessibilidade.Cor(CorVisual.WorkspaceBackground, ModoEscuro);
        public static Color CardBackground => Acessibilidade.Cor(CorVisual.CardBackground, ModoEscuro);
        public static Color CardBorder => Acessibilidade.Cor(CorVisual.CardBorder, ModoEscuro);
        public static Color TitleBar => Acessibilidade.Cor(CorVisual.TitleBar, ModoEscuro);
        public static Color TitleBarBorder => Acessibilidade.Cor(CorVisual.TitleBarBorder, ModoEscuro);

        public static Color InputBackground => Acessibilidade.Cor(CorVisual.InputBackground, ModoEscuro);
        public static Color InputBorder => Acessibilidade.Cor(CorVisual.InputBorder, ModoEscuro);

        public static Color RowHover => Acessibilidade.Cor(CorVisual.RowHover, ModoEscuro);
        public static Color Separator => Acessibilidade.Cor(CorVisual.Separator, ModoEscuro);
        public static Color Footer => Acessibilidade.Cor(CorVisual.Footer, ModoEscuro);

        public static Color AccentPrimary => Acessibilidade.Cor(CorVisual.AccentPrimary, ModoEscuro);
        public static Color AccentHover => Acessibilidade.Cor(CorVisual.AccentHover, ModoEscuro);
        public static Color AccentLight => Acessibilidade.Cor(CorVisual.AccentLight, ModoEscuro);

        public static Color TextPrimary => Acessibilidade.Cor(CorVisual.TextPrimary, ModoEscuro);
        public static Color TextSecondary => Acessibilidade.Cor(CorVisual.TextSecondary, ModoEscuro);
        public static Color TextTertiary => Acessibilidade.Cor(CorVisual.TextTertiary, ModoEscuro);

        public static Color StrengthWeak => Acessibilidade.Cor(CorVisual.StrengthWeak, ModoEscuro);
        public static Color StrengthMedium => Acessibilidade.Cor(CorVisual.StrengthMedium, ModoEscuro);
        public static Color StrengthStrong => Acessibilidade.Cor(CorVisual.StrengthStrong, ModoEscuro);
        public static Color StrengthExcelent => Acessibilidade.Cor(CorVisual.StrengthExcelent, ModoEscuro);

        public static Color TrailInactive => Acessibilidade.Cor(CorVisual.TrailInactive, ModoEscuro);
        public static Color ToggleOff => Acessibilidade.Cor(CorVisual.ToggleOff, ModoEscuro);
        public static Color HoverBackground => Acessibilidade.Cor(CorVisual.HoverBackground, ModoEscuro);
        public static Color IconHoverBackground => Acessibilidade.Cor(CorVisual.IconHoverBackground, ModoEscuro);
        public static Color FavoriteColor => Acessibilidade.Cor(CorVisual.FavoriteColor, ModoEscuro);
        public static Color FavoriteBorderColor => Acessibilidade.Cor(CorVisual.FavoriteBorderColor, ModoEscuro);
        public static Color CloseButtonHover => Acessibilidade.Cor(CorVisual.CloseButtonHover, ModoEscuro);
        public static Color CloseButtonPressed => Acessibilidade.Cor(CorVisual.CloseButtonPressed, ModoEscuro);
        public static Color StatusLocal => Acessibilidade.Cor(CorVisual.StatusLocal, ModoEscuro);
        public static Color StatusWarning => Acessibilidade.Cor(CorVisual.StatusWarning, ModoEscuro);
        public static Color StatusConnected => Acessibilidade.Cor(CorVisual.StatusConnected, ModoEscuro);

        public static IBrush Pincel(Color cor) => new SolidColorBrush(cor);
    }
}
