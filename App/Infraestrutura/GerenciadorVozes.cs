using System.Formats.Tar;
using System.Net.Http;
using ICSharpCode.SharpZipLib.BZip2;

namespace CofreDeSenhas
{
    internal sealed record VozNeural(string Idioma, string Nome, string Arquivo, long Bytes);

    // Vozes neurais Piper (via sherpa-onnx), baixadas sob demanda das releases do
    // projeto e guardadas no perfil do app. Uma voz por idioma da interface; os
    // dados do fonemizador (espeak-ng-data) são compartilhados entre elas.
    internal static class GerenciadorVozes
    {
        private const string BaseUrl =
            "https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/";

        public static readonly IReadOnlyDictionary<string, VozNeural> Catalogo =
            new Dictionary<string, VozNeural>(StringComparer.OrdinalIgnoreCase)
            {
                ["pt"] = new("pt", "Faber", "vits-piper-pt_BR-faber-medium", 67_183_065),
                ["en"] = new("en", "Lessac", "vits-piper-en_US-lessac-medium", 67_230_653),
                ["es"] = new("es", "DaveFX", "vits-piper-es_ES-davefx-medium", 67_184_952),
                ["fr"] = new("fr", "Siwis", "vits-piper-fr_FR-siwis-medium", 67_207_459),
                ["de"] = new("de", "Thorsten", "vits-piper-de_DE-thorsten-medium", 67_214_254),
                ["it"] = new("it", "Paola", "vits-piper-it_IT-paola-medium", 67_221_173),
            };

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(10) };

        public static string PastaBase => Path.Combine(CaminhosApp.PastaDados, "vozes");
        public static string PastaEspeak => Path.Combine(PastaBase, "espeak-ng-data");
        public static string PastaDoIdioma(string codigo) => Path.Combine(PastaBase, Curto(codigo));

        public static string CaminhoModelo(string codigo) => Path.Combine(PastaDoIdioma(codigo), "model.onnx");
        public static string CaminhoTokens(string codigo) => Path.Combine(PastaDoIdioma(codigo), "tokens.txt");

        public static string Curto(string codigo) => codigo.Split('-')[0].ToLowerInvariant();

        public static VozNeural? Voz(string codigo) =>
            Catalogo.TryGetValue(Curto(codigo), out var v) ? v : null;

        public static bool Suportado(string codigo) => Catalogo.ContainsKey(Curto(codigo));

        public static bool Instalada(string codigo)
        {
            if (!Suportado(codigo))
                return false;

            return File.Exists(CaminhoModelo(codigo))
                && File.Exists(CaminhoTokens(codigo))
                && Directory.Exists(PastaEspeak)
                && Directory.EnumerateFiles(PastaEspeak).Any();
        }

        public static int TamanhoMb(string codigo) =>
            Voz(codigo) is { } v ? (int)Math.Round(v.Bytes / 1_048_576.0) : 0;

        public static async Task BaixarAsync(string codigo, IProgress<double>? progresso, CancellationToken ct)
        {
            var voz = Voz(codigo) ?? throw new InvalidOperationException("idioma sem voz no catálogo: " + codigo);

            Directory.CreateDirectory(PastaBase);
            var temporario = Path.Combine(PastaBase, voz.Arquivo + ".tar.bz2.tmp");
            var destino = PastaDoIdioma(codigo);
            var destinoTmp = destino + ".tmp";

            try
            {
                using (var resposta = await _http.GetAsync(BaseUrl + voz.Arquivo + ".tar.bz2",
                           HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    resposta.EnsureSuccessStatusCode();
                    var total = resposta.Content.Headers.ContentLength ?? voz.Bytes;

                    await using var origem = await resposta.Content.ReadAsStreamAsync(ct);
                    await using var arquivo = File.Create(temporario);

                    var buffer = new byte[81920];
                    long lidos = 0;
                    int n;
                    while ((n = await origem.ReadAsync(buffer, ct)) > 0)
                    {
                        await arquivo.WriteAsync(buffer.AsMemory(0, n), ct);
                        lidos += n;
                        progresso?.Report(Math.Min(0.98, lidos / (double)total * 0.98));
                    }
                }

                if (Directory.Exists(destinoTmp))
                    Directory.Delete(destinoTmp, true);
                Directory.CreateDirectory(destinoTmp);
                Directory.CreateDirectory(PastaEspeak);

                await Task.Run(() => Extrair(temporario, destinoTmp), ct);

                if (Directory.Exists(destino))
                    Directory.Delete(destino, true);
                Directory.Move(destinoTmp, destino);
                progresso?.Report(1.0);
            }
            finally
            {
                TentarApagar(temporario);
                if (Directory.Exists(destinoTmp))
                    try { Directory.Delete(destinoTmp, true); } catch { }
            }
        }

        private static void Extrair(string tarBz2, string destino)
        {
            using var arquivo = File.OpenRead(tarBz2);
            using var bzip2 = new BZip2InputStream(arquivo);
            using var tar = new TarReader(bzip2);

            while (tar.GetNextEntry() is { } entrada)
            {
                if (entrada.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                    || string.IsNullOrEmpty(entrada.Name))
                    continue;

                var partes = entrada.Name.Replace('\\', '/').Split('/');
                if (partes.Length < 2)
                    continue;

                var relativo = string.Join('/', partes.Skip(1));
                string alvo;

                if (relativo.StartsWith("espeak-ng-data/", StringComparison.Ordinal))
                {
                    alvo = Path.Combine(PastaEspeak, relativo.Substring("espeak-ng-data/".Length));
                    if (File.Exists(alvo))
                        continue;
                }
                else if (relativo.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
                {
                    alvo = Path.Combine(destino, "model.onnx");
                }
                else if (relativo.Equals("tokens.txt", StringComparison.OrdinalIgnoreCase))
                {
                    alvo = Path.Combine(destino, "tokens.txt");
                }
                else if (relativo.EndsWith(".onnx.json", StringComparison.OrdinalIgnoreCase))
                {
                    alvo = Path.Combine(destino, "config.json");
                }
                else
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(alvo)!);
                entrada.ExtractToFile(alvo, overwrite: true);
            }
        }

        private static void TentarApagar(string caminho)
        {
            try { if (File.Exists(caminho)) File.Delete(caminho); } catch { }
        }
    }
}
