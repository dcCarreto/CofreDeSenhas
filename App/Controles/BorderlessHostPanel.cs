using System;
using System.Drawing;
using System.Windows.Forms;

namespace App
{
    public class BorderlessHostPanel : Panel
    {
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;

        public int BordaResize = 8;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                var form = FindForm();
                if (form != null && form.WindowState == FormWindowState.Normal)
                {
                    int x = unchecked((short)(long)m.LParam);
                    int y = unchecked((short)((long)m.LParam >> 16));
                    var p = form.PointToClient(new Point(x, y));
                    int w = form.ClientSize.Width, h = form.ClientSize.Height, b = BordaResize;
                    bool naBorda = p.X <= b || p.X >= w - b || p.Y <= b || p.Y >= h - b;
                    if (naBorda)
                    {
                        m.Result = (IntPtr)HTTRANSPARENT;
                        return;
                    }
                }
            }
            base.WndProc(ref m);
        }
    }
}
