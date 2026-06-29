using System;
using System.Windows.Forms;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace App
{
    public partial class FormEditarSenha : Form
    {
        private readonly IServicoSenha _servicoSenha;
        private readonly Senha _senhaAtual;

        public FormEditarSenha(IServicoSenha servicoSenha, Senha senhaAtual)
        {
            _servicoSenha = servicoSenha ?? throw new ArgumentNullException(nameof(servicoSenha));
            _senhaAtual = senhaAtual ?? throw new ArgumentNullException(nameof(senhaAtual));
            ConfigurarUI();
        }

        private void ConfigurarUI()
        {
            this.Text = $"Editar Senha - {_senhaAtual.NomeServico}";
            this.Width = 500;
            this.Height = 450;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var yPos = 20;
            const int xPos = 20;
            const int width = 430;

            var lblNomeServico = new Label { Text = "Nome do Serviço:", Left = xPos, Top = yPos, Width = width };
            var txtNomeServico = new TextBox { Name = "txtNomeServico", Left = xPos, Top = yPos + 25, Width = width, Height = 30, Text = _senhaAtual.NomeServico };
            yPos += 70;

            var lblUsuario = new Label { Text = "Usuário/Email:", Left = xPos, Top = yPos, Width = width };
            var txtUsuario = new TextBox { Name = "txtUsuario", Left = xPos, Top = yPos + 25, Width = width, Height = 30, Text = _senhaAtual.Usuario };
            yPos += 70;

            var lblSenha = new Label { Text = "Senha (deixar em branco para manter):", Left = xPos, Top = yPos, Width = width };
            var txtSenha = new TextBox { Name = "txtSenha", Left = xPos, Top = yPos + 25, Width = width, Height = 30, UseSystemPasswordChar = true };
            yPos += 70;

            var lblCategoria = new Label { Text = "Categoria:", Left = xPos, Top = yPos, Width = width };
            var cmbCategoria = new ComboBox { Name = "cmbCategoria", Left = xPos, Top = yPos + 25, Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategoria.Items.AddRange(CategoriasUI.Rotulos);
            cmbCategoria.SelectedIndex = (int)_senhaAtual.Categoria;
            yPos += 70;

            var lblUrl = new Label { Text = "URL (opcional):", Left = xPos, Top = yPos, Width = width };
            var txtUrl = new TextBox { Name = "txtUrl", Left = xPos, Top = yPos + 25, Width = width, Height = 30, Text = _senhaAtual.Url ?? "" };
            yPos += 70;

            var lblNotas = new Label { Text = "Notas (opcional):", Left = xPos, Top = yPos, Width = width };
            var txtNotas = new TextBox { Name = "txtNotas", Left = xPos, Top = yPos + 25, Width = width, Height = 60, Multiline = true, Text = _senhaAtual.Notas ?? "" };
            yPos += 100;

            var btnSalvar = new Button { Text = "Salvar", Left = xPos, Top = yPos, Width = 100, Height = 35 };
            var btnCancelar = new Button { Text = "Cancelar", Left = xPos + 120, Top = yPos, Width = 100, Height = 35 };

            btnSalvar.Click += async (s, e) => await BtnSalvar_Click(txtNomeServico, txtUsuario, txtSenha, cmbCategoria, txtUrl, txtNotas);
            btnCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] {
                lblNomeServico, txtNomeServico,
                lblUsuario, txtUsuario,
                lblSenha, txtSenha,
                lblCategoria, cmbCategoria,
                lblUrl, txtUrl,
                lblNotas, txtNotas,
                btnSalvar, btnCancelar
            });
        }

        private async System.Threading.Tasks.Task BtnSalvar_Click(TextBox txtNomeServico, TextBox txtUsuario, TextBox txtSenha, ComboBox cmbCategoria, TextBox txtUrl, TextBox txtNotas)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNomeServico.Text) || string.IsNullOrWhiteSpace(txtUsuario.Text))
                {
                    MessageBox.Show("Preencha os campos obrigatórios", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var senhaAtualizada = string.IsNullOrWhiteSpace(txtSenha.Text) ? "SENHA_NAO_ALTERADA" : txtSenha.Text;

                if (senhaAtualizada == "SENHA_NAO_ALTERADA")
                {
                    var categoria = (Categoria)cmbCategoria.SelectedIndex;
                    _senhaAtual.NomeServico = txtNomeServico.Text;
                    _senhaAtual.Usuario = txtUsuario.Text;
                    _senhaAtual.Categoria = categoria;
                    _senhaAtual.Url = string.IsNullOrWhiteSpace(txtUrl.Text) ? null : txtUrl.Text;
                    _senhaAtual.Notas = string.IsNullOrWhiteSpace(txtNotas.Text) ? null : txtNotas.Text;
                    _senhaAtual.DataAtualizacao = DateTime.UtcNow;

                    await _servicoSenha.AtualizarSenhaAsync(
                        _senhaAtual.Id,
                        _senhaAtual.NomeServico,
                        _senhaAtual.Usuario,
                        "PLACEHOLDER_SENHA",
                        _senhaAtual.Categoria,
                        _senhaAtual.Url,
                        _senhaAtual.Notas);
                }
                else
                {
                    var categoria = (Categoria)cmbCategoria.SelectedIndex;
                    await _servicoSenha.AtualizarSenhaAsync(
                        _senhaAtual.Id,
                        txtNomeServico.Text,
                        txtUsuario.Text,
                        txtSenha.Text,
                        categoria,
                        string.IsNullOrWhiteSpace(txtUrl.Text) ? null : txtUrl.Text,
                        string.IsNullOrWhiteSpace(txtNotas.Text) ? null : txtNotas.Text);
                }

                await _servicoSenha.PersistirAsync();

                MessageBox.Show("Senha atualizada com sucesso", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar senha: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
