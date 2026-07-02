using GerenciadorDeSenhas.Modelos;

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
    }
}
