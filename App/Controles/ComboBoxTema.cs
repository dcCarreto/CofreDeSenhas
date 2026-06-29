using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App
{
    public class ComboBoxTema : ComboBox
    {
        private const int WM_PAINT = 0x000F;

        public ComboBoxTema()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 22;
            BackColor = Theme.InputBackground;
            ForeColor = Theme.TextPrimary;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool selecionado = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var bg = selecionado ? Theme.AccentLight : Theme.InputBackground;
            using (var b = new SolidBrush(bg))
                e.Graphics.FillRectangle(b, e.Bounds);

            var texto = GetItemText(Items[e.Index]);
            TextRenderer.DrawText(e.Graphics, texto, Font,
                new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                Theme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                using var g = Graphics.FromHwnd(Handle);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int aw = 20;
                var arrow = new Rectangle(Width - aw, 0, aw, Height);
                using (var b = new SolidBrush(Theme.InputBackground))
                    g.FillRectangle(b, arrow);

                int cx = Width - aw / 2 - 1;
                int cy = Height / 2;
                using var pen = new Pen(Theme.TextSecondary, 1.6f);
                g.DrawLines(pen, new[]
                {
                    new PointF(cx - 4, cy - 2),
                    new PointF(cx, cy + 2.5f),
                    new PointF(cx + 4, cy - 2)
                });
            }
        }
    }
}
