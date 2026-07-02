using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas
{
    internal static class CategoriasUI
    {
        public static readonly string[] Rotulos = { "Trabalho", "Pessoal", "Finanças", "Social", "Outro" };

        public static bool TentarObterCategoria(string? rotulo, out Categoria categoria)
        {
            categoria = Categoria.Other;
            if (string.IsNullOrWhiteSpace(rotulo))
                return false;

            for (int i = 0; i < Rotulos.Length; i++)
            {
                if (!string.Equals(Rotulos[i], rotulo.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                categoria = (Categoria)i;
                return true;
            }

            return false;
        }
    }
}
