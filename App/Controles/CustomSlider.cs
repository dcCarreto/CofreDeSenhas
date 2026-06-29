using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App
{
    public class CustomSlider : Control
    {
        private int _value = 12;
        private int _minimum = 4;
        private int _maximum = 64;
        private bool _dragging = false;

        public event EventHandler? ValueChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value
        {
            get => _value;
            set
            {
                int newValue = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value != newValue)
                {
                    _value = newValue;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                if (_value < _minimum)
                    Value = _minimum;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                if (_value > _maximum)
                    Value = _maximum;
            }
        }

        public CustomSlider()
        {
            this.DoubleBuffered = true;
            this.Height = 24;
            this.Cursor = Cursors.Hand;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _dragging = true;
            UpdateValueFromMouse(e.X);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                UpdateValueFromMouse(e.X);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            base.OnMouseUp(e);
        }

        private void UpdateValueFromMouse(int x)
        {
            int trackStart = 12;
            int trackEnd = this.Width - 12;
            int trackWidth = trackEnd - trackStart;

            if (trackWidth <= 0)
                return;

            float ratio = Math.Max(0, Math.Min(1, (float)(x - trackStart) / trackWidth));
            Value = _minimum + (int)((ratio * (_maximum - _minimum)));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int trackStart = 12;
            int trackEnd = this.Width - 12;
            int trackWidth = trackEnd - trackStart;
            int centerY = this.Height / 2;
            int trackHeight = 4;

            Rectangle inactiveTrack = new Rectangle(trackStart, centerY - trackHeight / 2, trackWidth, trackHeight);
            using (var path = RoundedRectangle(inactiveTrack, trackHeight / 2f))
            {
                using (var brush = new SolidBrush(Theme.TrailInactive))
                {
                    g.FillPath(brush, path);
                }
            }

            float ratio = (float)(_value - _minimum) / (_maximum - _minimum);
            int filledWidth = (int)(trackWidth * ratio);
            Rectangle activeTrack = new Rectangle(trackStart, centerY - trackHeight / 2, filledWidth, trackHeight);
            using (var path = RoundedRectangle(activeTrack, trackHeight / 2f))
            {
                using (var brush = new SolidBrush(Theme.AccentPrimary))
                {
                    g.FillPath(brush, path);
                }
            }

            int thumbX = trackStart + filledWidth - 8;
            int thumbSize = 16;
            Rectangle thumbRect = new Rectangle(thumbX, centerY - thumbSize / 2, thumbSize, thumbSize);

            using (var path = RoundedRectangle(thumbRect, thumbSize / 2f))
            {
                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(Theme.AccentPrimary, 2))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private GraphicsPath RoundedRectangle(Rectangle rect, float radius)
        {
            return RoundedRectangle(rect.X, rect.Y, rect.Width, rect.Height, radius);
        }

        private GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;

            if (diameter > width)
                diameter = width;
            if (diameter > height)
                diameter = height;

            var arc = new RectangleF(x, y, diameter, diameter);
            path.AddArc(arc, 180, 90);

            arc.X = x + width - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = y + height - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = x;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
