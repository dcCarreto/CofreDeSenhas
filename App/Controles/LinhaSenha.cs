using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using GerenciadorDeSenhas.Modelos;

namespace App
{
    public class LinhaSenha : Panel
    {
        private readonly Senha _senha;
        private readonly Func<Senha, string?> _obterSenhaPlain;
        private readonly Action<Senha> _onFavoritar;
        private readonly Action<Senha> _onEditar;

        private bool _hover;
        private bool _revelada;

        private Label _lblEstrela = null!;
        private Label _lblNome = null!;
        private Label _lblUsuario = null!;
        private Label _lblData = null!;
        private Label _lblIndicador = null!;
        private Panel _avatar = null!;
        private Panel _chip = null!;
        private Button _btnOlho = null!;
        private Button _btnCopiar = null!;
        private Button _btnEditar = null!;
        private ToolTip _tip = null!;

        private int _nivelForca = -1;
        private int _vazamentos = -1;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int NivelForca
        {
            get => _nivelForca;
            set { _nivelForca = value; AtualizarIndicador(); }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Vazamentos
        {
            get => _vazamentos;
            set { _vazamentos = value; AtualizarIndicador(); }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Senha Senha => _senha;

        private static Color CorNormal => Theme.CardBackground;
        private static Color CorHover => Theme.RowHover;
        private static Color CorSeparador => Theme.Separator;
        private static Color CorAccent => Theme.AccentPrimary;
        private static Color CorIconHover => Theme.IconHoverBackground;

        private static readonly Color[] PaletaAvatar =
        {
            Color.FromArgb(124, 58, 237),
            Color.FromArgb(37, 99, 235),
            Color.FromArgb(22, 163, 74),
            Color.FromArgb(234, 88, 12),
            Color.FromArgb(219, 39, 119),
            Color.FromArgb(8, 145, 178),
            Color.FromArgb(202, 138, 4),
            Color.FromArgb(220, 38, 38),
        };

        public LinhaSenha(Senha senha, Func<Senha, string?> obterSenhaPlain,
            Action<Senha> onFavoritar, Action<Senha> onEditar)
        {
            _senha = senha;
            _obterSenhaPlain = obterSenhaPlain;
            _onFavoritar = onFavoritar;
            _onEditar = onEditar;

            this.Height = 58;
            this.Dock = DockStyle.Top;
            this.BackColor = CorNormal;
            this.DoubleBuffered = true;

            ConstruirControles();
            AnexarHover(this);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(CorSeparador, 1);
            e.Graphics.DrawLine(pen, 12, this.Height - 1, this.Width - 12, this.Height - 1);
        }

        private void ConstruirControles()
        {
            _lblEstrela = new Label
            {
                Text = _senha.Favorito ? "★" : "☆",
                Font = new Font("Segoe UI", 13),
                ForeColor = _senha.Favorito ? Theme.FavoriteColor : Theme.FavoriteBorderColor,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Width = 28,
                Height = 28
            };
            _lblEstrela.Click += (s, e) => _onFavoritar(_senha);
            Controls.Add(_lblEstrela);

            _avatar = new Panel { Width = 36, Height = 36 };
            _avatar.Paint += DesenharAvatar;
            Controls.Add(_avatar);

            _lblNome = new Label
            {
                Text = _senha.NomeServico,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 36
            };
            Controls.Add(_lblNome);

            _lblUsuario = new Label
            {
                Text = _senha.Usuario,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 36
            };
            Controls.Add(_lblUsuario);

            _chip = new Panel { Height = 24, BackColor = Color.Transparent };
            _chip.Paint += DesenharChip;
            Controls.Add(_chip);

            _lblData = new Label
            {
                Text = FormatarData(_senha.DataCriacao),
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.TextTertiary,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 36
            };
            Controls.Add(_lblData);

            _btnOlho = CriarBotaoAcao("👁");
            _btnOlho.Click += (s, e) => AlternarRevelar();
            Controls.Add(_btnOlho);

            _btnCopiar = CriarBotaoAcao("⧉");
            _btnCopiar.Click += (s, e) => Copiar();
            Controls.Add(_btnCopiar);

            _btnEditar = CriarBotaoAcao("✎");
            _btnEditar.Click += (s, e) => _onEditar(_senha);
            Controls.Add(_btnEditar);

            _tip = new ToolTip { InitialDelay = 250 };
            _lblIndicador = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 11),
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 16,
                Height = 16
            };
            Controls.Add(_lblIndicador);
            AtualizarIndicador();
        }

        private void AtualizarIndicador()
        {
            if (_lblIndicador == null) return;

            if (_vazamentos > 0)
            {
                _lblIndicador.Text = "⚠";
                _lblIndicador.ForeColor = Color.FromArgb(220, 38, 38);
                _tip.SetToolTip(_lblIndicador, $"Senha comprometida — encontrada em {_vazamentos:N0} vazamento(s). Troque-a!");
                return;
            }

            switch (_nivelForca)
            {
                case 0:
                case 1:
                    _lblIndicador.Text = "●";
                    _lblIndicador.ForeColor = Color.FromArgb(239, 68, 68);
                    _tip.SetToolTip(_lblIndicador, "Senha fraca" + (_vazamentos == 0 ? " (não encontrada em vazamentos)" : ""));
                    break;
                case 2:
                    _lblIndicador.Text = "●";
                    _lblIndicador.ForeColor = Color.FromArgb(245, 158, 11);
                    _tip.SetToolTip(_lblIndicador, "Senha média" + (_vazamentos == 0 ? " (não encontrada em vazamentos)" : ""));
                    break;
                case 3:
                case 4:
                    _lblIndicador.Text = "●";
                    _lblIndicador.ForeColor = Color.FromArgb(22, 163, 74);
                    _tip.SetToolTip(_lblIndicador, "Senha forte" + (_vazamentos == 0 ? " (não encontrada em vazamentos)" : ""));
                    break;
                default:
                    _lblIndicador.Text = "";
                    _tip.SetToolTip(_lblIndicador, "");
                    break;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_lblEstrela == null) return;
            int h = this.Height;
            int meioV = (h - 36) / 2;

            _lblEstrela.SetBounds(10, (h - 28) / 2, 28, 28);
            _avatar.SetBounds(44, meioV, 36, 36);
            _lblIndicador.SetBounds(82, (h - 16) / 2, 16, 16);

            int acoesW = 3 * 30 + 2 * 2;
            int acoesX = this.Width - acoesW - 14;
            _btnEditar.SetBounds(acoesX + 2 * 32, (h - 30) / 2, 30, 30);
            _btnCopiar.SetBounds(acoesX + 32, (h - 30) / 2, 30, 30);
            _btnOlho.SetBounds(acoesX, (h - 30) / 2, 30, 30);

            int dataW = 92;
            int dataX = acoesX - dataW - 10;
            _lblData.SetBounds(dataX, meioV, dataW, 36);

            int chipW = 104;
            int chipX = dataX - chipW - 10;
            _chip.SetBounds(chipX, (h - 24) / 2, chipW, 24);

            int nomeX = 102;
            int dispEsq = chipX - 12 - nomeX;
            if (dispEsq < 120) dispEsq = 120;
            int nomeW = (int)(dispEsq * 0.52);
            int usuarioW = dispEsq - nomeW - 8;
            _lblNome.SetBounds(nomeX, meioV, nomeW, 36);
            _lblUsuario.SetBounds(nomeX + nomeW + 8, meioV, usuarioW, 36);
        }

        private void DesenharAvatar(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColorAtual());
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Arredondado(0, 0, _avatar.Width, _avatar.Height, 9))
            using (var brush = new SolidBrush(CorAvatar(_senha.NomeServico)))
                g.FillPath(brush, path);

            string inicial = string.IsNullOrWhiteSpace(_senha.NomeServico)
                ? "?" : _senha.NomeServico.Trim().Substring(0, 1).ToUpper();
            TextRenderer.DrawText(g, inicial, new Font("Segoe UI", 12, FontStyle.Bold),
                new Rectangle(0, 0, _avatar.Width, _avatar.Height), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void DesenharChip(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColorAtual());
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var (bg, fg, texto) = InfoCategoria(_senha.Categoria);

            var sz = TextRenderer.MeasureText(texto, new Font("Segoe UI", 8.5f, FontStyle.Bold));
            int pillW = Math.Min(_chip.Width, sz.Width + 22);
            int pillH = _chip.Height;
            using (var path = Arredondado(0, 0, pillW, pillH, pillH / 2f))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);
            TextRenderer.DrawText(g, texto, new Font("Segoe UI", 8.5f, FontStyle.Bold),
                new Rectangle(0, 0, pillW, pillH), fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void AlternarRevelar()
        {
            _revelada = !_revelada;
            if (_revelada)
            {
                var plain = _obterSenhaPlain(_senha) ?? "••••••••";
                _lblUsuario.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
                _lblUsuario.ForeColor = CorAccent;
                _lblUsuario.Text = plain;
                _btnOlho.Text = "🙈";
            }
            else
            {
                _lblUsuario.Font = new Font("Segoe UI", 9.5f);
                _lblUsuario.ForeColor = Theme.TextSecondary;
                _lblUsuario.Text = _senha.Usuario;
                _btnOlho.Text = "👁";
            }
        }

        private void Copiar()
        {
            var plain = _obterSenhaPlain(_senha);
            if (string.IsNullOrEmpty(plain)) return;
            try { Clipboard.SetText(plain); } catch { }
            _btnCopiar.Text = "✓";
            _btnCopiar.ForeColor = Color.FromArgb(22, 163, 74);
            var t = new System.Windows.Forms.Timer { Interval = 1000 };
            t.Tick += (s, e) => { _btnCopiar.Text = "⧉"; _btnCopiar.ForeColor = Theme.TextSecondary; t.Stop(); t.Dispose(); };
            t.Start();
        }

        private void AnexarHover(Control c)
        {
            c.MouseEnter += (s, e) => DefinirHover(true);
            c.MouseLeave += (s, e) =>
            {
                var p = this.PointToClient(Cursor.Position);
                if (!this.ClientRectangle.Contains(p)) DefinirHover(false);
            };
            foreach (Control filho in c.Controls)
                AnexarHover(filho);
        }

        private void DefinirHover(bool ativo)
        {
            if (_hover == ativo) return;
            _hover = ativo;
            this.BackColor = ativo ? CorHover : CorNormal;
            foreach (Control c in Controls)
            {
                if (c is Button b && b.Tag is "acao") continue;
                c.Invalidate();
            }
            _avatar.Invalidate();
            _chip.Invalidate();
            this.Invalidate();
        }

        private Color BackColorAtual() => _hover ? CorHover : CorNormal;

        private Button CriarBotaoAcao(string icon)
        {
            var btn = new Button
            {
                Text = icon,
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Width = 30,
                Height = 30,
                Cursor = Cursors.Hand,
                Tag = "acao"
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = CorIconHover;
            btn.MouseEnter += (s, e) => btn.ForeColor = CorAccent;
            btn.MouseLeave += (s, e) => { if (btn.Text != "✓") btn.ForeColor = Theme.TextSecondary; };
            return btn;
        }

        public static Color CorAvatar(string nome)
        {
            int hash = 0;
            foreach (char c in nome ?? "")
                hash = hash * 31 + c;
            return PaletaAvatar[Math.Abs(hash) % PaletaAvatar.Length];
        }

        private static (Color bg, Color fg, string texto) InfoCategoria(Categoria cat) => cat switch
        {
            Categoria.Personal => (Color.FromArgb(234, 241, 255), Color.FromArgb(37, 99, 235), "Pessoal"),
            Categoria.Work => (Color.FromArgb(241, 236, 254), Color.FromArgb(124, 58, 237), "Trabalho"),
            Categoria.Finance => (Color.FromArgb(231, 247, 238), Color.FromArgb(22, 163, 74), "Finanças"),
            Categoria.Social => (Color.FromArgb(253, 234, 243), Color.FromArgb(219, 39, 119), "Social"),
            _ => (Color.FromArgb(253, 238, 224), Color.FromArgb(234, 88, 12), "Outro"),
        };

        private static string FormatarData(DateTime data)
        {
            var ptBR = CultureInfo.GetCultureInfo("pt-BR");
            return data.ToLocalTime().ToString("dd MMM yyyy", ptBR);
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
