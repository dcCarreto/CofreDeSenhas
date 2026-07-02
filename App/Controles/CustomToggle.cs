using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace CofreDeSenhas.Controles
{
    public class CustomToggle : Control
    {
        private bool _checked;
        private bool _hovered;

        public event EventHandler? CheckedChanged;

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                    InvalidateVisual();
                }
            }
        }

        public CustomToggle()
        {
            Width = 46;
            Height = 24;
            Cursor = new Cursor(StandardCursorType.Hand);
            ActualThemeVariantChanged += (s, e) => InvalidateVisual();
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            _hovered = true;
            InvalidateVisual();
            base.OnPointerEntered(e);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            _hovered = false;
            InvalidateVisual();
            base.OnPointerExited(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (new Rect(Bounds.Size).Contains(e.GetPosition(this)))
                Checked = !Checked;
            base.OnPointerReleased(e);
        }

        public override void Render(DrawingContext g)
        {
            g.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

            double w = Bounds.Width;
            double h = Bounds.Height;
            double diametroThumb = h - 4;

            var corTrilha = _checked ? Tema.AccentPrimary : Tema.ToggleOff;
            var trilha = new Rect(2, 2, w - 4, h - 4);
            g.DrawRectangle(Tema.Pincel(corTrilha), null, new RoundedRect(trilha, (h - 4) / 2));

            double thumbX = _checked ? w - diametroThumb - 2 : 2;
            var centro = new Point(thumbX + diametroThumb / 2, h / 2);
            var contorno = _hovered ? new Pen(Tema.Pincel(Tema.AccentPrimary), 2) : null;
            g.DrawEllipse(Brushes.White, contorno, centro, diametroThumb / 2, diametroThumb / 2);
        }
    }
}
