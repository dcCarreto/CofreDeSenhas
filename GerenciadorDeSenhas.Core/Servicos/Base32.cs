using System.Text;

namespace GerenciadorDeSenhas.Servicos
{
    // Base32 no alfabeto Crockford (sem I, L, O, U — reduz erro de transcrição).
    // Usado só para a chave de recuperação, que a pessoa lê de um papel e digita.
    internal static class Base32
    {
        private const string Alfabeto = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        public static string Codificar(ReadOnlySpan<byte> dados)
        {
            var sb = new StringBuilder((dados.Length * 8 + 4) / 5);
            int buffer = 0, bits = 0;
            foreach (var b in dados)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    bits -= 5;
                    sb.Append(Alfabeto[(buffer >> bits) & 0x1F]);
                }
            }
            if (bits > 0)
                sb.Append(Alfabeto[(buffer << (5 - bits)) & 0x1F]);
            return sb.ToString();
        }

        // Aceita minúsculas, e mapeia os enganos clássicos: I/L -> 1, O -> 0.
        // Devolve null se aparecer um caractere que não é do alfabeto.
        public static byte[]? Decodificar(string texto)
        {
            var saida = new List<byte>(texto.Length * 5 / 8 + 1);
            int buffer = 0, bits = 0;
            foreach (var bruto in texto)
            {
                var c = char.ToUpperInvariant(bruto);
                c = c switch { 'I' or 'L' => '1', 'O' => '0', _ => c };
                var v = Alfabeto.IndexOf(c);
                if (v < 0)
                    return null;

                buffer = (buffer << 5) | v;
                bits += 5;
                if (bits >= 8)
                {
                    bits -= 8;
                    saida.Add((byte)((buffer >> bits) & 0xFF));
                }
            }
            return saida.ToArray();
        }
    }
}
