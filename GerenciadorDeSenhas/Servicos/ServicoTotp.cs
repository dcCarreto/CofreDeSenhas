using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace GerenciadorDeSenhas.Servicos
{
    public readonly record struct CodigoTotp(string Codigo, int SegundosRestantes, int Periodo);

    public class ServicoTotp
    {
        public const int Periodo = 30;
        public const int Digitos = 6;

        private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public bool SegredoValido(string? entrada) => TentarNormalizar(entrada, out _);

        public string NormalizarSegredo(string entrada)
        {
            if (!TentarNormalizar(entrada, out var segredo))
                throw new FormatException("Chave de autenticação em duas etapas inválida.");
            return segredo;
        }

        public CodigoTotp Gerar(string entrada, DateTimeOffset? instante = null)
        {
            var segredo = NormalizarSegredo(entrada);
            var chave = DecodificarBase32(segredo);

            var momento = instante ?? DateTimeOffset.UtcNow;
            long segundos = momento.ToUnixTimeSeconds();
            long contador = segundos / Periodo;

            var buffer = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(buffer, contador);
            var hash = HMACSHA1.HashData(chave, buffer);

            int offset = hash[^1] & 0x0F;
            int binario = ((hash[offset] & 0x7F) << 24)
                        | (hash[offset + 1] << 16)
                        | (hash[offset + 2] << 8)
                        | hash[offset + 3];

            int valor = binario % (int)Math.Pow(10, Digitos);
            string codigo = valor.ToString().PadLeft(Digitos, '0');
            int restantes = Periodo - (int)(segundos % Periodo);

            return new CodigoTotp(codigo, restantes, Periodo);
        }

        private static bool TentarNormalizar(string? entrada, out string segredo)
        {
            segredo = string.Empty;
            if (string.IsNullOrWhiteSpace(entrada))
                return false;

            var texto = entrada.Trim();
            if (texto.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
            {
                var extraido = ExtrairSecretDeUri(texto);
                if (extraido == null)
                    return false;
                texto = extraido;
            }

            var limpo = LimparBase32(texto);
            if (limpo.Length < 8)
                return false;

            foreach (var c in limpo)
                if (Alfabeto.IndexOf(c) < 0)
                    return false;

            segredo = limpo;
            return true;
        }

        private static string? ExtrairSecretDeUri(string uri)
        {
            int inicio = uri.IndexOf('?');
            if (inicio < 0 || inicio == uri.Length - 1)
                return null;

            foreach (var par in uri[(inicio + 1)..].Split('&'))
            {
                var partes = par.Split('=', 2);
                if (partes.Length == 2 && partes[0].Equals("secret", StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(partes[1]);
            }

            return null;
        }

        private static string LimparBase32(string entrada)
        {
            var sb = new StringBuilder(entrada.Length);
            foreach (var c in entrada)
            {
                if (c == '=' || c == '-' || char.IsWhiteSpace(c))
                    continue;
                sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        private static byte[] DecodificarBase32(string base32)
        {
            int bits = 0, valor = 0;
            var saida = new List<byte>(base32.Length * 5 / 8);

            foreach (var c in base32)
            {
                int indice = Alfabeto.IndexOf(c);
                if (indice < 0)
                    throw new FormatException("Caractere inválido na chave de autenticação.");

                valor = (valor << 5) | indice;
                bits += 5;
                if (bits >= 8)
                {
                    bits -= 8;
                    saida.Add((byte)((valor >> bits) & 0xFF));
                }
            }

            return saida.ToArray();
        }
    }
}
