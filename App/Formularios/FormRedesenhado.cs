using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace App
{
    public partial class FormRedesenhado : Form
    {
        private readonly IServicoSenha _servicoSenha;
        private readonly IServicoCriptografia? _criptografia;
        private List<Senha> _senhasAtuais = new();

        private FlowLayoutPanel _flowGerador = null!;
        private TextBox _txtSenhaGerada = null!;
        private CustomSlider _sliderTamanho = null!;
        private CustomSlider _sliderQuantidade = null!;
        private Label _lblForça = null!;
        private CustomToggle _toggleMaiusculas = null!;
        private CustomToggle _toggleMinusculas = null!;
        private CustomToggle _toggleNumeros = null!;
        private CustomToggle _toggleEspeciais = null!;
        private Panel _pnlForcaBarra = null!;
        private Label _lblTamanhoValor = null!;
        private Label _lblQuantidadeValor = null!;
        private int _nivelForca = 0;
        private Color _corForca = Theme.StrengthWeak;
        private List<string> _senhasGeradas = new();

        private TextBox _txtBusca = null!;
        private ComboBox _cbCategoria = null!;
        private Button _btnFiltroFavoritos = null!;
        private Panel _pnlListaSenhas = null!;
        private Label _lblVazio = null!;
        private Label _lblContadorHeader = null!;
        private Label _lblStatusCriptografia = null!;
        private bool _somenteFavoritos = false;
        private readonly List<LinhaSenha> _linhasSenha = new();
        private readonly ServicoVazamento _servicoVazamento = new();
        private readonly ServicoExportacao _servicoExportacao = new();
        private Button _btnVerificarVazamento = null!;

        private string _senhaGerada = "";
        private bool _mostrarSenha = true;

        private NotifyIcon? _trayIcon;
        private bool _avisouBandeja = false;

        public FormRedesenhado(IServicoSenha servicoSenha, IServicoCriptografia? criptografia = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _criptografia = criptografia;

            this.Text = "Cofre de Senhas";
            this.Icon = Recursos.IconeApp();
            this.ClientSize = new Size(1160, 720);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.WorkspaceBackground;
            this.DoubleBuffered = true;
            this.MinimumSize = new Size(900, 600);

            ConfigurarBarraTitulo();
            ConfigurarUI();
            ConfigurarBandeja();
        }

        private void ConfigurarBandeja()
        {
            var menu = new ContextMenuStrip();
            var itemAbrir = new ToolStripMenuItem("Abrir cofre");
            itemAbrir.Font = new Font(itemAbrir.Font, FontStyle.Bold);
            itemAbrir.Click += (s, e) => RestaurarDaBandeja();
            var itemSair = new ToolStripMenuItem("Sair");
            itemSair.Click += (s, e) => this.Close();
            menu.Items.Add(itemAbrir);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemSair);

            _trayIcon = new NotifyIcon
            {
                Icon = Recursos.IconeApp() ?? System.Drawing.SystemIcons.Application,
                Text = "Cofre de Senhas",
                Visible = true,
                ContextMenuStrip = menu
            };
            _trayIcon.DoubleClick += (s, e) => RestaurarDaBandeja();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (WindowState == FormWindowState.Minimized && _trayIcon != null)
                MinimizarParaBandeja();
        }

        private void MinimizarParaBandeja()
        {
            Hide();
            if (!_avisouBandeja)
            {
                _avisouBandeja = true;
                _trayIcon?.ShowBalloonTip(2500, "Cofre de Senhas",
                    "O cofre continua aberto aqui na bandeja. Clique no ícone para reabrir.",
                    ToolTipIcon.Info);
            }
        }

        private void RestaurarDaBandeja()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        private const int WM_NCHITTEST = 0x0084;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int SC_SIZE = 0xF000;
        private const int HTCLIENT = 1, HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13,
            HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
        public const int BordaResize = 8;
        private const int AlturaBarra = 44;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private struct MARGINS { public int Left, Right, Top, Bottom; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
            const int DWMWCP_ROUND = 2;
            int pref = DWMWCP_ROUND;
            try { DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)); } catch { }

            try { var mg = new MARGINS { Top = 1 }; DwmExtendFrameIntoClientArea(Handle, ref mg); } catch { }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                int code = HitTestBorda(m.LParam);
                if (code != HTCLIENT)
                {
                    m.Result = (IntPtr)code;
                    return;
                }
            }

            if (m.Msg == WM_NCLBUTTONDOWN && WindowState == FormWindowState.Normal)
            {
                int ht = (int)m.WParam;
                if (ht >= HTLEFT && ht <= HTBOTTOMRIGHT)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_SYSCOMMAND, (IntPtr)(SC_SIZE + (ht - 9)), IntPtr.Zero);
                    return;
                }
            }

            if (m.Msg == WM_GETMINMAXINFO)
            {
                AjustarMaxInfo(m.LParam);
                base.WndProc(ref m);
                return;
            }

            base.WndProc(ref m);
        }

        private int HitTestBorda(IntPtr lParam)
        {
            int x = unchecked((short)(long)lParam);
            int y = unchecked((short)((long)lParam >> 16));
            var p = PointToClient(new Point(x, y));
            int w = ClientSize.Width, h = ClientSize.Height, b = BordaResize;
            bool left = p.X <= b, right = p.X >= w - b, top = p.Y <= b, bottom = p.Y >= h - b;
            if (left && top) return HTTOPLEFT;
            if (right && top) return HTTOPRIGHT;
            if (left && bottom) return HTBOTTOMLEFT;
            if (right && bottom) return HTBOTTOMRIGHT;
            if (left) return HTLEFT;
            if (right) return HTRIGHT;
            if (top) return HTTOP;
            if (bottom) return HTBOTTOM;
            return HTCLIENT;
        }

        private void AjustarMaxInfo(IntPtr lParam)
        {
            var mmi = System.Runtime.InteropServices.Marshal.PtrToStructure<MINMAXINFO>(lParam);
            var tela = Screen.FromHandle(Handle);
            var wa = tela.WorkingArea;
            var b = tela.Bounds;
            mmi.ptMaxPosition = new POINT { x = wa.Left - b.Left, y = wa.Top - b.Top };
            mmi.ptMaxSize = new POINT { x = wa.Width, y = wa.Height };
            mmi.ptMinTrackSize = new POINT { x = MinimumSize.Width, y = MinimumSize.Height };
            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, false);
        }

        private struct POINT { public int x; public int y; }

#pragma warning disable CS0649
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
#pragma warning restore CS0649

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ = CarregarSenhas();
        }

        private void AlternarTema()
        {
            Theme.DefinirModo(!Theme.ModoEscuro);
            Preferencias.ModoEscuro = Theme.ModoEscuro;
            Preferencias.Salvar();
            RecriarInterface();
        }

        private void RecriarInterface()
        {
            this.SuspendLayout();
            _linhasSenha.Clear();

            var antigos = this.Controls.Cast<Control>().ToArray();
            this.Controls.Clear();
            foreach (var c in antigos) c.Dispose();

            this.BackColor = Theme.WorkspaceBackground;
            ConfigurarBarraTitulo();
            ConfigurarUI();
            this.ResumeLayout();

            _ = CarregarSenhas();
        }

        private void ConfigurarBarraTitulo()
        {
            const int barHeight = AlturaBarra;
            var pnlTitle = new BorderlessHostPanel
            {
                Height = barHeight,
                Dock = DockStyle.Top,
                BackColor = Theme.TitleBar
            };

            pnlTitle.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.TitleBarBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlTitle.Height - 1, pnlTitle.Width, pnlTitle.Height - 1);
                }
            };

            var pnlLogo = new Panel
            {
                Left = 14,
                Top = (barHeight - 26) / 2,
                Width = 26,
                Height = 26,
                BackColor = Theme.TitleBar
            };
            pnlLogo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                var img = Recursos.IconeAppBitmap();
                if (img != null)
                    g.DrawImage(img, new Rectangle(0, 0, pnlLogo.Width, pnlLogo.Height));
            };
            pnlTitle.Controls.Add(pnlLogo);

            var lblTitle = new Label
            {
                Text = "Cofre de Senhas",
                Font = Theme.SegoeUI(12, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Left = 46,
                Top = 11,
                Width = 160,
                Height = 22
            };
            pnlTitle.Controls.Add(lblTitle);

            int btnWidth = 40;
            int btnHeight = 32;
            int rightStart = 5;

            var btnFechar = new Button
            {
                Text = "✕",
                Font = Theme.SegoeUI(16),
                ForeColor = Color.White,
                BackColor = Theme.CloseButtonHover,
                FlatStyle = FlatStyle.Flat,
                Left = pnlTitle.Width - btnWidth - rightStart,
                Top = (barHeight - btnHeight) / 2,
                Width = btnWidth,
                Height = btnHeight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnFechar.FlatAppearance.BorderSize = 0;
            btnFechar.Click += (s, e) => this.Close();
            pnlTitle.Controls.Add(btnFechar);

            var btnMaximizar = new Button
            {
                Text = "□",
                Font = Theme.SegoeUI(14),
                ForeColor = Theme.TextPrimary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Left = pnlTitle.Width - (btnWidth * 2) - rightStart,
                Top = (barHeight - btnHeight) / 2,
                Width = btnWidth,
                Height = btnHeight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMaximizar.FlatAppearance.BorderSize = 0;
            btnMaximizar.MouseEnter += (s, e) => btnMaximizar.BackColor = Theme.HoverBackground;
            btnMaximizar.MouseLeave += (s, e) => btnMaximizar.BackColor = Color.Transparent;
            btnMaximizar.Click += (s, e) => this.WindowState = (this.WindowState == FormWindowState.Maximized) ? FormWindowState.Normal : FormWindowState.Maximized;
            pnlTitle.Controls.Add(btnMaximizar);

            var btnMinimizar = new Button
            {
                Text = "−",
                Font = Theme.SegoeUI(16),
                ForeColor = Theme.TextPrimary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Left = pnlTitle.Width - (btnWidth * 3) - rightStart,
                Top = (barHeight - btnHeight) / 2,
                Width = btnWidth,
                Height = btnHeight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMinimizar.FlatAppearance.BorderSize = 0;
            btnMinimizar.MouseEnter += (s, e) => btnMinimizar.BackColor = Theme.HoverBackground;
            btnMinimizar.MouseLeave += (s, e) => btnMinimizar.BackColor = Color.Transparent;
            btnMinimizar.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            pnlTitle.Controls.Add(btnMinimizar);

            var btnTema = new Button
            {
                Text = Theme.ModoEscuro ? "☀" : "🌙",
                Font = Theme.SegoeUI(12),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Left = pnlTitle.Width - (btnWidth * 4) - rightStart,
                Top = (barHeight - btnHeight) / 2,
                Width = btnWidth,
                Height = btnHeight,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnTema.FlatAppearance.BorderSize = 0;
            btnTema.MouseEnter += (s, e) => btnTema.BackColor = Theme.HoverBackground;
            btnTema.MouseLeave += (s, e) => btnTema.BackColor = Color.Transparent;
            var tipTema = new ToolTip();
            tipTema.SetToolTip(btnTema, Theme.ModoEscuro ? "Tema claro" : "Tema escuro");
            btnTema.Click += (s, e) => AlternarTema();
            pnlTitle.Controls.Add(btnTema);

            var btnConfig = new Button
            {
                Text = "⚙",
                Font = Theme.SegoeUI(13),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Left = pnlTitle.Width - (btnWidth * 5) - rightStart,
                Top = (barHeight - btnHeight) / 2,
                Width = btnWidth,
                Height = btnHeight,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnConfig.FlatAppearance.BorderSize = 0;
            btnConfig.MouseEnter += (s, e) => btnConfig.BackColor = Theme.HoverBackground;
            btnConfig.MouseLeave += (s, e) => btnConfig.BackColor = Color.Transparent;
            var tipConfig = new ToolTip();
            tipConfig.SetToolTip(btnConfig, "Configurações");

            var menuConfig = new ContextMenuStrip();
            var itemAlterarSenha = new ToolStripMenuItem("Alterar senha mestra…");
            itemAlterarSenha.Click += (s, e) => _ = AlterarSenhaMestraAsync();
            menuConfig.Items.Add(itemAlterarSenha);
            btnConfig.Click += (s, e) => menuConfig.Show(btnConfig, new Point(0, btnConfig.Height));
            pnlTitle.Controls.Add(btnConfig);

            bool dragging = false;
            Point dragStart = Point.Empty;

            pnlTitle.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && this.WindowState != FormWindowState.Maximized)
                {
                    dragging = true;
                    dragStart = e.Location;
                }
            };

            pnlTitle.MouseMove += (s, e) =>
            {
                if (dragging)
                {
                    this.Location = new Point(this.Location.X + e.X - dragStart.X, this.Location.Y + e.Y - dragStart.Y);
                }
            };

            pnlTitle.MouseUp += (s, e) => dragging = false;

            this.Controls.Add(pnlTitle);
        }

        private void ConfigurarUI()
        {
            var fundo = new BorderlessHostPanel { Dock = DockStyle.Fill, BackColor = Theme.WorkspaceBackground };
            HabilitarDoubleBuffer(fundo);

            var pnlGerador = CriarPainelGerador();
            var pnlCofre = CriarPainelCofre();
            fundo.Controls.Add(pnlGerador);
            fundo.Controls.Add(pnlCofre);

            const int gap = 18;
            void Reposicionar()
            {
                int w = fundo.ClientSize.Width;
                int h = fundo.ClientSize.Height;
                if (w <= 0 || h <= 0) return;

                int topoBarra = fundo.Top < AlturaBarra ? AlturaBarra : 0;
                int disponivel = w - gap * 3;
                int wGer = Math.Max(280, (int)(disponivel * 0.30));
                int wCof = disponivel - wGer;
                int top = topoBarra + gap;
                int altura = h - topoBarra - gap * 2;
                pnlGerador.SetBounds(gap, top, wGer, altura);
                pnlCofre.SetBounds(gap * 2 + wGer, top, wCof, altura);
                AplicarRegiaoArredondada(pnlGerador, 12);
                AplicarRegiaoArredondada(pnlCofre, 12);
            }

            fundo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DesenharSombraCard(e.Graphics, pnlGerador.Bounds);
                DesenharSombraCard(e.Graphics, pnlCofre.Bounds);
            };
            fundo.Resize += (s, e) => { Reposicionar(); fundo.Invalidate(); };

            this.Controls.Add(fundo);

            fundo.SendToBack();
            fundo.HandleCreated += (s, e) => Reposicionar();
            Reposicionar();
        }

        private void AplicarRegiaoArredondada(Control ctrl, int radius)
        {
            if (ctrl.Width <= 0 || ctrl.Height <= 0) return;
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(ctrl.Width - d, 0, d, d, 270, 90);
            path.AddArc(ctrl.Width - d, ctrl.Height - d, d, d, 0, 90);
            path.AddArc(0, ctrl.Height - d, d, d, 90, 90);
            path.CloseFigure();
            var antiga = ctrl.Region;
            ctrl.Region = new Region(path);
            antiga?.Dispose();
            path.Dispose();
        }

        private static void HabilitarDoubleBuffer(Control c)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(c, true);
        }

        private void DesenharSombraCard(Graphics g, Rectangle card)
        {
            const int camadas = 14;
            for (int i = camadas; i >= 1; i--)
            {
                int spread = i;
                int offsetY = 3;
                var r = new Rectangle(
                    card.X - spread,
                    card.Y - spread + offsetY,
                    card.Width + spread * 2,
                    card.Height + spread * 2);
                using var path = RoundedRectangle(r.X, r.Y, r.Width, r.Height, 12 + spread);
                using var brush = new SolidBrush(Color.FromArgb(9, 38, 40, 64));
                g.FillPath(brush, path);
            }
        }

        private Panel CriarPainelGerador()
        {
            var pnl = new Panel { BackColor = Theme.CardBackground };
            AplicarEstiloPainel(pnl);

            _flowGerador = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(24, 22, 24, 22),
                BackColor = Theme.CardBackground
            };
            var flow = _flowGerador;

            var lblTitulo = new Label { Text = "Gerador de senha", Font = Theme.SegoeUI(15, FontStyle.Bold), ForeColor = Theme.TextPrimary, AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
            flow.Controls.Add(lblTitulo);

            var lblSub = new Label { Text = "Crie senhas fortes e únicas", Font = Theme.SegoeUI(9.5f), ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(0, 0, 0, 16) };
            flow.Controls.Add(lblSub);

            var corSenhaBg = Theme.InputBackground;
            var pnlSenha = new Panel { Height = 60, BackColor = corSenhaBg, Margin = new Padding(0, 0, 0, 18) };
            AplicarBordaArredondada(pnlSenha, Theme.InputBorder, 11);

            _txtSenhaGerada = new TextBox
            {
                BackColor = corSenhaBg,
                BorderStyle = BorderStyle.None,
                Font = Theme.Consolas(14, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                ReadOnly = true,
                Left = 16,
                Top = 19,
                Width = 100,
                Height = 26
            };
            pnlSenha.Controls.Add(_txtSenhaGerada);

            var btnRegenerar = CriarBotaoIcon("⟳", 0, 14, 32, 32);
            btnRegenerar.BackColor = corSenhaBg;
            btnRegenerar.Font = Theme.SegoeUI(13);
            btnRegenerar.Click += (s, e) => GerarSenha();
            pnlSenha.Controls.Add(btnRegenerar);

            var btnCopiar = CriarBotaoIcon("⧉", 0, 14, 32, 32);
            btnCopiar.BackColor = corSenhaBg;
            btnCopiar.Click += (s, e) => CopiarSenha();
            pnlSenha.Controls.Add(btnCopiar);

            var btnOlho = CriarBotaoIcon("👁", 0, 14, 32, 32);
            btnOlho.BackColor = corSenhaBg;
            btnOlho.Click += (s, e) => AlternarVisibilidadeSenha();
            pnlSenha.Controls.Add(btnOlho);

            pnlSenha.Resize += (s, e) =>
            {
                btnRegenerar.Left = pnlSenha.Width - btnRegenerar.Width - 8;
                btnCopiar.Left = btnRegenerar.Left - btnCopiar.Width - 2;
                btnOlho.Left = btnCopiar.Left - btnOlho.Width - 2;
                _txtSenhaGerada.Width = btnOlho.Left - _txtSenhaGerada.Left - 6;
            };
            flow.Controls.Add(pnlSenha);

            var pnlForcaHeader = new Panel { Height = 20, Margin = new Padding(0, 0, 0, 6) };
            var lblForcaLabel = new Label { Text = "Força da senha", Font = Theme.SegoeUI(9.5f), ForeColor = Theme.TextSecondary, AutoSize = true, Left = 0, Top = 2 };
            pnlForcaHeader.Controls.Add(lblForcaLabel);
            _lblForça = new Label { Text = "—", Font = Theme.SegoeUI(9.5f, FontStyle.Bold), ForeColor = Theme.TextSecondary, AutoSize = false, Width = 120, Height = 18, Top = 1, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            pnlForcaHeader.Controls.Add(_lblForça);
            pnlForcaHeader.Resize += (s, e) => { _lblForça.Left = pnlForcaHeader.Width - _lblForça.Width; };
            flow.Controls.Add(pnlForcaHeader);

            _pnlForcaBarra = new Panel { Height = 6, Margin = new Padding(0, 0, 0, 20), BackColor = Theme.CardBackground };
            HabilitarDoubleBuffer(_pnlForcaBarra);
            _pnlForcaBarra.Paint += DesenharBarraForca;
            _pnlForcaBarra.Resize += (s, e) => _pnlForcaBarra.Invalidate();
            flow.Controls.Add(_pnlForcaBarra);

            var pnlComp = new Panel { Height = 22, Margin = new Padding(0, 0, 0, 4) };
            var lblComp = new Label { Text = "Comprimento", Font = Theme.SegoeUI(9.5f), ForeColor = Theme.TextSecondary, AutoSize = true, Left = 0, Top = 2 };
            pnlComp.Controls.Add(lblComp);
            _lblTamanhoValor = new Label { Text = "12", Font = Theme.Consolas(10, FontStyle.Bold), ForeColor = Theme.AccentPrimary, AutoSize = false, Width = 50, Height = 18, Top = 1, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            pnlComp.Controls.Add(_lblTamanhoValor);
            pnlComp.Resize += (s, e) => { _lblTamanhoValor.Left = pnlComp.Width - _lblTamanhoValor.Width; };
            flow.Controls.Add(pnlComp);

            _sliderTamanho = new CustomSlider { Height = 24, Value = 12, Minimum = 4, Maximum = 64, Margin = new Padding(0, 0, 0, 18) };
            _sliderTamanho.ValueChanged += (s, e) => _lblTamanhoValor.Text = _sliderTamanho.Value.ToString();
            flow.Controls.Add(_sliderTamanho);

            var pnlQtd = new Panel { Height = 22, Margin = new Padding(0, 0, 0, 4) };
            var lblQtd = new Label { Text = "Quantidade de senhas", Font = Theme.SegoeUI(9.5f), ForeColor = Theme.TextSecondary, AutoSize = true, Left = 0, Top = 2 };
            pnlQtd.Controls.Add(lblQtd);
            _lblQuantidadeValor = new Label { Text = "1", Font = Theme.Consolas(10, FontStyle.Bold), ForeColor = Theme.AccentPrimary, AutoSize = false, Width = 50, Height = 18, Top = 1, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            pnlQtd.Controls.Add(_lblQuantidadeValor);
            pnlQtd.Resize += (s, e) => { _lblQuantidadeValor.Left = pnlQtd.Width - _lblQuantidadeValor.Width; };
            flow.Controls.Add(pnlQtd);

            _sliderQuantidade = new CustomSlider { Height = 24, Value = 1, Minimum = 1, Maximum = 10, Margin = new Padding(0, 0, 0, 18) };
            _sliderQuantidade.ValueChanged += (s, e) => _lblQuantidadeValor.Text = _sliderQuantidade.Value.ToString();
            flow.Controls.Add(_sliderQuantidade);

            _toggleMaiusculas = AdicionarToggle(flow, "Letras maiúsculas (A-Z)", true);
            _toggleMinusculas = AdicionarToggle(flow, "Letras minúsculas (a-z)", true);
            _toggleNumeros = AdicionarToggle(flow, "Números (0-9)", true);
            _toggleEspeciais = AdicionarToggle(flow, "Caracteres especiais (!@#)", false, marginBottom: 22);

            var btnGerar = new Button
            {
                Text = "Gerar nova senha",
                Font = Theme.SegoeUI(11, FontStyle.Bold),
                BackColor = Theme.AccentPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Height = 44,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };
            btnGerar.FlatAppearance.BorderSize = 0;
            btnGerar.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            btnGerar.Click += (s, e) => GerarSenha();
            flow.Controls.Add(btnGerar);

            var pnlAcoes = new Panel { Height = 40, Margin = new Padding(0) };
            var btnSalvar = new Button
            {
                Text = "Salvar no cofre",
                Font = Theme.SegoeUI(10),
                BackColor = Theme.CardBackground,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Left = 0,
                Top = 0,
                Height = 38,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            btnSalvar.FlatAppearance.BorderSize = 1;
            btnSalvar.FlatAppearance.BorderColor = Theme.InputBorder;
            btnSalvar.Click += (s, e) => SalvarNoCovre();
            pnlAcoes.Controls.Add(btnSalvar);

            var btnLimpar = new Button
            {
                Text = "Limpar",
                Font = Theme.SegoeUI(10),
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Top = 0,
                Width = 90,
                Height = 38,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnLimpar.FlatAppearance.BorderSize = 0;
            btnLimpar.Click += (s, e) =>
            {
                _txtSenhaGerada.Text = "";
                _senhaGerada = "";
                _mostrarSenha = true;
                _nivelForca = 0;
                _lblForça.Text = "—";
                _lblForça.ForeColor = Theme.TextSecondary;
                _pnlForcaBarra.Invalidate();
                _senhasGeradas.Clear();
                AtualizarListaSenhasGeradas();
            };
            pnlAcoes.Controls.Add(btnLimpar);

            pnlAcoes.Resize += (s, e) =>
            {
                btnLimpar.Left = pnlAcoes.Width - btnLimpar.Width;
                btnSalvar.Width = pnlAcoes.Width - btnLimpar.Width - 10;
            };
            flow.Controls.Add(pnlAcoes);

            void AjustarLargura()
            {
                int w = flow.ClientSize.Width - flow.Padding.Left - flow.Padding.Right;
                if (w <= 0) return;
                foreach (Control c in flow.Controls)
                {
                    if (c is Label && c.AutoSize) continue;
                    c.Width = w;
                }
            }
            flow.Resize += (s, e) => AjustarLargura();
            flow.HandleCreated += (s, e) => AjustarLargura();

            pnl.Controls.Add(flow);
            return pnl;
        }

        private void DesenharBarraForca(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int segmentos = 4;
            int gap = 6;
            int larguraTotal = _pnlForcaBarra.Width;
            int larguraSegmento = (larguraTotal - gap * (segmentos - 1)) / segmentos;
            int h = _pnlForcaBarra.Height;

            for (int i = 0; i < segmentos; i++)
            {
                int x = i * (larguraSegmento + gap);
                Color cor = (i < _nivelForca) ? _corForca : Theme.TrailInactive;
                using (var path = RoundedRectangle(x, 0, larguraSegmento, h, h / 2f))
                using (var brush = new SolidBrush(cor))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        private CustomToggle AdicionarToggle(FlowLayoutPanel flow, string label, bool padrao, int marginBottom = 6)
        {
            var pnl = new Panel { Height = 30, Margin = new Padding(0, 0, 0, marginBottom) };
            var lbl = new Label { Text = label, Font = Theme.SegoeUI(10), ForeColor = Theme.TextPrimary, AutoSize = false, Left = 0, Top = 0, Width = 200, Height = 30, TextAlign = ContentAlignment.MiddleLeft };
            pnl.Controls.Add(lbl);
            var toggle = new CustomToggle { Checked = padrao, Width = 46, Height = 24, Top = 3, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            pnl.Controls.Add(toggle);
            pnl.Resize += (s, e) => { toggle.Left = pnl.Width - toggle.Width; };
            flow.Controls.Add(pnl);
            return toggle;
        }

        private void AplicarBordaArredondada(Panel pnl, Color borderColor, int radius)
        {
            HabilitarDoubleBuffer(pnl);
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRectangle(0, 0, pnl.Width - 1, pnl.Height - 1, radius))
                using (var pen = new Pen(borderColor, 1))
                {
                    g.DrawPath(pen, path);
                }
            };

            pnl.Resize += (s, e) => pnl.Invalidate();
        }

        private Panel CriarPainelCofre()
        {
            var pnl = new Panel { BackColor = Theme.CardBackground, Padding = new Padding(24, 22, 24, 0) };
            AplicarEstiloPainel(pnl);

            var pnlRodape = new Panel { Height = 42, Dock = DockStyle.Bottom, BackColor = Theme.Footer };
            pnlRodape.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawLine(pen, 0, 0, pnlRodape.Width, 0);
            };
            _lblStatusCriptografia = new Label
            {
                Text = "0 senhas • 0 favoritas",
                Font = Theme.SegoeUI(9),
                ForeColor = Theme.TextSecondary,
                AutoSize = false,
                Left = 2,
                Top = 0,
                Height = 42,
                Width = 240,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlRodape.Controls.Add(_lblStatusCriptografia);

            var lblCripto = new Label
            {
                Text = "Cofre criptografado",
                Font = Theme.SegoeUI(9),
                ForeColor = Theme.TextSecondary,
                AutoSize = false,
                Top = 0,
                Height = 42,
                Width = 130,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            pnlRodape.Controls.Add(lblCripto);
            var pontoVerde = new Panel
            {
                Width = 8,
                Height = 8,
                BackColor = Color.FromArgb(34, 197, 94),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            pontoVerde.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var b = new SolidBrush(Color.FromArgb(34, 197, 94));
                e.Graphics.FillEllipse(b, 0, 0, 7, 7);
            };
            pontoVerde.BackColor = pnlRodape.BackColor;
            pnlRodape.Controls.Add(pontoVerde);
            pnlRodape.Resize += (s, e) =>
            {
                lblCripto.Left = pnlRodape.Width - lblCripto.Width - 2;
                pontoVerde.Left = lblCripto.Left - 12;
                pontoVerde.Top = (pnlRodape.Height - 8) / 2;
            };
            pnl.Controls.Add(pnlRodape);

            var pnlListaContainer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground };

            var pnlColunas = new Panel { Height = 30, Dock = DockStyle.Top, BackColor = Theme.CardBackground };
            pnlColunas.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Separator, 1);
                e.Graphics.DrawLine(pen, 0, pnlColunas.Height - 1, pnlColunas.Width, pnlColunas.Height - 1);
            };
            var colServico = new Label { Text = "SERVIÇO", Font = Theme.SegoeUI(8, FontStyle.Bold), ForeColor = Theme.TextHeader, AutoSize = false, Left = 92, Top = 0, Height = 30, Width = 200, TextAlign = ContentAlignment.MiddleLeft };
            var colAcoes = new Label { Text = "AÇÕES", Font = Theme.SegoeUI(8, FontStyle.Bold), ForeColor = Theme.TextHeader, AutoSize = false, Top = 0, Height = 30, Width = 100, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            pnlColunas.Controls.Add(colServico);
            pnlColunas.Controls.Add(colAcoes);
            pnlColunas.Resize += (s, e) => { colAcoes.Left = pnlColunas.Width - 104; };

            _pnlListaSenhas = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground, AutoScroll = true };

            _lblVazio = new Label
            {
                Text = "Nenhuma senha no cofre.\nClique em \"Nova senha\" para começar.",
                Font = Theme.SegoeUI(10),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            _pnlListaSenhas.Controls.Add(_lblVazio);

            pnlListaContainer.Controls.Add(_pnlListaSenhas);
            pnlListaContainer.Controls.Add(pnlColunas);
            pnl.Controls.Add(pnlListaContainer);

            var pnlFiltros = new Panel { Height = 46, Dock = DockStyle.Top, BackColor = Theme.CardBackground };
            var corCampo = Theme.InputBackground;
            var corBorda = Theme.InputBorder;

            var boxCat = new Panel { Left = 0, Top = 6, Width = 140, Height = 34, BackColor = corCampo, Anchor = AnchorStyles.Left | AnchorStyles.Top };
            AplicarBordaArredondada(boxCat, corBorda, 8);
            _cbCategoria = new ComboBoxTema
            {
                Font = Theme.SegoeUI(9.5f),
                Left = 8,
                Top = 7,
                Width = 124,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            _cbCategoria.Items.Add("Todas");
            _cbCategoria.Items.AddRange(CategoriasUI.Rotulos);
            _cbCategoria.SelectedIndex = 0;
            _cbCategoria.SelectedIndexChanged += (s, e) => FiltrarSenhas();
            boxCat.Controls.Add(_cbCategoria);
            pnlFiltros.Controls.Add(boxCat);

            var boxBusca = new Panel { Left = 150, Top = 6, Width = 100, Height = 34, BackColor = corCampo, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
            AplicarBordaArredondada(boxBusca, corBorda, 8);
            var lblLupa = new Label { Text = "🔍", Font = Theme.SegoeUI(9), ForeColor = Theme.TextSecondary, AutoSize = false, Left = 8, Top = 0, Width = 24, Height = 34, TextAlign = ContentAlignment.MiddleCenter, BackColor = corCampo };
            boxBusca.Controls.Add(lblLupa);
            _txtBusca = new TextBox
            {
                BackColor = corCampo,
                BorderStyle = BorderStyle.None,
                Font = Theme.SegoeUI(10),
                Left = 34,
                Top = 9,
                Width = 60,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _txtBusca.TextChanged += (s, e) => FiltrarSenhas();
            boxBusca.Controls.Add(_txtBusca);
            boxBusca.Resize += (s, e) => { _txtBusca.Width = boxBusca.Width - _txtBusca.Left - 10; };
            pnlFiltros.Controls.Add(boxBusca);

            _btnFiltroFavoritos = new Button
            {
                Text = "★",
                Font = Theme.SegoeUI(12),
                BackColor = corCampo,
                ForeColor = Theme.FavoriteBorderColor,
                FlatStyle = FlatStyle.Flat,
                Top = 6,
                Width = 40,
                Height = 34,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnFiltroFavoritos.FlatAppearance.BorderSize = 1;
            _btnFiltroFavoritos.FlatAppearance.BorderColor = corBorda;
            _btnFiltroFavoritos.Click += (s, e) => AlternarFiltroFavoritos();
            pnlFiltros.Controls.Add(_btnFiltroFavoritos);

            _btnVerificarVazamento = new Button
            {
                Text = "🛡",
                Font = Theme.SegoeUI(12),
                BackColor = corCampo,
                ForeColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Top = 6,
                Width = 40,
                Height = 34,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnVerificarVazamento.FlatAppearance.BorderSize = 1;
            _btnVerificarVazamento.FlatAppearance.BorderColor = corBorda;
            var tipVaz = new ToolTip();
            tipVaz.SetToolTip(_btnVerificarVazamento, "Verificar senhas vazadas (Have I Been Pwned)");
            _btnVerificarVazamento.Click += (s, e) => _ = VerificarVazamentosAsync();
            pnlFiltros.Controls.Add(_btnVerificarVazamento);

            pnlFiltros.Resize += (s, e) =>
            {
                _btnFiltroFavoritos.Left = pnlFiltros.Width - _btnFiltroFavoritos.Width;
                _btnVerificarVazamento.Left = _btnFiltroFavoritos.Left - _btnVerificarVazamento.Width - 6;
                boxBusca.Width = _btnVerificarVazamento.Left - 10 - boxBusca.Left;
            };
            pnl.Controls.Add(pnlFiltros);

            var pnlHeader = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = Theme.CardBackground };
            var lblCofre = new Label
            {
                Text = "Cofre de senhas",
                Font = Theme.SegoeUI(15, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 0,
                Top = 9
            };
            pnlHeader.Controls.Add(lblCofre);

            _lblContadorHeader = new Label
            {
                Text = "0 itens",
                Font = Theme.SegoeUI(10),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Left = 178,
                Top = 14
            };
            pnlHeader.Controls.Add(_lblContadorHeader);

            var btnNova = new Button
            {
                Text = "+  Nova senha",
                Font = Theme.SegoeUI(10, FontStyle.Bold),
                BackColor = Theme.AccentPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Top = 4,
                Width = 130,
                Height = 38,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnNova.FlatAppearance.BorderSize = 0;
            btnNova.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            btnNova.Click += (s, e) => CriarNovaSenha();
            pnlHeader.Controls.Add(btnNova);

            var btnImportar = CriarBotaoHeaderSecundario("📥");
            var tipImp = new ToolTip();
            tipImp.SetToolTip(btnImportar, "Importar senhas de um arquivo");
            btnImportar.Click += (s, e) => _ = ImportarSenhasAsync();
            pnlHeader.Controls.Add(btnImportar);

            var btnExportar = CriarBotaoHeaderSecundario("📤");
            var tipExp = new ToolTip();
            tipExp.SetToolTip(btnExportar, "Exportar senhas para um arquivo criptografado");
            btnExportar.Click += (s, e) => _ = ExportarSenhasAsync();
            pnlHeader.Controls.Add(btnExportar);

            pnlHeader.Resize += (s, e) =>
            {
                btnNova.Left = pnlHeader.Width - btnNova.Width;
                btnExportar.Left = btnNova.Left - btnExportar.Width - 8;
                btnImportar.Left = btnExportar.Left - btnImportar.Width - 6;
            };
            pnl.Controls.Add(pnlHeader);

            return pnl;
        }

        private Button CriarBotaoHeaderSecundario(string icon)
        {
            var btn = new Button
            {
                Text = icon,
                Font = Theme.SegoeUI(12),
                BackColor = Theme.InputBackground,
                ForeColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Top = 4,
                Width = 40,
                Height = 38,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Theme.InputBorder;
            btn.FlatAppearance.MouseOverBackColor = Theme.IconHoverBackground;
            return btn;
        }

        private Button CriarBotaoIcon(string icon, int x, int y, int width, int height)
        {
            var btn = new Button
            {
                Text = icon,
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.InputBackground,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Left = x,
                Top = y,
                Width = width,
                Height = height,
                Cursor = Cursors.Hand
            };

            btn.MouseEnter += (s, e) => btn.BackColor = Theme.IconHoverBackground;
            btn.MouseLeave += (s, e) => btn.BackColor = Theme.InputBackground;

            return btn;
        }

        private void AplicarEstiloPainel(Panel pnl)
        {
            pnl.BackColor = Theme.CardBackground;
            pnl.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
                }
            };
        }

        private void AplicarEstiloPainelInterno(Control ctrl)
        {
            ctrl.BackColor = Theme.InputBackground;
            if (ctrl is Panel p)
            {
                p.Paint += (s, e) =>
                {
                    using (var pen = new Pen(Theme.InputBorder, 1))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                    }
                };
            }
        }

        private void AplicarEstiloBlocoForça(Panel bloco)
        {
            bloco.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var path = RoundedRectangle(0, 0, bloco.Width, bloco.Height, 3))
                {
                    using (var brush = new SolidBrush(bloco.BackColor))
                    {
                        g.FillPath(brush, path);
                    }
                }
            };
        }

        private GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;

            if (diameter > width) diameter = width;
            if (diameter > height) diameter = height;

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

        private void GerarSenha()
        {
            string opcoes = "";

            if (_toggleMaiusculas.Checked) opcoes += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (_toggleMinusculas.Checked) opcoes += "abcdefghijklmnopqrstuvwxyz";
            if (_toggleNumeros.Checked) opcoes += "0123456789";
            if (_toggleEspeciais.Checked) opcoes += "!@#$%^&*()_+-=[]{}|;:,.<>?";

            if (string.IsNullOrEmpty(opcoes))
            {
                MessageBox.Show("Selecione pelo menos uma opção", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int quantidade = _sliderQuantidade.Value;
            _senhasGeradas.Clear();
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int n = 0; n < quantidade; n++)
                    _senhasGeradas.Add(GerarUmaSenha(rng, opcoes, _sliderTamanho.Value));
            }

            _senhaGerada = _senhasGeradas[0];
            _txtSenhaGerada.Text = _mostrarSenha ? _senhaGerada : new string('•', _senhaGerada.Length);
            AtualizarBarraForca();

            AtualizarListaSenhasGeradas();
        }

        private static string GerarUmaSenha(RandomNumberGenerator rng, string opcoes, int tamanho)
        {
            var senha = new StringBuilder(tamanho);
            byte[] data = new byte[1];
            for (int i = 0; i < tamanho; i++)
            {
                rng.GetBytes(data);
                senha.Append(opcoes[data[0] % opcoes.Length]);
            }
            return senha.ToString();
        }

        private void AtualizarListaSenhasGeradas()
        {
            _flowGerador.SuspendLayout();

            for (int i = _flowGerador.Controls.Count - 1; i >= 0; i--)
            {
                if (_flowGerador.Controls[i].Tag is "lista-senhas")
                {
                    var c = _flowGerador.Controls[i];
                    _flowGerador.Controls.RemoveAt(i);
                    c.Dispose();
                }
            }

            int largura = _flowGerador.ClientSize.Width - _flowGerador.Padding.Left - _flowGerador.Padding.Right;

            if (_senhasGeradas.Count > 1)
            {
                var header = new Panel { Height = 26, Width = largura, Tag = "lista-senhas", Margin = new Padding(0, 8, 0, 6) };
                var lblTitulo = new Label
                {
                    Text = $"Senhas geradas ({_senhasGeradas.Count})",
                    Font = Theme.SegoeUI(9.5f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    AutoSize = true,
                    Left = 0,
                    Top = 4
                };
                header.Controls.Add(lblTitulo);
                var btnCopiarTodas = new Button
                {
                    Text = "Copiar todas",
                    Font = Theme.SegoeUI(8.5f),
                    BackColor = Color.Transparent,
                    ForeColor = Theme.AccentPrimary,
                    FlatStyle = FlatStyle.Flat,
                    Width = 90,
                    Height = 24,
                    Top = 0,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Right | AnchorStyles.Top
                };
                btnCopiarTodas.FlatAppearance.BorderSize = 0;
                btnCopiarTodas.Click += (s, e) =>
                {
                    try { Clipboard.SetText(string.Join(Environment.NewLine, _senhasGeradas)); } catch { }
                };
                header.Controls.Add(btnCopiarTodas);
                header.Resize += (s, e) => { btnCopiarTodas.Left = header.Width - btnCopiarTodas.Width; };
                _flowGerador.Controls.Add(header);

                foreach (var senha in _senhasGeradas)
                    _flowGerador.Controls.Add(CriarItemSenhaGerada(senha, largura));
            }

            _flowGerador.ResumeLayout();
        }

        private Panel CriarItemSenhaGerada(string senha, int largura)
        {
            var corBg = Theme.InputBackground;
            var item = new Panel { Height = 38, Width = largura, Tag = "lista-senhas", BackColor = corBg, Margin = new Padding(0, 0, 0, 6) };
            AplicarBordaArredondada(item, Theme.InputBorder, 8);

            var lbl = new Label
            {
                Text = senha,
                Font = Theme.Consolas(10),
                ForeColor = Theme.TextPrimary,
                BackColor = corBg,
                AutoSize = false,
                AutoEllipsis = true,
                Left = 12,
                Top = 0,
                Height = 38,
                TextAlign = ContentAlignment.MiddleLeft
            };
            item.Controls.Add(lbl);

            var btnCopiar = CriarBotaoIcon("⧉", 0, 5, 28, 28);
            btnCopiar.BackColor = corBg;
            btnCopiar.Click += (s, e) =>
            {
                try { Clipboard.SetText(senha); } catch { }
                btnCopiar.Text = "✓";
                btnCopiar.ForeColor = Theme.StrengthStrong;
                var t = new System.Windows.Forms.Timer { Interval = 1000 };
                t.Tick += (ss, ee) => { btnCopiar.Text = "⧉"; btnCopiar.ForeColor = Theme.TextSecondary; t.Stop(); t.Dispose(); };
                t.Start();
            };
            item.Controls.Add(btnCopiar);

            item.Resize += (s, e) =>
            {
                btnCopiar.Left = item.Width - btnCopiar.Width - 6;
                lbl.Width = btnCopiar.Left - lbl.Left - 6;
            };
            return item;
        }

        private void AtualizarBarraForca()
        {
            int forca = CalcularForcaSenha(_senhaGerada);
            string forcaTexto = "";
            Color corForca = Theme.StrengthWeak;

            switch (forca)
            {
                case 1:
                    forcaTexto = "Fraca";
                    corForca = Theme.StrengthWeak;
                    break;
                case 2:
                    forcaTexto = "Média";
                    corForca = Theme.StrengthMedium;
                    break;
                case 3:
                case 4:
                    forcaTexto = "Forte";
                    corForca = Theme.StrengthStrong;
                    break;
            }

            _lblForça.Text = forcaTexto;
            _lblForça.ForeColor = corForca;

            _nivelForca = forca;
            _corForca = corForca;
            _pnlForcaBarra.Invalidate();
        }

        private int CalcularForcaSenha(string senha)
        {
            int forca = 0;

            if (string.IsNullOrEmpty(senha)) return 0;
            if (senha.Length >= 8) forca++;
            if (senha.Length >= 12) forca++;
            if (System.Text.RegularExpressions.Regex.IsMatch(senha, "[A-Z]") &&
                System.Text.RegularExpressions.Regex.IsMatch(senha, "[a-z]")) forca++;
            if (System.Text.RegularExpressions.Regex.IsMatch(senha, "[0-9]")) forca++;

            return Math.Min(forca, 4);
        }

        private void AlternarVisibilidadeSenha()
        {
            _mostrarSenha = !_mostrarSenha;
            if (!string.IsNullOrEmpty(_senhaGerada))
                _txtSenhaGerada.Text = _mostrarSenha ? _senhaGerada : new string('•', _senhaGerada.Length);
        }

        private void CopiarSenha()
        {
            if (!string.IsNullOrEmpty(_senhaGerada))
            {
                Clipboard.SetText(_senhaGerada);
                MessageBox.Show("Senha copiada!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SalvarNoCovre()
        {
            if (string.IsNullOrEmpty(_senhaGerada))
            {
                MessageBox.Show("Gere uma senha primeiro", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var form = new FormCriarSenha(_servicoSenha, _senhaGerada);
            if (form.ShowDialog() == DialogResult.OK)
                _ = CarregarSenhas();
        }

        private void CriarNovaSenha()
        {
            var form = new FormCriarSenha(_servicoSenha);
            if (form.ShowDialog() == DialogResult.OK)
                _ = CarregarSenhas();
        }

        private async System.Threading.Tasks.Task CarregarSenhas()
        {
            try
            {
                var senhas = await _servicoSenha.ListarTodosAsync();
                _senhasAtuais = senhas;
                FiltrarSenhas();
                AtualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AtualizarLista(List<Senha> lista)
        {
            _pnlListaSenhas.SuspendLayout();

            for (int i = _pnlListaSenhas.Controls.Count - 1; i >= 0; i--)
            {
                if (_pnlListaSenhas.Controls[i] is LinhaSenha velha)
                {
                    _pnlListaSenhas.Controls.RemoveAt(i);
                    velha.Dispose();
                }
            }
            _linhasSenha.Clear();

            _lblVazio.Visible = lista.Count == 0;

            for (int i = lista.Count - 1; i >= 0; i--)
            {
                var linha = new LinhaSenha(lista[i], ObterSenhaPlain, FavoritarToggle, EditarSenha);

                var plain = ObterSenhaPlain(lista[i]);
                if (!string.IsNullOrEmpty(plain))
                    linha.NivelForca = CalcularForcaSenha(plain);

                _pnlListaSenhas.Controls.Add(linha);
                _linhasSenha.Add(linha);
            }

            _pnlListaSenhas.ResumeLayout();
        }

        private string? ObterSenhaPlain(Senha s)
        {
            try { return _criptografia?.Descriptografar(s.SenhaHash); }
            catch { return null; }
        }

        private async System.Threading.Tasks.Task AlterarSenhaMestraAsync()
        {
            using var dlg = new FormAlterarSenhaMestra();
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var servico = new ServicoMudancaSenhaMestra();
                await servico.AlterarAsync(dlg.SenhaAtual, dlg.NovaSenha);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Alterar senha mestra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Alterar senha mestra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar a senha mestra: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            QrCodeUtil.OferecerSalvarBackup(this, dlg.NovaSenha);

            MessageBox.Show(
                "Senha mestra alterada com sucesso.\n\nO aplicativo será reiniciado para aplicar a nova senha.",
                "Alterar senha mestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Restart();
        }

        private async System.Threading.Tasks.Task ExportarSenhasAsync()
        {
            try
            {
                var senhas = await _servicoSenha.ListarTodosAsync();
                if (senhas.Count == 0)
                {
                    MessageBox.Show("O cofre está vazio. Não há nada para exportar.", "Exportar",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var dlg = new FormSenhaExportacao(modoExportar: true);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                using var sfd = new SaveFileDialog
                {
                    Title = "Exportar senhas",
                    Filter = "Cofre exportado (*.gsenhas)|*.gsenhas|Todos os arquivos (*.*)|*.*",
                    FileName = $"cofre-senhas-{DateTime.Now:yyyy-MM-dd}.gsenhas"
                };
                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                var itens = new List<SenhaExportada>();
                foreach (var s in senhas)
                {
                    var plain = ObterSenhaPlain(s);
                    if (plain == null) continue;
                    itens.Add(new SenhaExportada
                    {
                        NomeServico = s.NomeServico,
                        Usuario = s.Usuario,
                        Senha = plain,
                        Url = s.Url,
                        Categoria = s.Categoria,
                        Notas = s.Notas,
                        Favorito = s.Favorito,
                        DataCriacao = s.DataCriacao,
                        DataAtualizacao = s.DataAtualizacao
                    });
                }

                await _servicoExportacao.ExportarAsync(sfd.FileName, itens, dlg.SenhaInformada);

                MessageBox.Show(
                    $"{itens.Count} senha(s) exportada(s) com sucesso.\n\nGuarde bem a senha de exportação — sem ela o arquivo não pode ser aberto.",
                    "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task ImportarSenhasAsync()
        {
            try
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Importar senhas",
                    Filter = "Cofre exportado (*.gsenhas)|*.gsenhas|Todos os arquivos (*.*)|*.*"
                };
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                using var dlg = new FormSenhaExportacao(modoExportar: false);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                List<SenhaExportada> itens;
                try
                {
                    itens = await _servicoExportacao.ImportarAsync(ofd.FileName, dlg.SenhaInformada);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Importar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (itens.Count == 0)
                {
                    MessageBox.Show("Nenhuma senha encontrada no arquivo.", "Importar",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var existentes = await _servicoSenha.ListarTodosAsync();
                var chaves = new HashSet<string>(
                    existentes.Select(s => s.NomeServico + " " + s.Usuario),
                    StringComparer.OrdinalIgnoreCase);

                int adicionadas = 0, ignoradas = 0;
                foreach (var item in itens)
                {
                    if (string.IsNullOrWhiteSpace(item.NomeServico) ||
                        string.IsNullOrWhiteSpace(item.Usuario) ||
                        string.IsNullOrWhiteSpace(item.Senha) ||
                        !chaves.Add(item.NomeServico + " " + item.Usuario))
                    {
                        ignoradas++;
                        continue;
                    }

                    var nova = await _servicoSenha.CriarSenhaAsync(
                        item.NomeServico, item.Usuario, item.Senha, item.Categoria, item.Url, item.Notas);
                    if (item.Favorito)
                        await _servicoSenha.MarcarComoFavoritoAsync(nova.Id);
                    adicionadas++;
                }

                await _servicoSenha.PersistirAsync();
                await CarregarSenhas();

                var msg = $"{adicionadas} senha(s) importada(s) com sucesso.";
                if (ignoradas > 0)
                    msg += $"\n{ignoradas} ignorada(s) (já existiam ou inválidas).";
                MessageBox.Show(msg, "Importar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao importar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task VerificarVazamentosAsync()
        {
            if (_linhasSenha.Count == 0)
            {
                MessageBox.Show("Não há senhas no cofre para verificar.", "Verificar vazamentos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var textoOriginal = _btnVerificarVazamento.Text;
            _btnVerificarVazamento.Enabled = false;
            _btnVerificarVazamento.Text = "…";

            int comprometidas = 0;
            int verificadas = 0;
            try
            {
                foreach (var linha in _linhasSenha)
                {
                    var plain = ObterSenhaPlain(linha.Senha);
                    if (string.IsNullOrEmpty(plain)) continue;

                    int contagem = await _servicoVazamento.VerificarAsync(plain);
                    linha.Vazamentos = contagem;
                    if (contagem > 0) comprometidas++;
                    verificadas++;
                }

                string msg = comprometidas == 0
                    ? $"Boa notícia! Nenhuma das {verificadas} senha(s) verificada(s) foi encontrada em vazamentos conhecidos."
                    : $"Atenção: {comprometidas} de {verificadas} senha(s) aparecem em vazamentos conhecidos. Considere trocá-las (marcadas com ⚠).";

                MessageBox.Show(msg, "Verificação concluída", MessageBoxButtons.OK,
                    comprometidas == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível verificar os vazamentos.\nVerifique sua conexão com a internet.\n\nDetalhe: {ex.Message}",
                    "Erro de rede", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnVerificarVazamento.Text = textoOriginal;
                _btnVerificarVazamento.Enabled = true;
            }
        }

        private async void FavoritarToggle(Senha s)
        {
            try
            {
                if (s.Favorito) await _servicoSenha.RemoverDeFavoritoAsync(s.Id);
                else await _servicoSenha.MarcarComoFavoritoAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao favoritar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditarSenha(Senha s)
        {
            var form = new FormEditarSenha(_servicoSenha, s);
            if (form.ShowDialog() == DialogResult.OK)
                _ = CarregarSenhas();
        }

        private void AtualizarContador()
        {
            int total = _senhasAtuais.Count;
            int favoritos = _senhasAtuais.Count(s => s.Favorito);
            string itensTxt = total == 1 ? "1 item" : $"{total} itens";
            _lblContadorHeader.Text = itensTxt;
            _lblStatusCriptografia.Text = $"{total} {(total == 1 ? "senha" : "senhas")} • {favoritos} {(favoritos == 1 ? "favorita" : "favoritas")}";
        }

        private void FiltrarSenhas()
        {
            var termo = _txtBusca.Text.ToLower();
            Categoria? categoriaFiltro = null;
            if (_cbCategoria.SelectedIndex > 0)
                categoriaFiltro = (Categoria)(_cbCategoria.SelectedIndex - 1);

            var filtradas = _senhasAtuais
                .Where(s => (string.IsNullOrEmpty(termo) || s.NomeServico.ToLower().Contains(termo) || s.Usuario.ToLower().Contains(termo)))
                .Where(s => categoriaFiltro == null || s.Categoria == categoriaFiltro)
                .Where(s => !_somenteFavoritos || s.Favorito)
                .ToList();

            AtualizarLista(filtradas);
        }

        private void AlternarFiltroFavoritos()
        {
            _somenteFavoritos = !_somenteFavoritos;
            _btnFiltroFavoritos.ForeColor = _somenteFavoritos ? Theme.FavoriteColor : Theme.FavoriteBorderColor;
            _btnFiltroFavoritos.BackColor = _somenteFavoritos ? Theme.AccentLight : Theme.InputBackground;
            FiltrarSenhas();
        }
    }
}
