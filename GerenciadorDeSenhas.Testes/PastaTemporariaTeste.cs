namespace GerenciadorDeSenhas.Testes;

internal static class PastaTemporariaTeste
{
    public static string Criar(string prefixo)
    {
        var pasta = Path.Combine(Path.GetTempPath(), prefixo + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        return pasta;
    }

    public static void Apagar(string pasta)
    {
        try { if (Directory.Exists(pasta)) Directory.Delete(pasta, recursive: true); } catch { }
    }
}
