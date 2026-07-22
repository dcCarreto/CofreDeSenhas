using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace CofreDeSenhas.Controles
{
    public sealed class Icone : AvaloniaPath
    {
        public static readonly StyledProperty<string?> ChaveProperty =
            AvaloniaProperty.Register<Icone, string?>(nameof(Chave));

        public static readonly StyledProperty<bool> PreenchidoProperty =
            AvaloniaProperty.Register<Icone, bool>(nameof(Preenchido));

        public string? Chave
        {
            get => GetValue(ChaveProperty);
            set => SetValue(ChaveProperty, value);
        }

        public bool Preenchido
        {
            get => GetValue(PreenchidoProperty);
            set => SetValue(PreenchidoProperty, value);
        }

        public Icone()
        {
            Stretch = Stretch.Uniform;
            StrokeLineCap = PenLineCap.Round;
            StrokeJoin = PenLineJoin.Round;
        }

        static Icone()
        {
            ChaveProperty.Changed.AddClassHandler<Icone>((icone, e) => icone.AtualizarGeometria());
        }

        private void AtualizarGeometria() =>
            Data = Chave != null ? (Geometry)Application.Current!.FindResource(Chave)! : null;
    }
}
