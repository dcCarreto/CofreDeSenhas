using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GerenciadorDeSenhas.Servicos;

namespace App
{
    public class FormLogin : Form
    {
        private readonly AutenticacaoMestra _auth;
        private readonly bool _primeiroAcesso;

        private TextBox _txtSenha = null!;
        private TextBox _txtConfirmar = null!;
        private Label _lblErro = null!;
        private Button _btnPrincipal = null!;

        private int _tentativas = 0;

        public byte[]? ChaveDerivada { get; private set; }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public FormLogin(AutenticacaoMestra auth)
        {
            _auth = auth;
            _primeiroAcesso = !auth.ExisteSenhaMestra();
            ConfigurarUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
            const int DWMWCP_ROUND = 2;
            int pref = DWMWCP_ROUND;
            try { DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)); } catch { }
        }

        private void ConfigurarUI()
        {
            this.Text = "Cofre de Senhas";
            this.Icon = Recursos.IconeApp();
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.CardBackground;
            this.DoubleBuffered = true;
            this.ClientSize = new Size(420, _primeiroAcesso ? 560 : 480);
            this.KeyPreview = true;

            int w = 340;
            int x = (this.ClientSize.Width - w) / 2;
            int y = 40;

            var btnFechar = new Button
            {
                Text = "✕",
                Font = Theme.SegoeUI(11),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Width = 34,
                Height = 34,
                Left = this.ClientSize.Width - 42,
                Top = 8,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnFechar.FlatAppearance.BorderSize = 0;
            btnFechar.MouseEnter += (s, e) => btnFechar.BackColor = Theme.HoverBackground;
            btnFechar.MouseLeave += (s, e) => btnFechar.BackColor = Color.Transparent;
            btnFechar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnFechar);

            var logo = new Panel { Left = (this.ClientSize.Width - 64) / 2, Top = y, Width = 64, Height = 64, BackColor = Theme.CardBackground };
            logo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                var img = Recursos.IconeAppBitmap();
                if (img != null)
                    g.DrawImage(img, new Rectangle(0, 0, 64, 64));
            };
            this.Controls.Add(logo);
            y += 80;

            var lblTitulo = new Label
            {
                Text = "Cofre de Senhas",
                Font = Theme.SegoeUI(17, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 0,
                Top = y,
                Width = this.ClientSize.Width,
                Height = 28
            };
            this.Controls.Add(lblTitulo);
            y += 30;

            var lblSub = new Label
            {
                Text = _primeiroAcesso
                    ? "Crie uma senha mestra para proteger o cofre"
                    : "Digite sua senha mestra para desbloquear",
                Font = Theme.SegoeUI(10),
                ForeColor = Theme.TextSecondary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 0,
                Top = y,
                Width = this.ClientSize.Width,
                Height = 22
            };
            this.Controls.Add(lblSub);
            y += 44;

            var lblSenha = new Label { Text = "Senha mestra", Font = Theme.SegoeUI(9), ForeColor = Theme.TextSecondary, AutoSize = true, Left = x, Top = y };
            this.Controls.Add(lblSenha);
            y += 20;
            _txtSenha = CriarCampoSenha(x, y, w);
            this.Controls.Add(_txtSenha.Parent!);
            y += 52;

            if (_primeiroAcesso)
            {
                var lblConfirmar = new Label { Text = "Confirmar senha", Font = Theme.SegoeUI(9), ForeColor = Theme.TextSecondary, AutoSize = true, Left = x, Top = y };
                this.Controls.Add(lblConfirmar);
                y += 20;
                _txtConfirmar = CriarCampoSenha(x, y, w);
                this.Controls.Add(_txtConfirmar.Parent!);
                y += 52;
            }
            else
            {
                _txtConfirmar = new TextBox();
            }

            _lblErro = new Label
            {
                Text = "",
                Font = Theme.SegoeUI(9),
                ForeColor = Theme.StrengthWeak,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Left = x,
                Top = y,
                Width = w,
                Height = 20
            };
            this.Controls.Add(_lblErro);
            y += 26;

            _btnPrincipal = new Button
            {
                Text = _primeiroAcesso ? "Criar cofre" : "Desbloquear",
                Font = Theme.SegoeUI(11, FontStyle.Bold),
                BackColor = Theme.AccentPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Left = x,
                Top = y,
                Width = w,
                Height = 44,
                Cursor = Cursors.Hand
            };
            _btnPrincipal.FlatAppearance.BorderSize = 0;
            _btnPrincipal.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            _btnPrincipal.Click += (s, e) => Confirmar();
            this.Controls.Add(_btnPrincipal);

            this.AcceptButton = _btnPrincipal;

            bool dragging = false; Point start = Point.Empty;
            void Down(object? s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragging = true; start = e.Location; } }
            void Move(object? s, MouseEventArgs e) { if (dragging) this.Location = new Point(this.Location.X + e.X - start.X, this.Location.Y + e.Y - start.Y); }
            void Up(object? s, MouseEventArgs e) => dragging = false;
            this.MouseDown += Down; this.MouseMove += Move; this.MouseUp += Up;
            lblTitulo.MouseDown += Down; lblTitulo.MouseMove += Move; lblTitulo.MouseUp += Up;

            this.Shown += (s, e) => _txtSenha.Focus();
        }

        private TextBox CriarCampoSenha(int x, int y, int w)
        {
            var box = new Panel { Left = x, Top = y, Width = w, Height = 42, BackColor = Theme.InputBackground };
            box.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = Arredondado(0, 0, box.Width - 1, box.Height - 1, 9);
                using var pen = new Pen(Theme.InputBorder, 1);
                g.DrawPath(pen, path);
            };

            var txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.SegoeUI(11),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.InputBackground,
                UseSystemPasswordChar = true,
                Left = 12,
                Top = 12,
                Width = w - 56,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            box.Controls.Add(txt);

            var btnOlho = new Button
            {
                Text = "👁",
                Font = Theme.SegoeUI(10),
                BackColor = Theme.InputBackground,
                ForeColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Width = 34,
                Height = 30,
                Top = 6,
                Left = w - 40,
                Cursor = Cursors.Hand,
                TabStop = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnOlho.FlatAppearance.BorderSize = 0;
            btnOlho.Click += (s, e) => txt.UseSystemPasswordChar = !txt.UseSystemPasswordChar;
            box.Controls.Add(btnOlho);

            return txt;
        }

        private void Confirmar()
        {
            _lblErro.Text = "";
            var senha = _txtSenha.Text;

            if (_primeiroAcesso)
            {
                if (senha.Length < 8)
                {
                    MostrarErro("A senha deve ter pelo menos 8 caracteres.");
                    return;
                }
                if (senha != _txtConfirmar.Text)
                {
                    MostrarErro("As senhas não coincidem.");
                    return;
                }
                try
                {
                    ChaveDerivada = _auth.CriarSenhaMestra(senha);
                    QrCodeUtil.OferecerSalvarBackup(this, senha);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MostrarErro(ex.Message);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(senha))
                {
                    MostrarErro("Digite a senha mestra.");
                    return;
                }
                var chave = _auth.Autenticar(senha);
                if (chave != null)
                {
                    ChaveDerivada = chave;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    _tentativas++;
                    if (_tentativas >= 5)
                    {
                        MostrarErro("Muitas tentativas. Aguarde 5 segundos.");
                        _btnPrincipal.Enabled = false;
                        var t = new System.Windows.Forms.Timer { Interval = 5000 };
                        t.Tick += (s, e) => { _btnPrincipal.Enabled = true; _tentativas = 0; _lblErro.Text = ""; t.Stop(); t.Dispose(); };
                        t.Start();
                    }
                    else
                    {
                        MostrarErro($"Senha incorreta. Tentativa {_tentativas} de 5.");
                    }
                    _txtSenha.SelectAll();
                    _txtSenha.Focus();
                }
            }
        }

        private void MostrarErro(string msg)
        {
            _lblErro.Text = msg;
        }

        private static GraphicsPath Arredondado(float x, float y, float w, float h, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            if (d > w) d = w;
            if (d > h) d = h;
            var arc = new RectangleF(x, y, d, d);
            path.AddArc(arc, 180, 90);
            arc.X = x + w - d; path.AddArc(arc, 270, 90);
            arc.Y = y + h - d; path.AddArc(arc, 0, 90);
            arc.X = x; path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
