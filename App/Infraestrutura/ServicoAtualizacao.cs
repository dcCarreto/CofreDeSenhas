using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CofreDeSenhas
{
    internal enum TipoAtualizacao { Instalador, Portatil, AppImage }

    internal readonly record struct AtualizacaoDisponivel(string Tag, string? NotasVersao);

    // Arquivo da atualização já baixado e conferido (assinatura do CHECKSUMS + SHA256),
    // esperando o momento de ser instalado.
    // ExigeAdministrador: instalação "para todos os usuários" (em Arquivos de Programas).
    // O instalador herda esse modo (UsePreviousPrivileges do Inno) e pede elevação.
    internal sealed record AtualizacaoBaixada(string Tag, string Caminho, string Hash, TipoAtualizacao Tipo, bool ExigeAdministrador = false);

    internal readonly record struct ResultadoDownload(AtualizacaoBaixada? Baixada, string? Falha)
    {
        public static ResultadoDownload Ok(AtualizacaoBaixada baixada) => new(baixada, null);
        public static ResultadoDownload Erro(string mensagem) => new(null, mensagem);
    }

    internal static class ServicoAtualizacao
    {
        private const string UrlUltimaRelease = "https://api.github.com/repos/dcCarreto/CofreDeSenhas/releases/latest";
        public const string UrlPaginaReleases = "https://github.com/dcCarreto/CofreDeSenhas/releases";
        private const string NomeChecksums = "CHECKSUMS.txt";
        private const string NomeAssinaturaChecksums = "CHECKSUMS.txt.sig";

        // Par da privada no secret UPDATE_SIGNING_KEY. Trocar as duas juntas — uma release
        // assinada com a chave nova quebra em quem ainda tem a pública antiga.
        private const string ChavePublicaAtualizacao = """
            -----BEGIN PUBLIC KEY-----
            MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAtP2TJOKW43UqVpBhMVjJ
            KclYUo8YiHNO7R3Fydh81DPMErRiY2yq2LexJXjAXCx4L+7UG7TjMO0ytRn7mH/j
            eNh47ZuZMhKnIuxjpFK3UgW1DnKtAElcwmU1Ug5nCTqblPFEoz451VB0wHIT5f60
            Lf2U6x/g2MMDOlVMdRSG8GGuhXIy+McT0aQyINImpV0ofrGgtayg9WZFYZcHJ1/h
            JfPRKDkQc3j3zD7WVcvfTv7sXc3vFQtQQQnh64SjQT9L/LUsUYaBY0/TnzDH09G9
            MLTATLPSHT5ND6UkPSmAp1+kkovSOggiFWMjwvriNeTEySKCtQVx6cqWxIxloJyq
            wlYXdchLZTmfGZpUL8gkKC90cYzttf/jCrdaXUKQ2gi7kPIfoTw3QuybwYegXEMP
            QisGJ1+M5o9Fys2rHGmYeXZtP/xBL4hA30pTA+r2xhJskIfTcBhQDXuNd1UZs7mq
            N4P28+68JcoVyug5TCc1N5kjrKzw6YPErAtoUozF0DX9L2WI6QwLwBvbgMHCg4qS
            owIScMX6IdIVDcBeHCeMt8UOYddgyCnEzHZNSgNqDuZcGU0FkbZlvlnB3dFHtE/0
            ticq58bkB/BQj+WDFI5ABy+f2ZjG+sT7lc3RQWsFJobf6Ew2is/+LUR+oOfsy0lZ
            Dv2YOPd/vclwT1T1iu0Opm0CAwEAAQ==
            -----END PUBLIC KEY-----
            """;

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

        // Argumento com que o app se reabre depois de trocar o próprio executável
        // (portátil/AppImage): a instância nova espera a antiga liberar o mutex de
        // instância única em vez de só ativá-la e sair — ver Program.Main.
        public const string ArgumentoAposAtualizacao = "--apos-atualizacao";

        // A atualização embutida só vale para o app instalado ou portátil de verdade.
        // Na Microsoft Store quem atualiza é a Loja; num build de desenvolvimento
        // (Debug ou COFRE_BASE) ela trocaria o .exe de bin/ por uma release. No Linux,
        // só rodando como AppImage — o único pacote de arquivo único, análogo ao
        // portátil do Windows.
        public static bool Suportada =>
            !PacoteLoja.Empacotado &&
            !GerenciadorDeSenhas.AmbienteCofre.Isolado &&
            (OperatingSystem.IsWindows() ||
             (OperatingSystem.IsLinux() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE"))));

        // Baixa e confere a atualização da release `tag`, sem instalar. O arquivo fica
        // numa pasta temporária até AplicarAsync (ou Descartar).
        public static async Task<ResultadoDownload> BaixarAsync(string tag)
        {
            if (!Suportada)
                return ResultadoDownload.Erro(Idioma.Texto("Update.Error.ExecutableNotFound"));

            string? caminhoTemp = null;
            try
            {
                using var httpMeta = CriarHttpClient(TimeSpan.FromSeconds(15));
                var json = await httpMeta.GetStringAsync(UrlUltimaRelease);
                var resposta = JsonSerializer.Deserialize<RespostaRelease>(json);
                if (resposta?.Assets is not { Count: > 0 })
                    return ResultadoDownload.Erro(Idioma.Texto("Update.Error.NoAssets"));

                var versaoTexto = tag.TrimStart('v', 'V');

                TipoAtualizacao tipo;
                string nomeAtivo;
                if (OperatingSystem.IsLinux())
                {
                    // $APPIMAGE (convenção do runtime do AppImage) aponta pro .AppImage de
                    // verdade; Environment.ProcessPath aponta pro ponto de montagem FUSE.
                    tipo = TipoAtualizacao.AppImage;
                    nomeAtivo = $"CofreDeSenhas-{versaoTexto}-x86_64.AppImage";
                }
                else
                {
                    var instaladoViaSetup = File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));
                    if (!instaladoViaSetup && string.IsNullOrEmpty(Environment.ProcessPath))
                        return ResultadoDownload.Erro(Idioma.Texto("Update.Error.ExecutableNotFound"));

                    tipo = instaladoViaSetup ? TipoAtualizacao.Instalador : TipoAtualizacao.Portatil;
                    nomeAtivo = instaladoViaSetup
                        ? $"CofreDeSenhas-Setup-{versaoTexto}.exe"
                        : $"CofreDeSenhas-{versaoTexto}-win-x64-portatil.exe";
                }

                var ativo = resposta.Assets.Find(a => string.Equals(a.Name, nomeAtivo, StringComparison.OrdinalIgnoreCase));
                var ativoChecksums = resposta.Assets.Find(a => string.Equals(a.Name, NomeChecksums, StringComparison.OrdinalIgnoreCase));
                var ativoAssinatura = resposta.Assets.Find(a => string.Equals(a.Name, NomeAssinaturaChecksums, StringComparison.OrdinalIgnoreCase));
                if (ativo?.UrlDownload == null || ativoChecksums?.UrlDownload == null)
                    return ResultadoDownload.Erro(Idioma.Texto("Update.Error.AssetNotFound"));

                if (!ChavePublicaAtualizacaoUtilizavel() || ativoAssinatura?.UrlDownload == null)
                    return ResultadoDownload.Erro(Idioma.Texto("Update.Error.SignatureMissing"));

                var checksumsBytes = await httpMeta.GetByteArrayAsync(ativoChecksums.UrlDownload);
                var assinaturaChecksums = await httpMeta.GetByteArrayAsync(ativoAssinatura.UrlDownload);
                if (!VerificarAssinaturaChecksums(ChavePublicaAtualizacao, checksumsBytes, assinaturaChecksums))
                    return ResultadoDownload.Erro(Idioma.Texto("Update.Error.SignatureInvalid"));

                var checksums = Encoding.UTF8.GetString(checksumsBytes);
                var hashEsperado = ExtrairHash(checksums, nomeAtivo);
                if (hashEsperado == null)
                    return ResultadoDownload.Erro(Idioma.Texto("Update.Error.ChecksumUnavailable"));

                // Pasta com nome aleatório, não Path.GetTempPath() direto: o nome do
                // arquivo (nomeAtivo) é previsível a partir só da tag da release, e em
                // Linux o temp compartilhado (/tmp) é gravável por qualquer usuário
                // local — sem isto, dava pra pré-posicionar um arquivo/link no caminho
                // exato antes mesmo do download começar.
                var pastaTemp = Path.Combine(Path.GetTempPath(), "CofreDeSenhas-update-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(pastaTemp);
                caminhoTemp = Path.Combine(pastaTemp, nomeAtivo);
                using (var httpDownload = CriarHttpClient(TimeSpan.FromMinutes(10)))
                    await BaixarArquivoAsync(httpDownload, ativo.UrlDownload, caminhoTemp);

                var hashObtido = await CalcularSha256Async(caminhoTemp);
                if (!string.Equals(hashObtido, hashEsperado, StringComparison.OrdinalIgnoreCase))
                {
                    LimparPastaTemp(caminhoTemp);
                    return ResultadoDownload.Erro(Idioma.Texto("Update.Error.ChecksumMismatch"));
                }

                return ResultadoDownload.Ok(new AtualizacaoBaixada(tag, caminhoTemp, hashEsperado, tipo,
                    ExigeAdministrador: tipo == TipoAtualizacao.Instalador && InstaladoParaTodosUsuarios()));
            }
            catch (Exception ex)
            {
                if (caminhoTemp != null)
                    LimparPastaTemp(caminhoTemp);
                return ResultadoDownload.Erro(ErrosUi.MensagemAmigavel(ex));
            }
        }

        // Instala uma atualização já baixada. Confere o SHA256 de novo: entre o download
        // e a instalação (o próximo bloqueio do cofre) podem ter passado horas, e o que
        // vai ser executado é o que está no disco agora. Se voltar sem exceção, quem
        // chamou deve encerrar o app: o instalador silencioso fecha e reabre o app
        // sozinho, e no portátil/AppImage a instância nova já foi iniciada.
        public static async Task AplicarAsync(AtualizacaoBaixada atualizacao)
        {
            var hashAtual = await CalcularSha256Async(atualizacao.Caminho);
            if (!string.Equals(hashAtual, atualizacao.Hash, StringComparison.OrdinalIgnoreCase))
            {
                LimparPastaTemp(atualizacao.Caminho);
                throw new InvalidOperationException(Idioma.Texto("Update.Error.ChecksumMismatch"));
            }

            switch (atualizacao.Tipo)
            {
                case TipoAtualizacao.Instalador:
                    Process.Start(new ProcessStartInfo(atualizacao.Caminho, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART") { UseShellExecute = true });
                    break;
                case TipoAtualizacao.Portatil:
                    AtualizarPortatil(atualizacao.Caminho);
                    break;
                case TipoAtualizacao.AppImage:
                    AtualizarAppImage(atualizacao.Caminho, Environment.GetEnvironmentVariable("APPIMAGE")!);
                    break;
            }
        }

        public static void Descartar(AtualizacaoBaixada atualizacao) => LimparPastaTemp(atualizacao.Caminho);

        private static bool InstaladoParaTodosUsuarios()
        {
            var pastaApp = Path.GetFullPath(AppContext.BaseDirectory);
            foreach (var pasta in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
            {
                var programas = Environment.GetFolderPath(pasta);
                if (!string.IsNullOrEmpty(programas) &&
                    pastaApp.StartsWith(Path.TrimEndingDirectorySeparator(programas) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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

            Process.Start(new ProcessStartInfo(exeAtual, ArgumentoAposAtualizacao) { UseShellExecute = true });
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

            Process.Start(new ProcessStartInfo(appImageAtual, ArgumentoAposAtualizacao) { UseShellExecute = false });
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

        internal static bool ChavePublicaAtualizacaoUtilizavel()
        {
            if (string.IsNullOrWhiteSpace(ChavePublicaAtualizacao))
                return false;

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(ChavePublicaAtualizacao);
                return rsa.KeySize >= 3072;
            }
            catch
            {
                return false;
            }
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
