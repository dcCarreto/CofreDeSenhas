using System.Drawing;
using CofreDeSenhas.Nucleo;

namespace CofreDeSenhas.Agent;

internal sealed class AgentContext : ApplicationContext
{
    private readonly Form _ancora;
    private readonly SessaoCofre _sessao;
    private readonly ServidorPipe _servidor;

    public AgentContext()
    {
        _ancora = new Form
        {
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Size = new Size(1, 1),
        };
        _ = _ancora.Handle;

        _sessao = new SessaoCofre();
        _servidor = new ServidorPipe(new Processador(_sessao, SolicitarUnlock));
        _servidor.Iniciar();
    }

    private bool SolicitarUnlock()
    {
        if (_ancora.InvokeRequired)
            return (bool)_ancora.Invoke(SolicitarUnlock);

        return DialogoUnlock.Mostrar(_sessao, _ancora);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _servidor.Parar();
            _sessao.Dispose();
            _ancora.Dispose();
        }
        base.Dispose(disposing);
    }
}
