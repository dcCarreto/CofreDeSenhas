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
            new(new[] { "youtube" }, "YT", 0xFFFF0000, Dominio: "youtube.com"),
            new(new[] { "facebook", "fb" }, "f", 0xFF1877F2, Dominio: "facebook.com"),
            new(new[] { "instagram" }, "IG", 0xFFE1306C, Dominio: "instagram.com"),
            new(new[] { "whatsapp", "zap" }, "WA", 0xFF25D366, Dominio: "whatsapp.com"),
            new(new[] { "twitter", "x", "x com" }, "X", 0xFF111111, Dominio: "x.com"),
            new(new[] { "linkedin" }, "in", 0xFF0A66C2, Dominio: "linkedin.com"),
            new(new[] { "tiktok" }, "TT", 0xFF111111, Dominio: "tiktok.com"),
            new(new[] { "discord" }, "DI", 0xFF5865F2, Dominio: "discord.com"),
            new(new[] { "telegram" }, "TG", 0xFF26A5E4, Dominio: "telegram.org"),
            new(new[] { "reddit" }, "RD", 0xFFFF4500, Dominio: "reddit.com"),
            new(new[] { "netflix" }, "N", 0xFFE50914, Dominio: "netflix.com"),
            new(new[] { "prime video", "amazon prime" }, "PV", 0xFF00A8E1, Dominio: "primevideo.com"),
            new(new[] { "amazon", "aws" }, "A", 0xFFFF9900, Frente: 0xFF111827, Dominio: "amazon.com"),
            new(new[] { "spotify" }, "SP", 0xFF1DB954, Frente: 0xFF111827, Dominio: "spotify.com"),
            new(new[] { "steam" }, "ST", 0xFF171A21, Dominio: "steampowered.com"),
            new(new[] { "epic games", "epic" }, "E", 0xFF2563EB, Dominio: "epicgames.com"),
            new(new[] { "gog" }, "GOG", 0xFFDC2626, Dominio: "gog.com"),
            new(new[] { "battle net", "battlenet", "bnet", "blizzard" }, "B", 0xFF00AEFF, Dominio: "battle.net"),
            new(new[] { "playstation", "psn" }, "PS", 0xFF003791, Dominio: "playstation.com"),
            new(new[] { "xbox" }, "XB", 0xFF107C10, Dominio: "xbox.com"),
            new(new[] { "nintendo" }, "N", 0xFFE60012, Dominio: "nintendo.com"),
            new(new[] { "riot", "valorant", "league of legends" }, "RI", 0xFFD32936, Dominio: "riotgames.com"),
            new(new[] { "tarkov", "escape from tarkov" }, "T", 0xFF4B5563, Dominio: "escapefromtarkov.com"),
            new(new[] { "dauntless" }, "D", 0xFF2563EB, Dominio: "playdauntless.com"),
            new(new[] { "modern warfare" }, "MW", 0xFF0891B2, Dominio: "callofduty.com"),
            new(new[] { "call of duty", "cod" }, "CD", 0xFF111827, Dominio: "callofduty.com"),
            new(new[] { "github" }, "GH", 0xFF24292F, Dominio: "github.com"),
            new(new[] { "gitlab" }, "GL", 0xFFFC6D26, Dominio: "gitlab.com"),
            new(new[] { "bitbucket" }, "BB", 0xFF0052CC, Dominio: "bitbucket.org"),
            new(new[] { "microsoft", "outlook", "hotmail", "live" }, "O", 0xFF0078D4, Dominio: "outlook.com"),
            new(new[] { "office", "office 365", "microsoft 365" }, "365", 0xFFD83B01, Dominio: "microsoft365.com"),
            new(new[] { "apple", "icloud" }, "A", 0xFF6B7280, Dominio: "icloud.com"),
            new(new[] { "dropbox" }, "DB", 0xFF0061FF, Dominio: "dropbox.com"),
            new(new[] { "onedrive" }, "OD", 0xFF0364B8, Dominio: "onedrive.live.com"),
            new(new[] { "paypal" }, "PP", 0xFF003087, Dominio: "paypal.com"),
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
            new(new[] { "claro", "claro net" }, "C", 0xFFE3262E, Dominio: "claro.com.br"),
            new(new[] { "vivo" }, "VI", 0xFF660099, Dominio: "vivo.com.br"),
            new(new[] { "tim" }, "TM", 0xFF004691, Dominio: "tim.com.br"),
            new(new[] { "oi" }, "OI", 0xFFF59E0B, Frente: 0xFF111827, Dominio: "oi.com.br"),
            new(new[] { "submarino" }, "S", 0xFF0EA5E9, Dominio: "submarino.com.br"),
            new(new[] { "americanas" }, "AM", 0xFFE30613, Dominio: "americanas.com.br"),
            new(new[] { "magalu", "magazine luiza" }, "MG", 0xFF0086FF, Dominio: "magazineluiza.com.br"),
            new(new[] { "sticky password" }, "SP", 0xFFEAB308, Frente: 0xFF111827, Dominio: "stickypassword.com"),
            new(new[] { "1password", "one password" }, "1P", 0xFF0A84FF, Dominio: "1password.com"),
            new(new[] { "bitwarden" }, "BW", 0xFF175DDC, Dominio: "bitwarden.com"),
            new(new[] { "lastpass" }, "LP", 0xFFD32D27, Dominio: "lastpass.com"),
            new(new[] { "dashlane" }, "DL", 0xFF0E353D, Dominio: "dashlane.com"),
        };

        private static readonly HttpClient ClienteHttp = new()
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        private static readonly ConcurrentDictionary<string, Task<Bitmap?>> CacheBitmap =
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

        public static IconeServico Obter(string? servico)
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

            return new IconeServico(Iniciais(servico), CorFallback(servico), Color.FromUInt32(0xFFFFFFFF), null);
        }

        public static Task<Bitmap?> ObterBitmapAsync(IconeServico icone)
        {
            if (string.IsNullOrWhiteSpace(icone.Dominio))
                return Task.FromResult<Bitmap?>(null);

            return CacheBitmap.GetOrAdd(icone.Dominio, BaixarBitmapAsync);
        }

        private static async Task<Bitmap?> BaixarBitmapAsync(string dominio)
        {
            try
            {
                string url = "https://www.google.com/s2/favicons?domain=" +
                    Uri.EscapeDataString(dominio) + "&sz=64";

                using var resposta = await ClienteHttp.GetAsync(url);
                if (!resposta.IsSuccessStatusCode)
                    return null;

                await using var stream = await resposta.Content.ReadAsStreamAsync();
                using var memoria = new MemoryStream();
                await stream.CopyToAsync(memoria);
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
