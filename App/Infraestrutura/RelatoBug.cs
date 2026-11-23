using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace CofreDeSenhas
{
    internal readonly record struct ResultadoRelato(string? UrlIssue, string? Falha);

    // Abre uma issue no repositório com o relato do usuário e o relatório de diagnóstico.
    internal static class RelatoBug
    {
        private const string UrlIssues = "https://api.github.com/repos/dcCarreto/CofreDeSenhas/issues";

        // Token fine-grained com só "Issues: write" neste repositório, injetado na
        // release pelo secret COFRE_TOKEN_RELATO (App.csproj). Build local fica sem ele.
        // shortcut: o token vai legível no binário (no pior caso, alguém abre issues em
        // nome do app); trocar por um proxy próprio se aparecer abuso.
        private static readonly string? Token = typeof(RelatoBug).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "TokenRelatoBug")?.Value;

        public static bool Disponivel => !string.IsNullOrWhiteSpace(Token);

        internal static (string Titulo, string Corpo) MontarIssue(string descricao, string relatorio)
        {
            descricao = descricao.Trim();
            var primeiraLinha = descricao.Split('\n')[0].Trim();
            var titulo = "[Relato] " + (primeiraLinha.Length > 80 ? primeiraLinha[..80] + "…" : primeiraLinha);
            // Cerca de 4 crases: o log nunca fecha o bloco antes da hora.
            var corpo = $"""
                ## Relato do usuário

                {descricao}

                ## Diagnóstico

                <details><summary>Ambiente e log de erros</summary>

                ````text
                {relatorio}
                ````

                </details>
                """;
            return (titulo, corpo);
        }

        public static async Task<ResultadoRelato> EnviarAsync(string descricao, string relatorio)
        {
            if (!Disponivel)
                return new(null, Idioma.Texto("BugReport.Unavailable"));

            try
            {
                var (titulo, corpo) = MontarIssue(descricao, relatorio);
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("CofreDeSenhas-BugReport");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                http.DefaultRequestHeaders.Authorization = new("Bearer", Token);

                using var resposta = await http.PostAsJsonAsync(UrlIssues,
                    new { title = titulo, body = corpo, labels = new[] { "bug" } });
                if (!resposta.IsSuccessStatusCode)
                {
                    Diagnostico.Registrar($"GitHub respondeu {(int)resposta.StatusCode}", "RelatoBug");
                    return new(null, $"HTTP {(int)resposta.StatusCode}");
                }

                using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
                return new(json.RootElement.GetProperty("html_url").GetString(), null);
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "RelatoBug");
                return new(null, ex.Message);
            }
        }
    }
}
