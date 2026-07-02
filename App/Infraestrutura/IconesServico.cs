using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace CofreDeSenhas
{
    internal readonly record struct IconeServico(string Texto, Color Fundo, Color Frente, string? Dominio);

    internal static class IconesServico
    {
        private sealed record Entrada(string[] Aliases, string Texto, uint Fundo,
            uint Frente = 0xFFFFFFFF, string? Dominio = null);

        private static readonly Entrada[] Banco =
        {
            new(new[] { "gmail", "google mail" }, "G", 0xFFEA4335, Dominio: "gmail.com"),
            new(new[] { "google", "google account" }, "G", 0xFF4285F4, Dominio: "google.com"),
            new(new[] { "google drive", "drive" }, "GD", 0xFF0F9D58, Dominio: "drive.google.com"),
            new(new[] { "google photos", "photos" }, "GP", 0xFF4285F4, Dominio: "photos.google.com"),
            new(new[] { "google cloud", "gcp" }, "GC", 0xFF4285F4, Dominio: "cloud.google.com"),
            new(new[] { "google ads", "adsense", "adwords" }, "AD", 0xFF4285F4, Dominio: "ads.google.com"),
            new(new[] { "youtube" }, "YT", 0xFFFF0000, Dominio: "youtube.com"),
            new(new[] { "yahoo", "yahoo mail" }, "Y", 0xFF6001D2, Dominio: "yahoo.com"),
            new(new[] { "proton", "proton mail", "protonmail" }, "PM", 0xFF6D4AFF, Dominio: "proton.me"),
            new(new[] { "zoho", "zoho mail" }, "ZO", 0xFFE42527, Dominio: "zoho.com"),
            new(new[] { "icloud mail" }, "IC", 0xFF3B82F6, Dominio: "icloud.com"),
            new(new[] { "facebook", "fb" }, "f", 0xFF1877F2, Dominio: "facebook.com"),
            new(new[] { "meta" }, "ME", 0xFF0668E1, Dominio: "meta.com"),
            new(new[] { "instagram" }, "IG", 0xFFE1306C, Dominio: "instagram.com"),
            new(new[] { "whatsapp", "zap" }, "WA", 0xFF25D366, Dominio: "whatsapp.com"),
            new(new[] { "twitter", "x", "x com" }, "X", 0xFF111111, Dominio: "x.com"),
            new(new[] { "linkedin" }, "in", 0xFF0A66C2, Dominio: "linkedin.com"),
            new(new[] { "tiktok" }, "TT", 0xFF111111, Dominio: "tiktok.com"),
            new(new[] { "discord" }, "DI", 0xFF5865F2, Dominio: "discord.com"),
            new(new[] { "telegram" }, "TG", 0xFF26A5E4, Dominio: "telegram.org"),
            new(new[] { "reddit" }, "RD", 0xFFFF4500, Dominio: "reddit.com"),
            new(new[] { "pinterest" }, "PI", 0xFFE60023, Dominio: "pinterest.com"),
            new(new[] { "snapchat" }, "SC", 0xFFFFFC00, Frente: 0xFF111827, Dominio: "snapchat.com"),
            new(new[] { "threads" }, "TH", 0xFF111111, Dominio: "threads.net"),
            new(new[] { "mastodon" }, "MS", 0xFF6364FF, Dominio: "mastodon.social"),
            new(new[] { "netflix" }, "N", 0xFFE50914, Dominio: "netflix.com"),
            new(new[] { "prime video", "amazon prime" }, "PV", 0xFF00A8E1, Dominio: "primevideo.com"),
            new(new[] { "disney", "disney plus", "disney+" }, "D+", 0xFF113CCF, Dominio: "disneyplus.com"),
            new(new[] { "hbo", "hbo max", "max" }, "MX", 0xFF5B21B6, Dominio: "max.com"),
            new(new[] { "globoplay", "globo play" }, "GP", 0xFFF97316, Dominio: "globoplay.globo.com"),
            new(new[] { "crunchyroll" }, "CR", 0xFFF47521, Dominio: "crunchyroll.com"),
            new(new[] { "paramount", "paramount plus", "paramount+" }, "P+", 0xFF0064FF, Dominio: "paramountplus.com"),
            new(new[] { "star plus", "star+" }, "S+", 0xFF4F46E5, Dominio: "starplus.com"),
            new(new[] { "deezer" }, "DZ", 0xFFA238FF, Dominio: "deezer.com"),
            new(new[] { "soundcloud" }, "SC", 0xFFFF5500, Dominio: "soundcloud.com"),
            new(new[] { "twitch" }, "TW", 0xFF9146FF, Dominio: "twitch.tv"),
            new(new[] { "amazon" }, "A", 0xFFFF9900, Frente: 0xFF111827, Dominio: "amazon.com"),
            new(new[] { "aws", "amazon web services" }, "AWS", 0xFFFF9900, Frente: 0xFF111827, Dominio: "aws.amazon.com"),
            new(new[] { "spotify" }, "SP", 0xFF1DB954, Frente: 0xFF111827, Dominio: "spotify.com"),
            new(new[] { "steam" }, "ST", 0xFF171A21, Dominio: "steampowered.com"),
            new(new[] { "epic games", "epic" }, "E", 0xFF2563EB, Dominio: "epicgames.com"),
            new(new[] { "gog" }, "GOG", 0xFFDC2626, Dominio: "gog.com"),
            new(new[] { "battle net", "battlenet", "bnet", "blizzard" }, "B", 0xFF00AEFF, Dominio: "battle.net"),
            new(new[] { "ea", "electronic arts", "origin" }, "EA", 0xFFFF4747, Dominio: "ea.com"),
            new(new[] { "ubisoft", "uplay" }, "UB", 0xFF111827, Dominio: "ubisoft.com"),
            new(new[] { "rockstar", "rockstar games" }, "R", 0xFFFCAF17, Frente: 0xFF111827, Dominio: "rockstargames.com"),
            new(new[] { "playstation", "psn" }, "PS", 0xFF003791, Dominio: "playstation.com"),
            new(new[] { "xbox" }, "XB", 0xFF107C10, Dominio: "xbox.com"),
            new(new[] { "nintendo" }, "N", 0xFFE60012, Dominio: "nintendo.com"),
            new(new[] { "minecraft" }, "MC", 0xFF62B447, Dominio: "minecraft.net"),
            new(new[] { "roblox" }, "RB", 0xFF111827, Dominio: "roblox.com"),
            new(new[] { "fortnite" }, "FN", 0xFF4F46E5, Dominio: "fortnite.com"),
            new(new[] { "bethesda" }, "BE", 0xFF111827, Dominio: "bethesda.net"),
            new(new[] { "warframe" }, "WA", 0xFF0891B2, Dominio: "warframe.com"),
            new(new[] { "riot", "valorant", "league of legends" }, "RI", 0xFFD32936, Dominio: "riotgames.com"),
            new(new[] { "tarkov", "escape from tarkov" }, "T", 0xFF4B5563, Dominio: "escapefromtarkov.com"),
            new(new[] { "dauntless" }, "D", 0xFF2563EB, Dominio: "playdauntless.com"),
            new(new[] { "modern warfare" }, "MW", 0xFF0891B2, Dominio: "callofduty.com"),
            new(new[] { "call of duty", "cod" }, "CD", 0xFF111827, Dominio: "callofduty.com"),
            new(new[] { "unity" }, "UN", 0xFF111827, Dominio: "unity.com"),
            new(new[] { "unreal", "unreal engine" }, "UE", 0xFF111827, Dominio: "unrealengine.com"),
            new(new[] { "itch", "itch io" }, "IT", 0xFFFA5C5C, Dominio: "itch.io"),
            new(new[] { "github" }, "GH", 0xFF24292F, Dominio: "github.com"),
            new(new[] { "gitlab" }, "GL", 0xFFFC6D26, Dominio: "gitlab.com"),
            new(new[] { "bitbucket" }, "BB", 0xFF0052CC, Dominio: "bitbucket.org"),
            new(new[] { "stackoverflow", "stack overflow" }, "SO", 0xFFF48024, Dominio: "stackoverflow.com"),
            new(new[] { "docker", "docker hub" }, "DK", 0xFF2496ED, Dominio: "docker.com"),
            new(new[] { "kubernetes", "k8s" }, "K8", 0xFF326CE5, Dominio: "kubernetes.io"),
            new(new[] { "vercel" }, "VC", 0xFF111111, Dominio: "vercel.com"),
            new(new[] { "netlify" }, "NL", 0xFF00C7B7, Frente: 0xFF111827, Dominio: "netlify.com"),
            new(new[] { "heroku" }, "HK", 0xFF430098, Dominio: "heroku.com"),
            new(new[] { "digitalocean", "digital ocean" }, "DO", 0xFF0080FF, Dominio: "digitalocean.com"),
            new(new[] { "cloudflare" }, "CF", 0xFFF38020, Frente: 0xFF111827, Dominio: "cloudflare.com"),
            new(new[] { "firebase" }, "FB", 0xFFFFCA28, Frente: 0xFF111827, Dominio: "firebase.google.com"),
            new(new[] { "supabase" }, "SB", 0xFF3ECF8E, Frente: 0xFF111827, Dominio: "supabase.com"),
            new(new[] { "postman" }, "PM", 0xFFFF6C37, Dominio: "postman.com"),
            new(new[] { "npm" }, "NPM", 0xFFCB3837, Dominio: "npmjs.com"),
            new(new[] { "pypi", "python package index" }, "PY", 0xFF3775A9, Dominio: "pypi.org"),
            new(new[] { "nuget" }, "NG", 0xFF004880, Dominio: "nuget.org"),
            new(new[] { "jetbrains" }, "JB", 0xFF111111, Dominio: "jetbrains.com"),
            new(new[] { "visual studio", "visual studio code", "vscode", "vs code" }, "VS", 0xFF007ACC, Dominio: "code.visualstudio.com"),
            new(new[] { "microsoft", "outlook", "hotmail", "live" }, "O", 0xFF0078D4, Dominio: "outlook.com"),
            new(new[] { "office", "office 365", "microsoft 365" }, "365", 0xFFD83B01, Dominio: "microsoft365.com"),
            new(new[] { "teams", "microsoft teams" }, "TM", 0xFF6264A7, Dominio: "teams.microsoft.com"),
            new(new[] { "azure", "microsoft azure" }, "AZ", 0xFF0078D4, Dominio: "azure.microsoft.com"),
            new(new[] { "apple id", "appleid" }, "A", 0xFF6B7280, Dominio: "appleid.apple.com"),
            new(new[] { "apple", "icloud" }, "A", 0xFF6B7280, Dominio: "apple.com"),
            new(new[] { "dropbox" }, "DB", 0xFF0061FF, Dominio: "dropbox.com"),
            new(new[] { "onedrive" }, "OD", 0xFF0364B8, Dominio: "onedrive.live.com"),
            new(new[] { "notion" }, "NO", 0xFF111111, Dominio: "notion.so"),
            new(new[] { "slack" }, "SL", 0xFF4A154B, Dominio: "slack.com"),
            new(new[] { "trello" }, "TR", 0xFF0079BF, Dominio: "trello.com"),
            new(new[] { "jira", "atlassian" }, "JR", 0xFF0052CC, Dominio: "atlassian.com"),
            new(new[] { "figma" }, "FG", 0xFFF24E1E, Dominio: "figma.com"),
            new(new[] { "canva" }, "CV", 0xFF00C4CC, Frente: 0xFF111827, Dominio: "canva.com"),
            new(new[] { "zoom" }, "ZM", 0xFF0B5CFF, Dominio: "zoom.us"),
            new(new[] { "skype" }, "SK", 0xFF00AFF0, Dominio: "skype.com"),
            new(new[] { "openai", "chatgpt" }, "AI", 0xFF111827, Dominio: "openai.com"),
            new(new[] { "paypal" }, "PP", 0xFF003087, Dominio: "paypal.com"),
            new(new[] { "wise", "transferwise" }, "WI", 0xFF9FE870, Frente: 0xFF111827, Dominio: "wise.com"),
            new(new[] { "binance" }, "BN", 0xFFF0B90B, Frente: 0xFF111827, Dominio: "binance.com"),
            new(new[] { "coinbase" }, "CB", 0xFF0052FF, Dominio: "coinbase.com"),
            new(new[] { "stripe" }, "ST", 0xFF635BFF, Dominio: "stripe.com"),
            new(new[] { "mercado livre", "mercadolivre" }, "ML", 0xFFFFE600, Frente: 0xFF111827, Dominio: "mercadolivre.com.br"),
            new(new[] { "mercado pago", "mercadopago" }, "MP", 0xFF00A650, Dominio: "mercadopago.com.br"),
            new(new[] { "nubank", "nu bank" }, "NU", 0xFF820AD1, Dominio: "nubank.com.br"),
            new(new[] { "itau", "itaucard" }, "IT", 0xFFFF6B00, Dominio: "itau.com.br"),
            new(new[] { "bradesco" }, "BR", 0xFFCC092F, Dominio: "bradesco.com.br"),
            new(new[] { "santander" }, "SA", 0xFFE40000, Dominio: "santander.com.br"),
            new(new[] { "caixa" }, "CX", 0xFF0066B3, Dominio: "caixa.gov.br"),
            new(new[] { "banco do brasil", "bb" }, "BB", 0xFFFFD100, Frente: 0xFF173B8F, Dominio: "bb.com.br"),
            new(new[] { "inter" }, "IN", 0xFFFF7A00, Dominio: "bancointer.com.br"),
            new(new[] { "picpay" }, "PI", 0xFF21C25E, Dominio: "picpay.com"),
            new(new[] { "c6", "c6 bank" }, "C6", 0xFF111111, Dominio: "c6bank.com.br"),
            new(new[] { "next" }, "NX", 0xFF00AEEF, Dominio: "next.me"),
            new(new[] { "neon" }, "NE", 0xFF00AEEF, Dominio: "neon.com.br"),
            new(new[] { "pagbank", "pagseguro" }, "PB", 0xFFFFCC00, Frente: 0xFF111827, Dominio: "pagseguro.uol.com.br"),
            new(new[] { "stone" }, "ST", 0xFF00A868, Dominio: "stone.com.br"),
            new(new[] { "serasa" }, "SE", 0xFFE63888, Dominio: "serasa.com.br"),
            new(new[] { "gov br", "gov.br", "govbr" }, "GB", 0xFF1351B4, Dominio: "gov.br"),
            new(new[] { "receita federal" }, "RF", 0xFF1351B4, Dominio: "gov.br"),
            new(new[] { "claro", "claro net" }, "C", 0xFFE3262E, Dominio: "claro.com.br"),
            new(new[] { "vivo" }, "VI", 0xFF660099, Dominio: "vivo.com.br"),
            new(new[] { "tim" }, "TM", 0xFF004691, Dominio: "tim.com.br"),
            new(new[] { "oi" }, "OI", 0xFFF59E0B, Frente: 0xFF111827, Dominio: "oi.com.br"),
            new(new[] { "submarino" }, "S", 0xFF0EA5E9, Dominio: "submarino.com.br"),
            new(new[] { "americanas" }, "AM", 0xFFE30613, Dominio: "americanas.com.br"),
            new(new[] { "magalu", "magazine luiza" }, "MG", 0xFF0086FF, Dominio: "magazineluiza.com.br"),
            new(new[] { "shopee" }, "SH", 0xFFEE4D2D, Dominio: "shopee.com.br"),
            new(new[] { "aliexpress", "ali express" }, "AE", 0xFFFF4747, Dominio: "aliexpress.com"),
            new(new[] { "shein" }, "SH", 0xFF111111, Dominio: "shein.com"),
            new(new[] { "ebay" }, "EB", 0xFF0064D2, Dominio: "ebay.com"),
            new(new[] { "kabum", "ka bu m" }, "KB", 0xFFFF6500, Dominio: "kabum.com.br"),
            new(new[] { "terabyte" }, "TB", 0xFF008FD2, Dominio: "terabyteshop.com.br"),
            new(new[] { "pichau" }, "PC", 0xFFDC2626, Dominio: "pichau.com.br"),
            new(new[] { "ifood", "i food" }, "IF", 0xFFEA1D2C, Dominio: "ifood.com.br"),
            new(new[] { "uber" }, "UB", 0xFF111111, Dominio: "uber.com"),
            new(new[] { "99", "99 app", "99 taxi" }, "99", 0xFFFFD000, Frente: 0xFF111827, Dominio: "99app.com"),
            new(new[] { "rappi" }, "RA", 0xFFFF5A5F, Dominio: "rappi.com.br"),
            new(new[] { "airbnb" }, "AB", 0xFFFF5A5F, Dominio: "airbnb.com"),
            new(new[] { "booking", "booking com" }, "BO", 0xFF003B95, Dominio: "booking.com"),
            new(new[] { "latam" }, "LA", 0xFF1B0088, Dominio: "latamairlines.com"),
            new(new[] { "azul" }, "AZ", 0xFF0066B3, Dominio: "voeazul.com.br"),
            new(new[] { "gol" }, "GO", 0xFFFF6B00, Dominio: "voegol.com.br"),
            new(new[] { "udemy" }, "UD", 0xFFA435F0, Dominio: "udemy.com"),
            new(new[] { "coursera" }, "CO", 0xFF0056D2, Dominio: "coursera.org"),
            new(new[] { "alura" }, "AL", 0xFF0B182C, Dominio: "alura.com.br"),
            new(new[] { "duolingo" }, "DU", 0xFF58CC02, Frente: 0xFF111827, Dominio: "duolingo.com"),
            new(new[] { "khan academy" }, "KA", 0xFF14BF96, Dominio: "khanacademy.org"),
            new(new[] { "mysql" }, "MY", 0xFF00758F, Dominio: "mysql.com"),
            new(new[] { "mariadb", "maria db" }, "MA", 0xFF003545, Dominio: "mariadb.org"),
            new(new[] { "postgres", "postgresql" }, "PG", 0xFF336791, Dominio: "postgresql.org"),
            new(new[] { "sqlite" }, "SQ", 0xFF003B57, Dominio: "sqlite.org"),
            new(new[] { "sql server", "mssql" }, "MS", 0xFFA91D22, Dominio: "microsoft.com"),
            new(new[] { "oracle" }, "OR", 0xFFF80000, Dominio: "oracle.com"),
            new(new[] { "mongodb", "mongo db" }, "MO", 0xFF47A248, Dominio: "mongodb.com"),
            new(new[] { "redis" }, "RE", 0xFFDC382D, Dominio: "redis.io"),
            new(new[] { "sticky password" }, "SP", 0xFFEAB308, Frente: 0xFF111827, Dominio: "stickypassword.com"),
            new(new[] { "1password", "one password" }, "1P", 0xFF0A84FF, Dominio: "1password.com"),
            new(new[] { "bitwarden" }, "BW", 0xFF175DDC, Dominio: "bitwarden.com"),
            new(new[] { "lastpass" }, "LP", 0xFFD32D27, Dominio: "lastpass.com"),
            new(new[] { "dashlane" }, "DL", 0xFF0E353D, Dominio: "dashlane.com"),
            new(new[] { "nordpass" }, "NP", 0xFF3E5FFF, Dominio: "nordpass.com"),
            new(new[] { "keeper" }, "KP", 0xFFFFB600, Frente: 0xFF111827, Dominio: "keepersecurity.com"),
            new(new[] { "avast" }, "AV", 0xFFFF7800, Dominio: "avast.com"),
            new(new[] { "avg" }, "AVG", 0xFF008A00, Dominio: "avg.com"),
            new(new[] { "kaspersky" }, "KA", 0xFF00A88E, Dominio: "kaspersky.com"),
            new(new[] { "norton" }, "NO", 0xFFFFD100, Frente: 0xFF111827, Dominio: "norton.com"),
            new(new[] { "malwarebytes" }, "MW", 0xFF0047FF, Dominio: "malwarebytes.com"),
        };

        private static readonly TimeSpan TempoLimiteRequisicao = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan EsperaAposFalha = TimeSpan.FromMinutes(5);
        private const int MaxBytesIcone = 512 * 1024;
        private const int MaxDominiosCache = 512;

        private static readonly string[] SufixosNaoPublicos =
        {
            ".local", ".localhost", ".internal", ".intranet",
            ".lan", ".home", ".corp", ".test", ".example", ".invalid",
        };

        private static readonly HttpClient ClienteHttp = new()
        {
            Timeout = TempoLimiteRequisicao
        };

        private static readonly ConcurrentDictionary<string, Bitmap> IconesProntos =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, Task<Bitmap?>> DownloadsEmAndamento =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, DateTime> FalhasRecentes =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Color[] PaletaFallback =
        {
            Color.FromUInt32(0xFF7C3AED),
            Color.FromUInt32(0xFF2563EB),
            Color.FromUInt32(0xFF16A34A),
            Color.FromUInt32(0xFFEA580C),
            Color.FromUInt32(0xFFDB2777),
            Color.FromUInt32(0xFF0891B2),
            Color.FromUInt32(0xFFCA8A04),
            Color.FromUInt32(0xFFDC2626),
        };

        public static IconeServico Obter(string? servico, string? url = null)
        {
            string normalizado = Normalizar(servico);
            var tokens = normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            foreach (var entrada in Banco)
            {
                if (entrada.Aliases.Any(alias => Combina(normalizado, tokens, alias)))
                {
                    return new IconeServico(
                        entrada.Texto,
                        Color.FromUInt32(entrada.Fundo),
                        Color.FromUInt32(entrada.Frente),
                        entrada.Dominio);
                }
            }

            return new IconeServico(
                Iniciais(servico),
                CorFallback(servico),
                Color.FromUInt32(0xFFFFFFFF),
                ExtrairDominio(url) ?? ExtrairDominio(servico));
        }

        public static async Task<Bitmap?> ObterBitmapAsync(IconeServico icone)
        {
            string? dominio = NormalizarDominio(icone.Dominio);
            if (dominio == null)
                return null;

            if (IconesProntos.TryGetValue(dominio, out var pronto))
                return pronto;

            if (FalhasRecentes.TryGetValue(dominio, out var quando))
            {
                if (DateTime.UtcNow - quando < EsperaAposFalha)
                    return null;
                FalhasRecentes.TryRemove(dominio, out _);
            }

            var download = DownloadsEmAndamento.GetOrAdd(dominio, BaixarBitmapAsync);
            try
            {
                var bitmap = await download.ConfigureAwait(false);
                if (bitmap != null)
                {
                    if (IconesProntos.Count < MaxDominiosCache)
                        IconesProntos.TryAdd(dominio, bitmap);
                }
                else
                {
                    FalhasRecentes[dominio] = DateTime.UtcNow;
                }
                return bitmap;
            }
            finally
            {
                DownloadsEmAndamento.TryRemove(
                    new KeyValuePair<string, Task<Bitmap?>>(dominio, download));
            }
        }

        private static async Task<Bitmap?> BaixarBitmapAsync(string dominio)
        {
            try
            {
                string url = "https://www.google.com/s2/favicons?domain=" +
                    Uri.EscapeDataString(dominio) + "&sz=64";

                using var cts = new CancellationTokenSource(TempoLimiteRequisicao);
                using var resposta = await ClienteHttp.GetAsync(
                    url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                if (!resposta.IsSuccessStatusCode)
                    return null;

                string? tipo = resposta.Content.Headers.ContentType?.MediaType;
                if (tipo != null && !tipo.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (resposta.Content.Headers.ContentLength is long tamanho && tamanho > MaxBytesIcone)
                    return null;

                await using var stream = await resposta.Content.ReadAsStreamAsync(cts.Token);
                using var memoria = new MemoryStream();
                if (!await CopiarLimitadoAsync(stream, memoria, MaxBytesIcone, cts.Token))
                    return null;

                if (memoria.Length == 0)
                    return null;

                memoria.Position = 0;
                return new Bitmap(memoria);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<bool> CopiarLimitadoAsync(
            Stream origem, Stream destino, int limite, CancellationToken ct)
        {
            var buffer = new byte[8192];
            int total = 0;
            int lidos;
            while ((lidos = await origem.ReadAsync(buffer, ct)) > 0)
            {
                total += lidos;
                if (total > limite)
                    return false;

                await destino.WriteAsync(buffer.AsMemory(0, lidos), ct);
            }
            return true;
        }

        public static Color CorFallback(string? texto)
        {
            int hash = 0;
            foreach (char c in texto ?? "")
                hash = hash * 31 + c;

            return PaletaFallback[(int)((uint)hash % (uint)PaletaFallback.Length)];
        }

        private static bool Combina(string normalizado, HashSet<string> tokens, string alias)
        {
            string chave = Normalizar(alias);
            return chave.Contains(' ')
                ? normalizado.Contains(chave, StringComparison.Ordinal)
                : tokens.Contains(chave);
        }

        private static string? ExtrairDominio(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            foreach (var candidato in CandidatosDominio(texto))
            {
                string? dominio = HostDeCandidato(candidato);
                if (dominio != null)
                    return dominio;
            }

            return null;
        }

        private static string? HostDeCandidato(string candidato)
        {
            if (Uri.TryCreate(candidato, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                !string.IsNullOrWhiteSpace(uri.Host))
            {
                string limpo = LimparHost(uri.Host);
                if (EhDominioPublico(limpo))
                    return limpo;
            }

            if (candidato.Contains('.') &&
                Uri.TryCreate("https://" + candidato, UriKind.Absolute, out uri) &&
                !string.IsNullOrWhiteSpace(uri.Host))
            {
                string limpo = LimparHost(uri.Host);
                if (EhDominioPublico(limpo))
                    return limpo;
            }

            return null;
        }

        private static string? NormalizarDominio(string? dominio)
        {
            if (string.IsNullOrWhiteSpace(dominio))
                return null;

            string limpo = LimparHost(dominio);
            return EhDominioPublico(limpo) ? limpo : null;
        }

        private static bool EhDominioPublico(string dominio)
        {
            if (dominio.Length is 0 or > 253)
                return false;

            if (!dominio.Contains('.'))
                return false;

            if (Uri.CheckHostName(dominio) != UriHostNameType.Dns)
                return false;

            foreach (var sufixo in SufixosNaoPublicos)
            {
                if (dominio.EndsWith(sufixo, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static IEnumerable<string> CandidatosDominio(string texto)
        {
            yield return texto.Trim();

            foreach (var parte in texto.Split(new[] { ' ', '\t', '\r', '\n', ',', ';' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                yield return parte.Trim('(', ')', '[', ']', '{', '}', '<', '>', '"', '\'');
            }
        }

        private static string LimparHost(string host)
        {
            string normalizado = host.Trim().TrimEnd('.').ToLowerInvariant();
            return normalizado.StartsWith("www.", StringComparison.Ordinal)
                ? normalizado[4..]
                : normalizado;
        }

        private static string Iniciais(string? texto)
        {
            var partes = (texto ?? "")
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length >= 2)
                return (partes[0][0].ToString() + partes[1][0]).ToUpperInvariant();

            if (partes.Length == 1 && partes[0].Length >= 2)
                return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();

            if (partes.Length == 1)
                return partes[0][0].ToString().ToUpperInvariant();

            return "?";
        }

        private static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "";

            string decomposed = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }

            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
