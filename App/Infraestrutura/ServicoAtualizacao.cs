using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CofreDeSenhas
{
    internal enum ResultadoAtualizacaoTipo { Sucesso, NaoSuportado, Falha }

    internal readonly record struct ResultadoAtualizacao(ResultadoAtualizacaoTipo Tipo, string? Mensagem = null)
    {
        public static ResultadoAtualizacao Sucesso() => new(ResultadoAtualizacaoTipo.Sucesso);
        public static ResultadoAtualizacao NaoSuportado() => new(ResultadoAtualizacaoTipo.NaoSuportado);
        public static ResultadoAtualizacao Falha(string mensagem) => new(ResultadoAtualizacaoTipo.Falha, mensagem);
    }

    internal static class ServicoAtualizacao
    {
        private const string UrlUltimaRelease = "https://api.github.com/repos/dcCarreto/CofreDeSenhas/releases/latest";
        public const string UrlPaginaReleases = "https://github.com/dcCarreto/CofreDeSenhas/releases";
        private const string NomeChecksums = "CHECKSUMS.txt";

        private sealed class RespostaRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("assets")]
            public List<Ativo>? Assets { get; set; }
        }

        private sealed class Ativo
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("browser_download_url")]
            public string? UrlDownload { get; set; }
        }

        public static async Task<string?> VerificarNovaVersaoAsync()
        {
            try
            {
                using var http = CriarHttpClient(TimeSpan.FromSeconds(10));
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

        public static async Task<ResultadoAtualizacao> AtualizarAgoraAsync(string tag)
        {
            if (!OperatingSystem.IsWindows())
                return ResultadoAtualizacao.NaoSuportado();

            try
            {
                using var httpMeta = CriarHttpClient(TimeSpan.FromSeconds(15));
                var json = await httpMeta.GetStringAsync(UrlUltimaRelease);
                var resposta = JsonSerializer.Deserialize<RespostaRelease>(json);
                if (resposta?.Assets is not { Count: > 0 })
                    return ResultadoAtualizacao.Falha("Não foi possível obter os arquivos da versão.");

                var instaladoViaSetup = File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));
                if (!instaladoViaSetup && string.IsNullOrEmpty(Environment.ProcessPath))
                    return ResultadoAtualizacao.Falha("Não foi possível localizar o executável atual.");

                var versaoTexto = tag.TrimStart('v', 'V');
                var nomeAtivo = instaladoViaSetup
                    ? $"CofreDeSenhas-Setup-{versaoTexto}.exe"
                    : $"CofreDeSenhas-{versaoTexto}-win-x64-portatil.exe";

                var ativo = resposta.Assets.Find(a => string.Equals(a.Name, nomeAtivo, StringComparison.OrdinalIgnoreCase));
                var ativoChecksums = resposta.Assets.Find(a => string.Equals(a.Name, NomeChecksums, StringComparison.OrdinalIgnoreCase));
                if (ativo?.UrlDownload == null || ativoChecksums?.UrlDownload == null)
                    return ResultadoAtualizacao.Falha("Arquivo de atualização não encontrado nesta versão.");

                var checksums = await httpMeta.GetStringAsync(ativoChecksums.UrlDownload);
                var hashEsperado = ExtrairHash(checksums, nomeAtivo);
                if (hashEsperado == null)
                    return ResultadoAtualizacao.Falha("Não foi possível verificar a integridade do arquivo.");

                var caminhoTemp = Path.Combine(Path.GetTempPath(), nomeAtivo);
                using (var httpDownload = CriarHttpClient(TimeSpan.FromMinutes(3)))
                    await BaixarArquivoAsync(httpDownload, ativo.UrlDownload, caminhoTemp);

                var hashObtido = await CalcularSha256Async(caminhoTemp);
                if (!string.Equals(hashObtido, hashEsperado, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(caminhoTemp);
                    return ResultadoAtualizacao.Falha("A verificação de integridade do arquivo baixado falhou.");
                }

                if (instaladoViaSetup)
                    Process.Start(new ProcessStartInfo(caminhoTemp, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART") { UseShellExecute = true });
                else
                    AtualizarPortatil(caminhoTemp);

                return ResultadoAtualizacao.Sucesso();
            }
            catch (Exception ex)
            {
                return ResultadoAtualizacao.Falha(ex.Message);
            }
        }

        private static void AtualizarPortatil(string caminhoNovoExe)
        {
            var exeAtual = Environment.ProcessPath!;
            var exeAntigo = exeAtual + ".old";

            if (File.Exists(exeAntigo))
                File.Delete(exeAntigo);

            File.Move(exeAtual, exeAntigo);
            File.Move(caminhoNovoExe, exeAtual);

            Process.Start(new ProcessStartInfo(exeAtual) { UseShellExecute = true });
        }

        private static async Task BaixarArquivoAsync(HttpClient http, string url, string destino)
        {
            using var resposta = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resposta.EnsureSuccessStatusCode();
            await using var origem = await resposta.Content.ReadAsStreamAsync();
            await using var arquivo = File.Create(destino);
            await origem.CopyToAsync(arquivo);
        }

        private static async Task<string> CalcularSha256Async(string caminho)
        {
            await using var stream = File.OpenRead(caminho);
            var hash = await SHA256.HashDataAsync(stream);
            return Convert.ToHexString(hash);
        }

        internal static string? ExtrairHash(string conteudoChecksums, string nomeArquivo)
        {
            foreach (var linhaBruta in conteudoChecksums.Split('\n'))
            {
                var linha = linhaBruta.Trim();
                if (linha.Length == 0)
                    continue;

                var partes = linha.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length == 2 && partes[1].TrimStart('*').Equals(nomeArquivo, StringComparison.OrdinalIgnoreCase))
                    return partes[0];
            }
            return null;
        }

        private static HttpClient CriarHttpClient(TimeSpan timeout)
        {
            var http = new HttpClient { Timeout = timeout };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CofreDeSenhas-UpdateCheck");
            return http;
        }

        internal static Version? ExtrairVersao(string tag) =>
            Version.TryParse(tag.TrimStart('v', 'V'), out var versao) ? versao : null;
    }
}
