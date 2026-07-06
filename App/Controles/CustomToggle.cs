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
                    AtualizarEstadoAcessivel();
                    InvalidateVisual();
                }
            }
        }

        public CustomToggle()
        {
            Width = 40;
            Height = 22;
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

        private void AtualizarEstadoAcessivel() =>
            AutomationProperties.SetItemStatus(this, Idioma.Texto(_checked ? "A11y.ToggleOn" : "A11y.ToggleOff"));

        protected override AutomationPeer OnCreateAutomationPeer() => new CustomTogglePeer(this);

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
            {
                Checked = !Checked;
                Focus();
            }
            base.OnPointerReleased(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
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

            if (IsFocused)
            {
                var foco = new Rect(-2, -2, w + 4, h + 4);
                g.DrawRectangle(null, new Pen(Tema.Pincel(Tema.AccentPrimary), 2), new RoundedRect(foco, (h + 4) / 2));
            }
        }

        private sealed class CustomTogglePeer : ControlAutomationPeer, IToggleProvider
        {
            public CustomTogglePeer(CustomToggle owner) : base(owner) { }

            private CustomToggle Alvo => (CustomToggle)Owner;

            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

            protected override string GetClassNameCore() => nameof(CustomToggle);

            public ToggleState ToggleState => Alvo.Checked ? ToggleState.On : ToggleState.Off;

            public void Toggle() => Alvo.Checked = !Alvo.Checked;
        }
    }
}
