using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CofreDeSenhas.Janelas;
using QRCoder;
using SkiaSharp;

namespace CofreDeSenhas
{
    internal static class QrBackup
    {
        public static async Task OferecerSalvarAsync(Window dono, string senhaMestra)
        {
            var aceitou = await CaixaMensagem.ConfirmarAsync(dono,
                "Deseja salvar um QR code de backup da sua senha mestra?\n\n" +
                "ATENÇÃO: o QR code contém a versão senha-frase da sua senha mestra. " +
                "Qualquer pessoa que escaneie a imagem poderá reconstruir a senha e acessar o cofre.\n\n" +
                "Guarde o arquivo em local seguro e offline (ou impresso) — " +
                "nunca em nuvem, e-mail ou pastas compartilhadas.",
                "Backup da senha mestra (QR code)");

            if (!aceitou)
                return;

            var arquivo = await dono.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Salvar QR code da senha mestra",
                SuggestedFileName = $"senha-mestra-qrcode-{DateTime.Now:yyyy-MM-dd}.png",
                DefaultExtension = "png",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Imagem PNG") { Patterns = new[] { "*.png" } }
                }
            });
            if (arquivo == null)
                return;

            try
            {
                await File.WriteAllBytesAsync(arquivo.Path.LocalPath, GerarFolhaBackupPng(senhaMestra));
                await CaixaMensagem.MostrarAsync(dono,
                    "QR code salvo com sucesso. Guarde-o em local seguro.",
                    "Backup da senha mestra");
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(dono,
                    $"Não foi possível salvar o QR code: {ex.Message}",
                    "Erro", TipoMensagem.Erro);
            }
        }

        public static byte[] GerarFolhaBackupPng(string senha)
        {
            var senhaFrase = ConverterSenhaParaFrase(senha);
            using var gerador = new QRCodeGenerator();
            using var dados = gerador.CreateQrCode(senhaFrase, QRCodeGenerator.ECCLevel.Q);
            var qrPng = new PngByteQRCode(dados).GetGraphic(10);

            using var qr = SKBitmap.Decode(qrPng);

            const int margem = 40;
            const int topo = 110;
            const int rodape = 86;
            int qrW = qr.Width, qrH = qr.Height;
            int largura = Math.Max(qrW + margem * 2, 420);
            int altura = topo + qrH + rodape;

            var roxo = new SKColor(124, 58, 237);
            var escuro = new SKColor(32, 35, 43);
            var cinza = new SKColor(110, 114, 128);

            using var surface = SKSurface.Create(new SKImageInfo(largura, altura));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            int textoX = margem;
            using (var logo = SKBitmap.Decode(Recursos.LogoPng()))
            {
                if (logo != null)
                {
                    using var paintLogo = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    canvas.DrawBitmap(logo, new SKRect(margem, 30, margem + 36, 30 + 36), paintLogo);
                    textoX = margem + 46;
                }
            }

            using var negrito = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
            using var normal = SKTypeface.FromFamilyName(null, SKFontStyle.Normal);

            using (var paint = new SKPaint { Color = escuro, TextSize = 20, IsAntialias = true, Typeface = negrito })
                canvas.DrawText("Cofre de Senhas", textoX, 31 + 20, paint);
            using (var paint = new SKPaint { Color = roxo, TextSize = 14, IsAntialias = true, Typeface = normal })
                canvas.DrawText("Backup da senha mestra", textoX, 56 + 14, paint);

            int qrX = (largura - qrW) / 2;
            canvas.DrawBitmap(qr, new SKRect(qrX, topo, qrX + qrW, topo + qrH));

            using (var paint = new SKPaint { Color = cinza, TextSize = 11.5f, IsAntialias = true, Typeface = normal })
            {
                string[] aviso =
                {
                    "Contém a versão senha-frase da senha mestra.",
                    "Guarde em local seguro e offline."
                };
                float y = topo + qrH + 18 + paint.TextSize;
                foreach (var linha in aviso)
                {
                    float w = paint.MeasureText(linha);
                    canvas.DrawText(linha, (largura - w) / 2, y, paint);
                    y += paint.TextSize + 5;
                }
            }

            using var imagem = surface.Snapshot();
            using var png = imagem.Encode(SKEncodedImageFormat.Png, 100);
            return png.ToArray();
        }

        internal static string ConverterSenhaParaFrase(string senha)
        {
            if (string.IsNullOrEmpty(senha))
                return string.Empty;

            var partes = new List<string>(senha.Length);
            var ocorrencias = new Dictionary<char, int>();

            foreach (var caractere in senha)
            {
                var chave = char.ToLowerInvariant(caractere);
                ocorrencias.TryGetValue(chave, out var ocorrencia);
                ocorrencias[chave] = ocorrencia + 1;

                partes.Add(ConverterCaractereParaPalavra(caractere, ocorrencia));
            }

            return string.Join(" ", partes);
        }

        private static string ConverterCaractereParaPalavra(char caractere, int ocorrencia)
        {
            if (char.IsDigit(caractere))
                return caractere.ToString();

            var minusculo = char.ToLowerInvariant(caractere);
            var palavra = PalavraPorOcorrencia(PalavrasPorLetra, minusculo, ocorrencia)
                ?? PalavraPorOcorrencia(PalavrasPorSimbolo, caractere, ocorrencia)
                ?? $"unicode-{(int)caractere:X4}";

            return char.IsUpper(caractere)
                ? char.ToUpperInvariant(palavra[0]) + palavra[1..]
                : palavra;
        }

        private static string? PalavraPorOcorrencia(
            IReadOnlyDictionary<char, string[]> mapa, char caractere, int ocorrencia)
        {
            return mapa.TryGetValue(caractere, out var palavras)
                ? palavras[ocorrencia % palavras.Length]
                : null;
        }

        private static readonly IReadOnlyDictionary<char, string[]> PalavrasPorLetra =
            new Dictionary<char, string[]>
            {
                ['a'] = new[] { "abelha", "abacate", "amora", "agenda", "arroz" },
                ['b'] = new[] { "barco", "banana", "bolacha", "brilho", "bambu" },
                ['c'] = new[] { "casa", "caderno", "campo", "cesta", "cristal" },
                ['d'] = new[] { "dado", "dedal", "desenho", "diario", "diamante" },
                ['e'] = new[] { "escola", "estrela", "espelho", "envelope", "eclipse" },
                ['f'] = new[] { "faca", "fonte", "flor", "farol", "figura" },
                ['g'] = new[] { "gato", "galho", "gaveta", "globo", "grama" },
                ['h'] = new[] { "hotel", "horta", "harpa", "haste", "historia" },
                ['i'] = new[] { "ilha", "impressora", "inverno", "isca", "ideia" },
                ['j'] = new[] { "janela", "jardim", "jarra", "jornal", "joia" },
                ['k'] = new[] { "kilo", "karma", "kiwi", "kart", "karaoke" },
                ['l'] = new[] { "lateral", "lagoa", "lapis", "leque", "livro" },
                ['m'] = new[] { "mesa", "mala", "manga", "martelo", "moeda" },
                ['n'] = new[] { "navio", "nuvem", "ninho", "novela", "neblina" },
                ['o'] = new[] { "ovelha", "oceano", "oficina", "oliva", "onda" },
                ['p'] = new[] { "pedra", "papel", "praia", "panela", "pincel" },
                ['q'] = new[] { "queijo", "quadro", "quintal", "queda", "quilo" },
                ['r'] = new[] { "raio", "radio", "ramo", "relogio", "rocha" },
                ['s'] = new[] { "sapato", "sacola", "semente", "sino", "sombra" },
                ['t'] = new[] { "torre", "tapete", "teatro", "telha", "tijolo" },
                ['u'] = new[] { "uva", "urso", "universo", "urna", "util" },
                ['v'] = new[] { "vaca", "vela", "vento", "vidro", "violeta" },
                ['w'] = new[] { "web", "wifi", "wafer", "watt", "western" },
                ['x'] = new[] { "xadrez", "xarope", "xicara", "xale", "xerife" },
                ['y'] = new[] { "yoga", "yakisoba", "yate", "yin", "youtube" },
                ['z'] = new[] { "zebra", "zinco", "zangado", "ziper", "zumbido" }
            };

        private static readonly IReadOnlyDictionary<char, string[]> PalavrasPorSimbolo =
            new Dictionary<char, string[]>
            {
                ['!'] = new[] { "exclamacao", "alerta", "surpresa" },
                ['@'] = new[] { "arroba", "email", "at" },
                ['#'] = new[] { "cerquilha", "hashtag", "numero" },
                ['$'] = new[] { "cifrao", "dinheiro", "moeda" },
                ['%'] = new[] { "porcentagem", "percentual", "taxa" },
                ['^'] = new[] { "circunflexo", "acento", "chapeu" },
                ['&'] = new[] { "ecomercial", "conector", "ampersand" },
                ['*'] = new[] { "asterisco", "estrela", "multiplica" },
                ['('] = new[] { "abre-parentese", "parentese-abre", "abre-curva" },
                [')'] = new[] { "fecha-parentese", "parentese-fecha", "fecha-curva" },
                ['_'] = new[] { "sublinhado", "linha-baixa", "underscore" },
                ['+'] = new[] { "mais", "soma", "positivo" },
                ['-'] = new[] { "hifen", "traco", "menos" },
                ['='] = new[] { "igual", "equivale", "resultado" },
                ['['] = new[] { "abre-colchete", "colchete-abre", "abre-reto" },
                [']'] = new[] { "fecha-colchete", "colchete-fecha", "fecha-reto" },
                ['{'] = new[] { "abre-chave", "chave-abre", "abre-bloco" },
                ['}'] = new[] { "fecha-chave", "chave-fecha", "fecha-bloco" },
                ['|'] = new[] { "barra-vertical", "vertical", "pipe" },
                [';'] = new[] { "ponto-e-virgula", "semicolon", "pausa" },
                [':'] = new[] { "dois-pontos", "colon", "separador" },
                [','] = new[] { "virgula", "comma", "pausa-curta" },
                ['.'] = new[] { "ponto", "dot", "final" },
                ['<'] = new[] { "menor", "abre-angular", "inferior" },
                ['>'] = new[] { "maior", "fecha-angular", "superior" },
                ['?'] = new[] { "interrogacao", "pergunta", "duvida" },
                ['/'] = new[] { "barra", "slash", "divisao" },
                ['\\'] = new[] { "barra-invertida", "contrabarra", "backslash" },
                ['\''] = new[] { "apostrofo", "aspa-simples", "quote" },
                ['"'] = new[] { "aspas", "aspa-dupla", "citacao" },
                [' '] = new[] { "espaco", "vazio", "intervalo" }
            };
    }
}
