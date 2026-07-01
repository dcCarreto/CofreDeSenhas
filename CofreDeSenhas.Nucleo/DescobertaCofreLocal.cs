using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Nucleo;

public sealed record EstadoCofreLocal(
    bool Encontrado,
    bool Pronto,
    string Estado,
    string? Formato = null,
    string? Erro = null);

public sealed class DescobertaCofreLocal
{
    private const string NomePasta = "GerenciadorSenhas";
    private const string ArquivoAuth = "auth.dat";
    private const string ArquivoCofre = "senhas.json.enc";

    private readonly string? _pastaApp;

    public DescobertaCofreLocal(string? pastaApp = null)
    {
        _pastaApp = pastaApp;
    }

    public EstadoCofreLocal Verificar()
    {
        foreach (var pasta in ObterPastasSuportadas())
        {
            var caminhoAuth = Path.Combine(pasta, ArquivoAuth);
            var caminhoCofre = Path.Combine(pasta, ArquivoCofre);
            var authExiste = File.Exists(caminhoAuth);
            var cofreExiste = File.Exists(caminhoCofre);

            if (!authExiste && !cofreExiste)
                continue;

            if (!authExiste || !cofreExiste)
                return new EstadoCofreLocal(false, false, "incompleto",
                    Erro: "Arquivos auth.dat e senhas.json.enc devem existir juntos.");

            var auth = ValidarAuth(caminhoAuth);
            if (!auth.Pronto)
                return auth;

            var cofre = ValidarCofre(caminhoCofre);
            if (!cofre.Pronto)
                return cofre;

            return new EstadoCofreLocal(true, true, "pronto", EspecificacaoCriptografica.FormatoCofre);
        }

        return new EstadoCofreLocal(false, false, "nao_encontrado",
            Erro: "Nenhum cofre foi encontrado nos caminhos suportados.");
    }

    private IEnumerable<string> ObterPastasSuportadas()
    {
        if (!string.IsNullOrWhiteSpace(_pastaApp))
        {
            yield return _pastaApp;
            yield break;
        }

        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var basePath in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        })
        {
            if (string.IsNullOrWhiteSpace(basePath))
                continue;

            var pasta = Path.Combine(basePath, NomePasta);
            if (vistos.Add(pasta))
                yield return pasta;
        }
    }

    private static EstadoCofreLocal ValidarAuth(string caminho)
    {
        try
        {
            var texto = File.ReadAllText(caminho).Trim();
            var bytes = Convert.FromBase64String(texto);
            return bytes.Length >= EspecificacaoCriptografica.TamanhoMinimoAuth
                ? new EstadoCofreLocal(true, true, "auth_ok")
                : new EstadoCofreLocal(true, false, "formato_invalido",
                    Erro: "auth.dat nao tem o tamanho esperado.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new EstadoCofreLocal(true, false, "sem_permissao", Erro: ex.Message);
        }
        catch (IOException ex)
        {
            return new EstadoCofreLocal(true, false, "sem_permissao", Erro: ex.Message);
        }
        catch (FormatException)
        {
            return new EstadoCofreLocal(true, false, "formato_invalido",
                Erro: "auth.dat nao esta em Base64 valido.");
        }
    }

    private static EstadoCofreLocal ValidarCofre(string caminho)
    {
        try
        {
            string texto;
            using (var fs = new FileStream(caminho, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (var leitor = new StreamReader(fs))
            {
                texto = leitor.ReadToEnd().Trim();
            }

            var bytes = Convert.FromBase64String(texto);
            return bytes.Length >= EspecificacaoCriptografica.TamanhoMinimoPayloadCriptografado
                ? new EstadoCofreLocal(true, true, "cofre_ok", EspecificacaoCriptografica.FormatoCofre)
                : new EstadoCofreLocal(true, false, "formato_invalido",
                    Erro: "senhas.json.enc nao tem o tamanho minimo esperado.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new EstadoCofreLocal(true, false, "sem_permissao", Erro: ex.Message);
        }
        catch (IOException ex)
        {
            return new EstadoCofreLocal(true, false, "sem_permissao", Erro: ex.Message);
        }
        catch (FormatException)
        {
            return new EstadoCofreLocal(true, false, "formato_invalido",
                Erro: "senhas.json.enc nao esta em Base64 valido.");
        }
    }
}
