using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas
{
    internal static class CategoriasUI
    {
        private static readonly Categoria[] Categorias =
        {
            Categoria.Work,
            Categoria.Personal,
            Categoria.Finance,
            Categoria.Social,
            Categoria.Other
        };

        public static string[] Rotulos => Categorias.Select(Idioma.RotuloCategoria).ToArray();

        public static string Rotulo(Categoria categoria) => Idioma.RotuloCategoria(categoria);

        public static bool TentarObterCategoria(string? rotulo, out Categoria categoria)
        {
            categoria = Categoria.Other;
            if (string.IsNullOrWhiteSpace(rotulo))
                return false;

            for (int i = 0; i < Categorias.Length; i++)
            {
                if (!Idioma.RotulosCategoria(Categorias[i]).Any(r =>
                    string.Equals(r, rotulo.Trim(), StringComparison.OrdinalIgnoreCase)))
                    continue;

                categoria = Categorias[i];
                return true;
            }

            return false;
        }

        public static (Categoria Categoria, List<string> Etiquetas) LerCategoriaEEtiquetas(int categoriaIndex, string? etiquetasTexto)
        {
            var categoria = (Categoria)Math.Max(0, categoriaIndex);
            var etiquetas = Etiquetas.Analisar(etiquetasTexto);

            if (categoria == Categoria.Other)
            {
                // "!= Categoria.Other" evita reclassificar pra a categoria que já era —
                // sem isto, uma etiqueta cujo texto batesse com a tradução de "Other"
                // em qualquer idioma suportado (mesmo não sendo o idioma atual) era
                // engolida silenciosamente, já que TentarObterCategoria varre todos os
                // idiomas de uma vez.
                var indice = etiquetas.FindIndex(e => TentarObterCategoria(e, out var candidata) && candidata != Categoria.Other);
                if (indice >= 0)
                {
                    TentarObterCategoria(etiquetas[indice], out categoria);
                    etiquetas.RemoveAt(indice);
                }
            }

            return (categoria, etiquetas);
        }
    }
}
