using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App
{
    public class CustomToggle : Control
    {
        private bool _checked = false;
        private bool _hovered = false;

        public event EventHandler? CheckedChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public CustomToggle()
        {
            this.DoubleBuffered = true;
            this.Height = 26;
            this.Width = 50;
            this.Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int width = this.Width;
            int height = this.Height;
            int thumbSize = height - 4;
            float borderRadius = height / 2f;

            Color trailColor = _checked ? Theme.AccentPrimary : Theme.ToggleOff;
            using (var path = RoundedRectangle(2, 2, width - 4, height - 4, borderRadius))
            {
                using (var brush = new SolidBrush(trailColor))
                {
                    g.FillPath(brush, path);
                }
            }

            int thumbX = _checked ? width - thumbSize - 2 : 2;
            Rectangle thumbRect = new Rectangle(thumbX, 2, thumbSize, thumbSize);

            using (var path = RoundedRectangle(thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height, thumbSize / 2f))
            {
                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillPath(brush, path);
                }
            }

            if (_hovered)
            {
                using (var pen = new Pen(Theme.AccentPrimary, 2))
                {
                    using (var path = RoundedRectangle(thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height, thumbSize / 2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
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
