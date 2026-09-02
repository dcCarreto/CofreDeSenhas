using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CofreDeSenhas
{
    internal enum ResultadoAtualizacaoTipo { Sucesso, NaoSuportado, Falha }

    internal readonly record struct AtualizacaoDisponivel(string Tag, string? NotasVersao);

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
        private const string NomeAssinaturaChecksums = "CHECKSUMS.txt.sig";

        // Par da chave privada mantida no secret UPDATE_SIGNING_KEY do workflow de release.
        // Enquanto estiver vazia, a atualização em um clique é recusada (fail-closed) e o
        // usuário é mandado para a página de releases.
        private const string ChavePublicaAtualizacao = "";

        private sealed class RespostaRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("body")]
            public string? Body { get; set; }

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

        public static async Task<AtualizacaoDisponivel?> VerificarNovaVersaoAsync()
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

                return new AtualizacaoDisponivel(tag, resposta?.Body);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<ResultadoAtualizacao> AtualizarAgoraAsync(string tag)
        {
            if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
                return ResultadoAtualizacao.NaoSuportado();

            // No Linux só dá pra fazer a troca em um clique rodando como AppImage: é o único
            // pacote de arquivo único, análogo ao portátil do Windows. A variável $APPIMAGE
            // (convenção do runtime do AppImage) aponta pro .AppImage de verdade — Environment.
            // ProcessPath aponta pro ponto de montagem FUSE temporário, não serve aqui.
            string? appImageAtual = null;
            if (OperatingSystem.IsLinux())
            {
                appImageAtual = Environment.GetEnvironmentVariable("APPIMAGE");
                if (string.IsNullOrEmpty(appImageAtual))
                    return ResultadoAtualizacao.NaoSuportado();
            }

            string? caminhoTemp = null;
            try
            {
                using var httpMeta = CriarHttpClient(TimeSpan.FromSeconds(15));
                var json = await httpMeta.GetStringAsync(UrlUltimaRelease);
                var resposta = JsonSerializer.Deserialize<RespostaRelease>(json);
                if (resposta?.Assets is not { Count: > 0 })
                    return ResultadoAtualizacao.Falha(Idioma.Texto("Update.Error.NoAssets"));

                var versaoTexto = tag.TrimStart('v', 'V');

                var instaladoViaSetup = false;
                string nomeAtivo;
                if (appImageAtual != null)
                {
                    nomeAtivo = $"CofreDeSenhas-{versaoTexto}-x86_64.AppImage";
                }
                else
                {
                    instaladoViaSetup = File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));
                    if (!instaladoViaSetup && string.IsNullOrEmpty(Environment.ProcessPath))
                        return ResultadoAtualizacao.Falha(Idioma.Texto("Update.Error.ExecutableNotFound"));

                    nomeAtivo = instaladoViaSetup
                        ? $"CofreDeSenhas-Setup-{versaoTexto}.exe"
                        : $"CofreDeSenhas-{versaoTexto}-win-x64-portatil.exe";
                }

                var ativo = resposta.Assets.Find(a => string.Equals(a.Name, nomeAtivo, StringComparison.OrdinalIgnoreCase));
                var ativoChecksums = resposta.Assets.Find(a => string.Equals(a.Name, NomeChecksums, StringComparison.OrdinalIgnoreCase));
                var ativoAssinatura = resposta.Assets.Find(a => string.Equals(a.Name, NomeAssinaturaChecksums, StringComparison.OrdinalIgnoreCase));
                if (ativo?.UrlDownload == null || ativoChecksums?.UrlDownload == null)
                    return ResultadoAtualizacao.Falha(Idioma.Texto("Update.Error.AssetNotFound"));

                if (string.IsNullOrWhiteSpace(ChavePublicaAtualizacao) || ativoAssinatura?.UrlDownload == null)
                    return ResultadoAtualizacao.Falha(Idioma.Texto("Update.Error.SignatureMissing"));

                var checksumsBytes = await httpMeta.GetByteArrayAsync(ativoChecksums.UrlDownload);
                var assinaturaChecksums = await httpMeta.GetByteArrayAsync(ativoAssinatura.UrlDownload);
                if (!VerificarAssinaturaChecksums(ChavePublicaAtualizacao, checksumsBytes, assinaturaChecksums))
                    return ResultadoAtualizacao.Falha(Idioma.Texto("Update.Error.SignatureInvalid"));

                var checksums = Encoding.UTF8.GetString(checksumsBytes);
                var hashEsperado = ExtrairHash(checksums, nomeAtivo);
                if (hashEsperado == null)
                    return ResultadoAtualizacao.Falha(Idioma.Texto("Update.Error.ChecksumUnavailable"));

                // Pasta com nome aleatório, não Path.GetTempPath() direto: o nome do
                // arquivo (nomeAtivo) é previsível a partir só da tag da release, e em
                // Linux o temp compartilhado (/tmp) é gravável por qualquer usuário
                // local — sem isto, dava pra pré-posicionar um arquivo/link no caminho
                // exato antes mesmo do download começar, e a checagem de hash abaixo
                // verificaria o arquivo baixado, mas o Move/Process.Start um passo
                // adiante poderia acabar pegando outra coisa se algo trocasse o
                // conteúdo bem no intervalo entre os dois.
                var pastaTemp = Path.Combine(Path.GetTempPath(), "CofreDeSenhas-update-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(pastaTemp);
                caminhoTemp = Path.Combine(pastaTemp, nomeAtivo);
                using (var httpDownload = CriarHttpClient(TimeSpan.FromMinutes(3)))
                    await BaixarArquivoAsync(httpDownload, ativo.UrlDownload, caminhoTemp);

                var hashObtido = await CalcularSha256Async(caminhoTemp);
                if (!string.Equals(hashObtido, hashEsperado, StringComparison.OrdinalIgnoreCase))
                {
                    LimparPastaTemp(caminhoTemp);
                    return ResultadoAtualizacao.Falha(Idioma.Texto("Update.Error.ChecksumMismatch"));
                }

                if (appImageAtual != null)
                    AtualizarAppImage(caminhoTemp, appImageAtual);
                else if (instaladoViaSetup)
                    Process.Start(new ProcessStartInfo(caminhoTemp, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART") { UseShellExecute = true });
                else
                    AtualizarPortatil(caminhoTemp);

                return ResultadoAtualizacao.Sucesso();
            }
            catch (Exception ex)
            {
                if (caminhoTemp != null)
                    LimparPastaTemp(caminhoTemp);
                return ResultadoAtualizacao.Falha(ErrosUi.MensagemAmigavel(ex));
            }
        }

        private static void LimparPastaTemp(string caminhoArquivo)
        {
            try
            {
                var pasta = Path.GetDirectoryName(caminhoArquivo);
                if (!string.IsNullOrEmpty(pasta) && Directory.Exists(pasta))
                    Directory.Delete(pasta, recursive: true);
            }
            catch { }
        }

        private static void AtualizarPortatil(string caminhoNovoExe)
        {
            var exeAtual = Environment.ProcessPath!;
            var exeAntigo = exeAtual + ".old";

            if (File.Exists(exeAntigo))
                File.Delete(exeAntigo);

            File.Move(exeAtual, exeAntigo);
            try
            {
                File.Move(caminhoNovoExe, exeAtual);
            }
            catch
            {
                File.Move(exeAntigo, exeAtual);
                throw;
            }

            Process.Start(new ProcessStartInfo(exeAtual) { UseShellExecute = true });
        }

        private static void AtualizarAppImage(string caminhoNovoAppImage, string appImageAtual)
        {
            if (OperatingSystem.IsLinux())
            {
                File.SetUnixFileMode(caminhoNovoAppImage,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            var appImageAntigo = appImageAtual + ".old";
            if (File.Exists(appImageAntigo))
                File.Delete(appImageAntigo);

            File.Move(appImageAtual, appImageAntigo);
            try
            {
                File.Move(caminhoNovoAppImage, appImageAtual);
            }
            catch
            {
                File.Move(appImageAntigo, appImageAtual);
                throw;
            }

            Process.Start(new ProcessStartInfo(appImageAtual) { UseShellExecute = false });
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

        internal static bool VerificarAssinaturaChecksums(string chavePublicaPem, byte[] conteudo, byte[] assinatura)
        {
            if (string.IsNullOrWhiteSpace(chavePublicaPem))
                return false;

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(chavePublicaPem);
                return rsa.VerifyData(conteudo, assinatura, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
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
