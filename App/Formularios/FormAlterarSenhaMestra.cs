using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App
{
    public class FormAlterarSenhaMestra : Form
    {
        private TextBox _txtAtual = null!;
        private TextBox _txtNova = null!;
        private TextBox _txtConfirmar = null!;
        private Label _lblErro = null!;

        public string SenhaAtual { get; private set; } = string.Empty;
        public string NovaSenha { get; private set; } = string.Empty;

        public FormAlterarSenhaMestra()
        {
            ConfigurarUI();
        }

        private void ConfigurarUI()
        {
            Text = "Alterar senha mestra";
            Icon = Recursos.IconeApp();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.CardBackground;
            DoubleBuffered = true;
            ClientSize = new Size(440, 388);

            ConfigurarHeader();
            ConfigurarCorpo();
            ConfigurarRodape();

            AplicarCantos();
            Resize += (s, e) => AplicarCantos();
        }

        private void ConfigurarHeader()
        {
            var header = new Panel { Height = 56, Dock = DockStyle.Top, BackColor = Theme.CardBackground };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var lblTitulo = new Label
            {
                Text = Text,
                Font = Theme.SegoeUI(13, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 24,
                Top = 17
            };
            header.Controls.Add(lblTitulo);

            var btnFechar = new Button
            {
                Text = "✕",
                Font = Theme.SegoeUI(11),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Width = 36,
                Height = 36,
                Top = 10,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnFechar.FlatAppearance.BorderSize = 0;
            btnFechar.MouseEnter += (s, e) => btnFechar.BackColor = Theme.HoverBackground;
            btnFechar.MouseLeave += (s, e) => btnFechar.BackColor = Color.Transparent;
            btnFechar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            header.Controls.Add(btnFechar);
            header.Resize += (s, e) => btnFechar.Left = header.Width - btnFechar.Width - 14;

            bool dragging = false; Point start = Point.Empty;
            void Down(object? s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragging = true; start = e.Location; } }
            void Move(object? s, MouseEventArgs e) { if (dragging) Location = new Point(Location.X + e.X - start.X, Location.Y + e.Y - start.Y); }
            void Up(object? s, MouseEventArgs e) => dragging = false;
            header.MouseDown += Down; header.MouseMove += Move; header.MouseUp += Up;
            lblTitulo.MouseDown += Down; lblTitulo.MouseMove += Move; lblTitulo.MouseUp += Up;

            Controls.Add(header);
        }

        private void ConfigurarCorpo()
        {
            var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground, Padding = new Padding(24, 12, 24, 8) };
            int w = ClientSize.Width - 48;
            int y = 14;

            _txtAtual = CriarCampoSenha(corpo, "Senha mestra atual", ref y, w);
            _txtNova = CriarCampoSenha(corpo, "Nova senha mestra", ref y, w);
            _txtConfirmar = CriarCampoSenha(corpo, "Confirmar nova senha", ref y, w);

            _lblErro = new Label
            {
                Text = string.Empty,
                Font = Theme.SegoeUI(9),
                ForeColor = Theme.StrengthWeak,
                AutoSize = false,
                Left = 0,
                Top = y,
                Width = w,
                Height = 20
            };
            corpo.Controls.Add(_lblErro);

            Controls.Add(corpo);
            corpo.BringToFront();
        }

        private TextBox CriarCampoSenha(Panel parent, string label, ref int y, int w)
        {
            var lbl = new Label
            {
                Text = label,
                Font = Theme.SegoeUI(9),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Left = 0,
                Top = y
            };
            parent.Controls.Add(lbl);
            y += 20;

            var box = new Panel { Left = 0, Top = y, Width = w, Height = 38, BackColor = Theme.InputBackground };
            box.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(0, 0, box.Width - 1, box.Height - 1, 8);
                using var pen = new Pen(Theme.InputBorder, 1);
                e.Graphics.DrawPath(pen, path);
            };

            var txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.SegoeUI(10.5f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.InputBackground,
                UseSystemPasswordChar = true,
                Left = 12,
                Width = w - 24,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            txt.Top = Math.Max(0, (38 - txt.PreferredHeight) / 2);
            box.Controls.Add(txt);
            parent.Controls.Add(box);

            y += 38 + 12;
            return txt;
        }

        private void ConfigurarRodape()
        {
            var rodape = new Panel { Height = 72, Dock = DockStyle.Bottom, BackColor = Theme.CardBackground };
            rodape.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, 0, rodape.Width, 0);
            };

            var btnPrincipal = new Button
            {
                Text = "Alterar",
                Font = Theme.SegoeUI(11, FontStyle.Bold),
                BackColor = Theme.AccentPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Left = 24,
                Top = 17,
                Width = 200,
                Height = 40,
                Cursor = Cursors.Hand
            };
            btnPrincipal.FlatAppearance.BorderSize = 0;
            btnPrincipal.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            btnPrincipal.Click += (s, e) => Confirmar();
            rodape.Controls.Add(btnPrincipal);
            AcceptButton = btnPrincipal;

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Font = Theme.SegoeUI(11),
                BackColor = Theme.InputBackground,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Top = 17,
                Width = 150,
                Height = 40,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancelar.FlatAppearance.BorderSize = 1;
            btnCancelar.FlatAppearance.BorderColor = Theme.InputBorder;
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            rodape.Controls.Add(btnCancelar);
            rodape.Resize += (s, e) => btnCancelar.Left = rodape.Width - btnCancelar.Width - 24;

            Controls.Add(rodape);
        }

        private void Confirmar()
        {
            if (string.IsNullOrWhiteSpace(_txtAtual.Text))
            {
                MostrarErro("Informe a senha mestra atual.");
                return;
            }
            if (_txtNova.Text.Length < 8)
            {
                MostrarErro("A nova senha deve ter pelo menos 8 caracteres.");
                return;
            }
            if (_txtNova.Text != _txtConfirmar.Text)
            {
                MostrarErro("A confirmação não coincide com a nova senha.");
                return;
            }

            SenhaAtual = _txtAtual.Text;
            NovaSenha = _txtNova.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void MostrarErro(string mensagem)
        {
            _lblErro.Text = mensagem;
        }

        private void AplicarCantos()
        {
            using var path = RoundedRect(0, 0, Width, Height, 12);
            Region = new Region(path);
        }

        private static GraphicsPath RoundedRect(float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            if (d > width) d = width;
            if (d > height) d = height;
            var arc = new RectangleF(x, y, d, d);
            path.AddArc(arc, 180, 90);
            arc.X = x + width - d; path.AddArc(arc, 270, 90);
            arc.Y = y + height - d; path.AddArc(arc, 0, 90);
            arc.X = x; path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
