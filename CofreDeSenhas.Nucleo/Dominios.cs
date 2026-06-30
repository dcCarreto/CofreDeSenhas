namespace CofreDeSenhas.Nucleo;

public static class Dominios
{
    public static string ExtrairHost(string? urlOuDominio)
    {
        if (string.IsNullOrWhiteSpace(urlOuDominio))
            return string.Empty;

        var texto = urlOuDominio.Trim();
        if (!texto.Contains("://", StringComparison.Ordinal))
            texto = "http://" + texto;

        return Uri.TryCreate(texto, UriKind.Absolute, out var uri)
            ? uri.Host.ToLowerInvariant()
            : string.Empty;
    }

    public static string Registravel(string? urlOuDominio)
    {
        var host = ExtrairHost(urlOuDominio);
        if (host.Length == 0)
            return string.Empty;

        var partes = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length <= 2)
            return host;

        var rotulos = SufixosPublicos.Contem($"{partes[^2]}.{partes[^1]}") ? 3 : 2;
        return partes.Length <= rotulos
            ? host
            : string.Join('.', partes[^rotulos..]);
    }

    public static bool Casa(string? dominioAba, string? urlSalva)
    {
        if (string.IsNullOrWhiteSpace(urlSalva))
            return false;

        var alvo = Registravel(dominioAba);
        return alvo.Length > 0 && alvo == Registravel(urlSalva);
    }
}
