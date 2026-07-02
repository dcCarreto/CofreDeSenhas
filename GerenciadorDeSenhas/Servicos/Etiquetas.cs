using System.Globalization;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public static class Etiquetas
    {
        public const int ComprimentoMaximo = 30;
        public const int QuantidadeMaxima = 20;

        private static readonly char[] Separadores = { ',', ';', '\n', '\r', '\t', '/', '\\', '|' };

        public static List<string> Analisar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return new List<string>();

            return Normalizar(texto.Split(Separadores));
        }

        public static List<string> Normalizar(IEnumerable<string>? etiquetas)
        {
            var resultado = new List<string>();
            if (etiquetas == null)
                return resultado;

            var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var bruta in etiquetas)
            {
                var limpa = Limpar(bruta);
                if (limpa.Length == 0 || !vistas.Add(limpa))
                    continue;

                resultado.Add(limpa);
                if (resultado.Count >= QuantidadeMaxima)
                    break;
            }

            return resultado;
        }

        public static string Formatar(IEnumerable<string> etiquetas) =>
            string.Join(", ", etiquetas);

        public static List<string> Distintas(IEnumerable<Senha> senhas)
        {
            var todas = new List<string>();
            foreach (var senha in senhas)
                todas.AddRange(senha.Etiquetas);

            return Normalizar(todas)
                .OrderBy(e => e, StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), ignoreCase: true))
                .ToList();
        }

        private static string Limpar(string bruta)
        {
            if (string.IsNullOrWhiteSpace(bruta))
                return string.Empty;

            var texto = string.Join(' ', bruta.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (texto.Length > ComprimentoMaximo)
                texto = texto[..ComprimentoMaximo].TrimEnd();

            return texto;
        }
    }
}
