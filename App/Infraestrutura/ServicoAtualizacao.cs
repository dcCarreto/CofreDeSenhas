using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CofreDeSenhas
{
    internal static class ServicoAtualizacao
    {
        private const string UrlUltimaRelease = "https://api.github.com/repos/dcCarreto/CofreDeSenhas/releases/latest";
        public const string UrlPaginaReleases = "https://github.com/dcCarreto/CofreDeSenhas/releases";

        private sealed class RespostaRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }
        }

        public static async Task<string?> VerificarNovaVersaoAsync()
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("CofreDeSenhas-UpdateCheck");

                var json = await http.GetStringAsync(UrlUltimaRelease);
                var resposta = JsonSerializer.Deserialize<RespostaRelease>(json);
                var tag = resposta?.TagName;
                if (string.IsNullOrWhiteSpace(tag))
                    return null;

                var versaoRemota = ExtrairVersao(tag);
                var versaoAtual = typeof(ServicoAtualizacao).Assembly.GetName().Version;
                if (versaoRemota == null || versaoAtual == null || versaoRemota <= versaoAtual)
                    return null;

                return tag;
            }
            catch
            {
                return null;
            }
        }

        private static Version? ExtrairVersao(string tag) =>
            Version.TryParse(tag.TrimStart('v', 'V'), out var versao) ? versao : null;
    }
}
