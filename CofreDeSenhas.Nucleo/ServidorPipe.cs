using System.IO.Pipes;
using System.Text;

namespace CofreDeSenhas.Nucleo;

public sealed class ServidorPipe
{
    private readonly Processador _processador;
    private volatile bool _rodando;

    public ServidorPipe(Processador processador) => _processador = processador;

    public void Iniciar()
    {
        _rodando = true;
        var thread = new Thread(Loop) { IsBackground = true, Name = "CofreDeSenhas.Pipe" };
        thread.Start();
    }

    public void Parar() => _rodando = false;

    private void Loop()
    {
        while (_rodando)
        {
            NamedPipeServerStream servidor;
            try
            {
                servidor = new NamedPipeServerStream(
                    Protocolo.NomePipe,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
            }
            catch
            {
                Thread.Sleep(200);
                continue;
            }

            try
            {
                servidor.WaitForConnection();
            }
            catch
            {
                servidor.Dispose();
                continue;
            }

            ThreadPool.QueueUserWorkItem(_ => Atender(servidor));
        }
    }

    private void Atender(NamedPipeServerStream stream)
    {
        try
        {
            using (stream)
            using (var leitor = new StreamReader(stream, new UTF8Encoding(false)))
            using (var escritor = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
            {
                string? linha;
                while ((linha = leitor.ReadLine()) != null)
                {
                    if (linha.Length == 0)
                        continue;

                    var resposta = _processador.Processar(linha);
                    escritor.WriteLine(resposta);
                }
            }
        }
        catch
        {
        }
    }
}
