using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using CofreDeSenhas;

namespace App.Testes
{
    public class RenderizadorAjudaTests
    {
        private static StackPanel Render(string markdown, RenderizadorAjuda? r = null) =>
            (StackPanel)(r ?? new RenderizadorAjuda()).Render(markdown);

        private static string TextoDe(TextBlock tb)
        {
            var sb = new StringBuilder();
            if (tb.Inlines != null)
                foreach (var inline in tb.Inlines)
                    Coletar(inline, sb);
            return sb.ToString();
        }

        private static void Coletar(Inline inline, StringBuilder sb)
        {
            switch (inline)
            {
                case Run r: sb.Append(r.Text); break;
                case InlineUIContainer { Child: Button b }: sb.Append(b.Content); break;
                case Span s:
                    foreach (var i in s.Inlines) Coletar(i, sb);
                    break;
            }
        }

        [AvaloniaFact]
        public void Titulos_ViramTextBlocksComTamanhoDecrescente()
        {
            var raiz = Render("# Um\n\n## Dois\n\n### Tres");

            var titulos = raiz.Children.OfType<TextBlock>().ToList();
            Assert.Equal(new[] { "Um", "Dois", "Tres" }, titulos.Select(t => t.Text));
            Assert.True(titulos[0].FontSize > titulos[1].FontSize);
            Assert.True(titulos[1].FontSize >= titulos[2].FontSize);
        }

        [AvaloniaFact]
        public void LinhasSeguidas_ViramUmParagrafoSo()
        {
            var raiz = Render("linha um\nlinha dois");

            var p = Assert.IsType<TextBlock>(Assert.Single(raiz.Children));
            Assert.Equal("linha um linha dois", TextoDe(p));
        }

        [AvaloniaFact]
        public void Negrito_ViraRunComPeso()
        {
            var raiz = Render("antes **forte** depois");

            var p = (TextBlock)raiz.Children[0];
            var forte = p.Inlines!.OfType<Run>().Single(r => r.Text == "forte");
            Assert.Equal(FontWeight.SemiBold, forte.FontWeight);
        }

        [AvaloniaFact]
        public void CodigoInline_UsaFonteMono()
        {
            var raiz = Render("rode `dotnet build` agora");
            var mono = (FontFamily)Application.Current!.FindResource("FonteMono")!;

            var p = (TextBlock)raiz.Children[0];
            var codigo = p.Inlines!.OfType<Run>().Single(r => r.Text == "dotnet build");
            Assert.Equal(mono, codigo.FontFamily);
        }

        [AvaloniaFact]
        public void LinkInterno_ViraRunSublinhadoComDestaque()
        {
            var raiz = Render("veja o [FAQ](faq) para mais");

            var p = (TextBlock)raiz.Children[0];
            var link = p.Inlines!.OfType<Run>().Single(r => r.Text == "FAQ");
            Assert.NotNull(link.TextDecorations);
            Assert.NotEqual("veja o para mais", TextoDe(p));
            Assert.Equal("veja o FAQ para mais", TextoDe(p));
        }

        [AvaloniaFact]
        public void Lista_GeraUmItemPorLinha()
        {
            var raiz = Render("- um\n- dois\n- tres");

            var lista = Assert.IsType<StackPanel>(Assert.Single(raiz.Children));
            Assert.Equal(3, lista.Children.Count);
        }

        [AvaloniaFact]
        public void ListaOrdenada_MantemOsNumeros()
        {
            var raiz = Render("1. primeiro\n2. segundo");

            var lista = (StackPanel)raiz.Children[0];
            var marcas = lista.Children.OfType<Grid>()
                .Select(g => ((TextBlock)g.Children[0]).Text)
                .ToList();
            Assert.Equal(new[] { "1.", "2." }, marcas);
        }

        [AvaloniaFact]
        public void Citacao_ViraCaixaDeNota()
        {
            var raiz = Render("> cuidado com isto");
            Assert.IsType<Border>(Assert.Single(raiz.Children));
        }
    }
}
