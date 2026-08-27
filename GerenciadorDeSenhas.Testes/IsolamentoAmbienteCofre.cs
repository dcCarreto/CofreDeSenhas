using System.Runtime.CompilerServices;

namespace GerenciadorDeSenhas.Testes;

internal static class IsolamentoAmbienteCofre
{
    // Roda uma vez, antes de qualquer teste do módulo: aponta COFRE_BASE pra uma
    // pasta temporária exclusiva desta execução, apagada quando o processo encerra.
    // AmbienteCofre lê COFRE_BASE só na primeira vez que é tocado, sempre depois
    // deste inicializador — então a suíte nunca escreve no %APPDATA% de um cofre
    // real, mesmo num teste que esqueça de passar pastaApp explícito.
    [ModuleInitializer]
    internal static void Isolar()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COFRE_BASE")))
            return;

        var raiz = Path.Combine(Path.GetTempPath(), "CofreDeSenhasTestes",
            "ambiente-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(raiz);
        Environment.SetEnvironmentVariable("COFRE_BASE", raiz);

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { Directory.Delete(raiz, recursive: true); } catch { }
        };
    }
}
