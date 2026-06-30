using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using CofreDeSenhas.Nucleo;

var stdin = Console.OpenStandardInput();
var stdout = Console.OpenStandardOutput();

using var pipe = ConectarOuIniciarAgent();
if (pipe is null)
    return 1;

using var leitorPipe = new StreamReader(pipe, new UTF8Encoding(false));
using var escritorPipe = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };

while (true)
{
    var requisicao = LerMensagemNativa(stdin);
    if (requisicao is null)
        break;

    escritorPipe.WriteLine(requisicao);

    var resposta = leitorPipe.ReadLine();
    if (resposta is null)
        break;

    EscreverMensagemNativa(stdout, resposta);
}

return 0;

static string? LerMensagemNativa(Stream entrada)
{
    var prefixo = LerExatamente(entrada, 4);
    if (prefixo is null)
        return null;

    uint tamanho = BitConverter.ToUInt32(prefixo, 0);
    if (tamanho == 0)
        return string.Empty;
    if (tamanho > 64 * 1024 * 1024)
        return null;

    var corpo = LerExatamente(entrada, (int)tamanho);
    return corpo is null ? null : Encoding.UTF8.GetString(corpo);
}

static void EscreverMensagemNativa(Stream saida, string mensagem)
{
    var bytes = Encoding.UTF8.GetBytes(mensagem);
    saida.Write(BitConverter.GetBytes((uint)bytes.Length), 0, 4);
    saida.Write(bytes, 0, bytes.Length);
    saida.Flush();
}

static byte[]? LerExatamente(Stream entrada, int n)
{
    var buffer = new byte[n];
    int lidos = 0;
    while (lidos < n)
    {
        int r = entrada.Read(buffer, lidos, n - lidos);
        if (r == 0)
            return null;
        lidos += r;
    }
    return buffer;
}

static NamedPipeClientStream? ConectarOuIniciarAgent()
{
    for (int tentativa = 0; tentativa < 12; tentativa++)
    {
        var cliente = new NamedPipeClientStream(".", Protocolo.NomePipe, PipeDirection.InOut);
        try
        {
            cliente.Connect(500);
            return cliente;
        }
        catch (Exception ex) when (ex is TimeoutException or IOException)
        {
            cliente.Dispose();
            if (tentativa == 0)
                IniciarAgent();
            Thread.Sleep(400);
        }
    }
    return null;
}

static void IniciarAgent()
{
    var caminho = LocalizarAgent();
    if (caminho is null)
        return;

    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = caminho,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
    }
    catch
    {
    }
}

static string? LocalizarAgent()
{
    var dir = AppContext.BaseDirectory;
    var candidatos = new[]
    {
        Path.Combine(dir, "CofreDeSenhas.Agent.exe"),
        Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "..",
            "CofreDeSenhas.Agent", "bin",
#if DEBUG
            "Debug",
#else
            "Release",
#endif
            "net10.0-windows", "CofreDeSenhas.Agent.exe")),
    };
    return Array.Find(candidatos, File.Exists);
}
