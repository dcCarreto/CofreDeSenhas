using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace CofreDeSenhas.Controles
{
    public class CustomSlider : Control
    {
        private int _value = 12;
        private int _minimum = 4;
        private int _maximum = 64;
        private bool _dragging;

        public event EventHandler? ValueChanged;

        public int Value
        {
            get => _value;
            set
            {
                int novo = Math.Clamp(value, _minimum, _maximum);
                if (_value != novo)
                {
                    _value = novo;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    InvalidateVisual();
                }
            }
        }

        public int Minimum
        {
            get => _minimum;
            set { _minimum = value; if (_value < _minimum) Value = _minimum; }
        }

        public int Maximum
        {
            get => _maximum;
            set { _maximum = value; if (_value > _maximum) Value = _maximum; }
        }

        public CustomSlider()
        {
            Height = 24;
            Cursor = new Cursor(StandardCursorType.Hand);
            ActualThemeVariantChanged += (s, e) => InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _dragging = true;
            e.Pointer.Capture(this);
            AtualizarPeloMouse(e.GetPosition(this).X);
            base.OnPointerPressed(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_dragging)
                AtualizarPeloMouse(e.GetPosition(this).X);
            base.OnPointerMoved(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            _dragging = false;
            e.Pointer.Capture(null);
            base.OnPointerReleased(e);
        }

        private void AtualizarPeloMouse(double x)
        {
            double inicio = 12;
            double largura = Bounds.Width - 24;
            if (largura <= 0) return;

            double razao = Math.Clamp((x - inicio) / largura, 0, 1);
            Value = _minimum + (int)(razao * (_maximum - _minimum));
        }

        public override void Render(DrawingContext g)
        {
            g.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

            double inicio = 12;
            double largura = Bounds.Width - 24;
            if (largura <= 0) return;

            double centroY = Bounds.Height / 2;
            const double alturaTrilha = 4;

            var trilha = new Rect(inicio, centroY - alturaTrilha / 2, largura, alturaTrilha);
            g.DrawRectangle(Tema.Pincel(Tema.TrailInactive), null, new RoundedRect(trilha, alturaTrilha / 2));

            double razao = (double)(_value - _minimum) / (_maximum - _minimum);
            double preenchido = largura * razao;
            if (preenchido > 0)
            {
                var ativa = new Rect(inicio, centroY - alturaTrilha / 2, preenchido, alturaTrilha);
                g.DrawRectangle(Tema.Pincel(Tema.AccentPrimary), null, new RoundedRect(ativa, alturaTrilha / 2));
            }

            const double raioThumb = 8;
            var centroThumb = new Point(inicio + preenchido, centroY);
            g.DrawEllipse(Brushes.White, new Pen(Tema.Pincel(Tema.AccentPrimary), 2), centroThumb, raioThumb, raioThumb);
        }
    }
}
