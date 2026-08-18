using CofreDeSenhas;
using GerenciadorDeSenhas.Modelos;
using Xunit;

namespace App.Testes
{
    public class CategoriasUITests
    {
        [Fact]
        public void LerCategoriaEEtiquetas_ComCategoriaComum_MantemEtiquetasIntactas()
        {
            var (categoria, etiquetas) = CategoriasUI.LerCategoriaEEtiquetas((int)Categoria.Work, "urgente, financeiro");

            Assert.Equal(Categoria.Work, categoria);
            Assert.Equal(new[] { "urgente", "financeiro" }, etiquetas);
        }

        [Fact]
        public void LerCategoriaEEtiquetas_ComOutroEUmaEtiquetaDeCategoria_ExtraiACategoriaDaEtiqueta()
        {
            var rotuloFinancas = CategoriasUI.Rotulo(Categoria.Finance);

            var (categoria, etiquetas) = CategoriasUI.LerCategoriaEEtiquetas(
                (int)Categoria.Other, $"{rotuloFinancas}, urgente");

            Assert.Equal(Categoria.Finance, categoria);
            Assert.Equal(new[] { "urgente" }, etiquetas);
        }

        [Fact]
        public void LerCategoriaEEtiquetas_ComOutroSemEtiquetaDeCategoria_PermaneceOutro()
        {
            var (categoria, etiquetas) = CategoriasUI.LerCategoriaEEtiquetas((int)Categoria.Other, "streaming");

            Assert.Equal(Categoria.Other, categoria);
            Assert.Equal(new[] { "streaming" }, etiquetas);
        }

        [Fact]
        public void LerCategoriaEEtiquetas_ComIndiceNegativo_UsaPrimeiraCategoria()
        {
            var (categoria, _) = CategoriasUI.LerCategoriaEEtiquetas(-1, null);

            Assert.Equal(Categoria.Work, categoria);
        }
    }
}
