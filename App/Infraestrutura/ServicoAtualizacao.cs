using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CofreDeSenhas
{
    internal enum TipoAtualizacao { Instalador, AppImage, Deb }

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

                return new AtualizacaoDisponivel(tag, NotasSemGuiaDeArquivos(resposta?.Body));
            }
            catch
            {
                return null;
            }
        }

        // Argumento com que o app se reabre depois de trocar o próprio arquivo
        // (AppImage): a instância nova espera a antiga liberar o mutex de
        // instância única em vez de só ativá-la e sair — ver Program.Main.
        public const string ArgumentoAposAtualizacao = "--apos-atualizacao";

        // A atualização embutida só vale para o app de verdade. Na Microsoft Store quem
        // atualiza é a Loja; num build de desenvolvimento (Debug ou COFRE_BASE) ela
        // instalaria uma release por cima do ambiente de teste. No Linux, só como AppImage
        // ou pelo pacote .deb — compilado do código-fonte não tem como se trocar.
        public static bool Suportada =>
            !PacoteLoja.Empacotado &&
            !GerenciadorDeSenhas.AmbienteCofre.Isolado &&
            (OperatingSystem.IsWindows() ||
             (OperatingSystem.IsLinux() &&
              (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE")) || PacoteDeb.Instalado)));

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
                if (PacoteDeb.Instalado)
                {
                    tipo = TipoAtualizacao.Deb;
                    nomeAtivo = $"cofre-de-senhas_{versaoTexto}_amd64.deb";
                }
                else if (OperatingSystem.IsLinux())
                {
                    // $APPIMAGE (convenção do runtime do AppImage) aponta pro .AppImage de
                    // verdade; Environment.ProcessPath aponta pro ponto de montagem FUSE.
                    tipo = TipoAtualizacao.AppImage;
                    nomeAtivo = $"CofreDeSenhas-{versaoTexto}-x86_64.AppImage";
                }
                else
                {
                    // Sempre o instalador, inclusive no executável solto (o antigo
                    // portátil, descontinuado): ele migra para a versão instalada —
                    // ver ArgumentosInstalador e InstalacaoWindows.
                    tipo = TipoAtualizacao.Instalador;
                    nomeAtivo = $"CofreDeSenhas-Setup-{versaoTexto}.exe";
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

                // O atualizar-pacote roda como root e confere tudo de novo por conta própria.
                if (tipo == TipoAtualizacao.Deb)
                {
                    await File.WriteAllBytesAsync(Path.Combine(pastaTemp, NomeChecksums), checksumsBytes);
                    await File.WriteAllBytesAsync(Path.Combine(pastaTemp, NomeAssinaturaChecksums), assinaturaChecksums);
                }

                return ResultadoDownload.Ok(new AtualizacaoBaixada(tag, caminhoTemp, hashEsperado, tipo,
                    ExigeAdministrador: tipo == TipoAtualizacao.Instalador &&
                        InstalacaoWindows.InstaladoViaSetup && InstaladoParaTodosUsuarios()));
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
        // sozinho, e no AppImage a instância nova já foi iniciada.
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
                    Process.Start(new ProcessStartInfo(atualizacao.Caminho, ArgumentosInstalador(InstalacaoWindows.InstaladoViaSetup)) { UseShellExecute = true });
                    break;
                case TipoAtualizacao.AppImage:
                    AtualizarAppImage(atualizacao.Caminho, Environment.GetEnvironmentVariable("APPIMAGE")!);
                    break;
                case TipoAtualizacao.Deb:
                    await AtualizarDebAsync(atualizacao.Caminho);
                    break;
            }
        }

        public static void Descartar(AtualizacaoBaixada atualizacao) => LimparPastaTemp(atualizacao.Caminho);

        // Instalação existente: o Inno repete o modo dela (usuário atual ou todos os
        // usuários). Executável solto (antigo portátil): instala só para o usuário
        // atual, sem pedir administrador — dá para fazer sem ninguém por perto, e os
        // dados já estão em %APPDATA%, os mesmos que o executável solto usava.
        internal static string ArgumentosInstalador(bool instaladoViaSetup) =>
            instaladoViaSetup
                ? "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART"
                : "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CURRENTUSER";

        // Marca onde começa, no corpo da release, a tabela que explica cada arquivo
        // (montada pelo workflow de release). Interessa a quem baixa pela página, não a
        // quem vê "o que mudou" antes de instalar — o app corta dali para baixo.
        internal const string MarcadorGuiaDeArquivos = "<!-- arquivos-da-release -->";

        internal static string? NotasSemGuiaDeArquivos(string? corpo)
        {
            if (corpo == null)
                return null;

            var inicio = corpo.IndexOf(MarcadorGuiaDeArquivos, StringComparison.Ordinal);
            var notas = (inicio >= 0 ? corpo[..inicio] : corpo).TrimEnd();
            return notas.Length > 0 ? notas : null;
        }

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

        // pkexec sem senha (regra do polkit do pacote); o ajudante confere assinatura e
        // versão e instala com o dpkg. Os arquivos do app em execução são trocados por
        // baixo dele, o que no Linux não atrapalha: o processo novo já abre a versão nova.
        private static async Task AtualizarDebAsync(string caminhoDeb)
        {
            var inicio = new ProcessStartInfo("pkexec") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
            inicio.ArgumentList.Add(PacoteDeb.Ajudante);
            inicio.ArgumentList.Add(caminhoDeb);
            using var processo = Process.Start(inicio)!;
            var saida = processo.StandardOutput.ReadToEndAsync();
            var erro = await processo.StandardError.ReadToEndAsync();
            await processo.WaitForExitAsync();
            await saida;
            LimparPastaTemp(caminhoDeb);
            if (processo.ExitCode != 0)
                throw new InvalidOperationException($"atualizar-pacote saiu com {processo.ExitCode}: {erro.Trim()}");

            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, ArgumentoAposAtualizacao) { UseShellExecute = false });
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
