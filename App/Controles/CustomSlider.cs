using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
                    AtualizarEstadoAcessivel();
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

        private int PassoGrande => Math.Max(1, (_maximum - _minimum) / 10);

        public CustomSlider()
        {
            Height = 32;
            Focusable = true;
            Cursor = new Cursor(StandardCursorType.Hand);
            ActualThemeVariantChanged += (s, e) => InvalidateVisual();
            Acessibilidade.Alterado += AoAlterarAcessibilidade;
            DetachedFromVisualTree += (s, e) => Acessibilidade.Alterado -= AoAlterarAcessibilidade;
            AtualizarEstadoAcessivel();
        }

        private void AoAlterarAcessibilidade(object? sender, EventArgs e)
        {
            AtualizarEstadoAcessivel();
            InvalidateVisual();
        }

        private void AtualizarEstadoAcessivel()
        {
            AutomationProperties.SetItemStatus(this, _value.ToString());
            AutomationProperties.SetHelpText(this, Idioma.Texto("A11y.SliderHelp"));
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new CustomSliderPeer(this);

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _dragging = true;
            e.Pointer.Capture(this);
            Focus();
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

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            _dragging = false;
            base.OnPointerCaptureLost(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.Down:
                    Value -= 1;
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.Up:
                    Value += 1;
                    e.Handled = true;
                    break;
                case Key.PageDown:
                    Value -= PassoGrande;
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    Value += PassoGrande;
                    e.Handled = true;
                    break;
                case Key.Home:
                    Value = _minimum;
                    e.Handled = true;
                    break;
                case Key.End:
                    Value = _maximum;
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            InvalidateVisual();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            InvalidateVisual();
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

            const double raioThumb = 9;
            var centroThumb = new Point(inicio + preenchido, centroY);
            if (IsFocused)
                g.DrawEllipse(null, new Pen(Tema.Pincel(Tema.AccentPrimary), 2), centroThumb, raioThumb + 3, raioThumb + 3);
            g.DrawEllipse(Brushes.White, new Pen(Tema.Pincel(Tema.AccentPrimary), 2), centroThumb, raioThumb, raioThumb);
        }

        private sealed class CustomSliderPeer : ControlAutomationPeer, IRangeValueProvider
        {
            public CustomSliderPeer(CustomSlider owner) : base(owner) { }

            private CustomSlider Alvo => (CustomSlider)Owner;

            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

            protected override string GetClassNameCore() => nameof(CustomSlider);

            public bool IsReadOnly => false;

            public double Minimum => Alvo.Minimum;

            public double Maximum => Alvo.Maximum;

            public double Value => Alvo.Value;

            public double SmallChange => 1;

            public double LargeChange => Alvo.PassoGrande;

            public void SetValue(double value) => Alvo.Value = (int)Math.Round(value);
        }
    }
}
