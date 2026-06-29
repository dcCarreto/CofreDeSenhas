using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace App
{
    public partial class FormCriarSenha : Form
    {
        private readonly IServicoSenha _servicoSenha;
        private readonly string? _senhaGerada;

        private TextBox _txtNomeServico = null!;
        private TextBox _txtUsuario = null!;
        private TextBox _txtSenha = null!;
        private ComboBox _cmbCategoria = null!;
        private TextBox _txtUrl = null!;
        private TextBox _txtNotas = null!;

        public FormCriarSenha(IServicoSenha servicoSenha, string? senhaGerada = null)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _senhaGerada = senhaGerada;
            ConfigurarUI();
        }

        private void ConfigurarUI()
        {
            this.Text = "Nova senha";
            this.ClientSize = new Size(460, 624);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.CardBackground;
            this.DoubleBuffered = true;

            ConfigurarHeader();
            ConfigurarRodape();
            ConfigurarCorpo();

            AplicarCantos();
            this.Resize += (s, e) => AplicarCantos();
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
                Text = "Nova senha",
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
            btnFechar.MouseEnter += (s, e) => { btnFechar.BackColor = Theme.HoverBackground; };
            btnFechar.MouseLeave += (s, e) => { btnFechar.BackColor = Color.Transparent; };
            btnFechar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            header.Controls.Add(btnFechar);
            header.Resize += (s, e) => { btnFechar.Left = header.Width - btnFechar.Width - 14; };

            bool dragging = false; Point start = Point.Empty;
            void Down(object? s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragging = true; start = e.Location; } }
            void Move(object? s, MouseEventArgs e) { if (dragging) this.Location = new Point(this.Location.X + e.X - start.X, this.Location.Y + e.Y - start.Y); }
            void Up(object? s, MouseEventArgs e) => dragging = false;
            header.MouseDown += Down; header.MouseMove += Move; header.MouseUp += Up;
            lblTitulo.MouseDown += Down; lblTitulo.MouseMove += Move; lblTitulo.MouseUp += Up;

            this.Controls.Add(header);
        }

        private void ConfigurarRodape()
        {
            var rodape = new Panel { Height = 72, Dock = DockStyle.Bottom, BackColor = Theme.CardBackground };
            rodape.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, 0, rodape.Width, 0);
            };

            var btnSalvar = new Button
            {
                Text = "Salvar",
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
            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            btnSalvar.Click += async (s, e) => await SalvarAsync();
            rodape.Controls.Add(btnSalvar);

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
            btnCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            rodape.Controls.Add(btnCancelar);
            rodape.Resize += (s, e) => { btnCancelar.Left = rodape.Width - btnCancelar.Width - 24; };

            this.Controls.Add(rodape);
        }

        private void ConfigurarCorpo()
        {
            var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBackground, Padding = new Padding(24, 18, 24, 8) };
            int w = this.ClientSize.Width - 48;
            int y = 18;

            _txtNomeServico = AdicionarCampo(corpo, "Nome do serviço", ref y, w);
            _txtUsuario = AdicionarCampo(corpo, "Usuário / E-mail", ref y, w);
            _txtSenha = AdicionarCampo(corpo, "Senha", ref y, w, isPassword: true);
            if (!string.IsNullOrEmpty(_senhaGerada)) _txtSenha.Text = _senhaGerada;

            _cmbCategoria = AdicionarCombo(corpo, "Categoria", ref y, w);
            _txtUrl = AdicionarCampo(corpo, "URL", ref y, w, opcional: true);
            _txtNotas = AdicionarCampo(corpo, "Notas", ref y, w, opcional: true, altura: 60);

            this.Controls.Add(corpo);
            corpo.BringToFront();
        }

        private TextBox AdicionarCampo(Panel parent, string label, ref int y, int w, bool isPassword = false, bool opcional = false, int altura = 38)
        {
            var lbl = new Label
            {
                Text = opcional ? $"{label}  (opcional)" : label,
                Font = Theme.SegoeUI(9),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Left = 0,
                Top = y
            };
            parent.Controls.Add(lbl);
            y += 20;

            var box = new Panel { Left = 0, Top = y, Width = w, Height = altura, BackColor = Theme.InputBackground, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
            AplicarBordaArredondada(box, Theme.InputBorder, 8);

            var txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.SegoeUI(10.5f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.InputBackground,
                UseSystemPasswordChar = isPassword,
                Multiline = altura > 40,
                Left = 12,
                Width = w - 24,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            if (altura > 40)
            {
                txt.Top = 8;
                txt.Height = altura - 16;
            }
            else
            {
                txt.Top = Math.Max(0, (altura - txt.PreferredHeight) / 2);
                txt.Height = txt.PreferredHeight;
            }
            box.Controls.Add(txt);
            parent.Controls.Add(box);

            y += altura + 16;
            return txt;
        }

        private ComboBox AdicionarCombo(Panel parent, string label, ref int y, int w)
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

            var box = new Panel { Left = 0, Top = y, Width = w, Height = 38, BackColor = Theme.InputBackground, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
            AplicarBordaArredondada(box, Theme.InputBorder, 8);

            var cmb = new ComboBoxTema
            {
                Font = Theme.SegoeUI(10.5f),
                Left = 10,
                Top = 8,
                Width = w - 20,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            cmb.Items.AddRange(CategoriasUI.Rotulos);
            cmb.SelectedIndex = (int)Categoria.Personal;
            box.Controls.Add(cmb);
            parent.Controls.Add(box);

            y += 38 + 16;
            return cmb;
        }

        private void AplicarBordaArredondada(Panel pnl, Color borderColor, int radius)
        {
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(0, 0, pnl.Width - 1, pnl.Height - 1, radius);
                using var pen = new Pen(borderColor, 1);
                g.DrawPath(pen, path);
            };
        }

        private void AplicarCantos()
        {
            using var path = RoundedRect(0, 0, this.Width, this.Height, 12);
            this.Region = new Region(path);
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

        private async System.Threading.Tasks.Task SalvarAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_txtNomeServico.Text) ||
                    string.IsNullOrWhiteSpace(_txtUsuario.Text) ||
                    string.IsNullOrWhiteSpace(_txtSenha.Text))
                {
                    MessageBox.Show("Preencha os campos obrigatórios (nome, usuário e senha).", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var categoria = (Categoria)_cmbCategoria.SelectedIndex;
                await _servicoSenha.CriarSenhaAsync(
                    _txtNomeServico.Text,
                    _txtUsuario.Text,
                    _txtSenha.Text,
                    categoria,
                    string.IsNullOrWhiteSpace(_txtUrl.Text) ? null : _txtUrl.Text,
                    string.IsNullOrWhiteSpace(_txtNotas.Text) ? null : _txtNotas.Text);

                await _servicoSenha.PersistirAsync();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar senha: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
