using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace CofreDeSenhas
{
    public static class Tema
    {
        public static Color CardBackground => Acessibilidade.Cor(CorVisual.CardBackground);
        public static Color CardBorder => Acessibilidade.Cor(CorVisual.CardBorder);

        public static Color InputBorder => Acessibilidade.Cor(CorVisual.InputBorder);

        public static Color RowHover => Acessibilidade.Cor(CorVisual.RowHover);
        public static Color Separator => Acessibilidade.Cor(CorVisual.Separator);

        public static Color AccentPrimary => Acessibilidade.Cor(CorVisual.AccentPrimary);
        public static Color AccentLight => Acessibilidade.Cor(CorVisual.AccentLight);
        public static Color AccentText => Acessibilidade.Cor(CorVisual.AccentText);

        public static Color TextPrimary => Acessibilidade.Cor(CorVisual.TextPrimary);
        public static Color TextSecondary => Acessibilidade.Cor(CorVisual.TextSecondary);
        public static Color TextTertiary => Acessibilidade.Cor(CorVisual.TextTertiary);

        public static Color StrengthWeak => Acessibilidade.Cor(CorVisual.StrengthWeak);
        public static Color StrengthMedium => Acessibilidade.Cor(CorVisual.StrengthMedium);
        public static Color StrengthStrong => Acessibilidade.Cor(CorVisual.StrengthStrong);
        public static Color StrengthExcellent => Acessibilidade.Cor(CorVisual.StrengthExcellent);

        public static Color TrailInactive => Acessibilidade.Cor(CorVisual.TrailInactive);
        public static Color ToggleOff => Acessibilidade.Cor(CorVisual.ToggleOff);
        public static Color FavoriteColor => Acessibilidade.Cor(CorVisual.FavoriteColor);
        public static Color FavoriteBorderColor => Acessibilidade.Cor(CorVisual.FavoriteBorderColor);
        public static Color StatusLocal => Acessibilidade.Cor(CorVisual.StatusLocal);
        public static Color StatusWarning => Acessibilidade.Cor(CorVisual.StatusWarning);
        public static Color StatusConnected => Acessibilidade.Cor(CorVisual.StatusConnected);

        private static readonly ConcurrentDictionary<uint, ImmutableSolidColorBrush> _pinceis = new();

        public static IBrush Pincel(Color cor) =>
            _pinceis.GetOrAdd(cor.ToUInt32(), argb => new ImmutableSolidColorBrush(Color.FromUInt32(argb)));
    }
}
