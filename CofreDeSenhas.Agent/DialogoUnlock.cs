using System.Drawing;
using CofreDeSenhas.Nucleo;

namespace CofreDeSenhas.Agent;

internal sealed class DialogoUnlock : Form
{
    private readonly SessaoCofre _sessao;
    private readonly TextBox _txtSenha;
    private readonly Label _lblErro;
    private readonly Button _btnOk;

    private DialogoUnlock(SessaoCofre sessao)
    {
        _sessao = sessao;

        Text = "Cofre de Senhas — desbloquear";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(372, 156);

        var lbl = new Label
        {
            Text = "Digite a senha mestra para a extensão acessar suas credenciais:",
            Location = new Point(12, 12),
            Size = new Size(348, 36),
        };

        _txtSenha = new TextBox
        {
            UseSystemPasswordChar = true,
            Location = new Point(12, 54),
            Size = new Size(348, 23),
        };

        _lblErro = new Label
        {
            ForeColor = Color.Firebrick,
            Location = new Point(12, 84),
            Size = new Size(348, 18),
            Text = string.Empty,
        };

        _btnOk = new Button
        {
            Text = "Desbloquear",
            Location = new Point(192, 116),
            Size = new Size(84, 30),
        };
        _btnOk.Click += (_, _) => Confirmar();

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Location = new Point(282, 116),
            Size = new Size(78, 30),
        };

        Controls.AddRange(new Control[] { lbl, _txtSenha, _lblErro, _btnOk, btnCancelar });
        AcceptButton = _btnOk;
        CancelButton = btnCancelar;
    }

    private async void Confirmar()
    {
        _btnOk.Enabled = false;
        _lblErro.Text = string.Empty;

        var destrancou = await _sessao.DestrancarAsync(_txtSenha.Text);
        if (destrancou)
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        _lblErro.Text = "Senha mestra incorreta.";
        _txtSenha.SelectAll();
        _txtSenha.Focus();
        _btnOk.Enabled = true;
    }

    public static bool Mostrar(SessaoCofre sessao, IWin32Window dono)
    {
        using var dlg = new DialogoUnlock(sessao);
        dlg.Shown += (_, _) =>
        {
            dlg.Activate();
            dlg.BringToFront();
        };
        dlg.ShowDialog(dono);
        return sessao.Destrancado;
    }
}
