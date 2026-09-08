using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;

namespace App.Testes
{
    public class JanelaAjudaTests
    {
        private static string TextoInline(TextBlock tb)
        {
            var sb = new StringBuilder();
            if (tb.Inlines != null)
                foreach (var i in tb.Inlines)
                    if (i is Run r)
                        sb.Append(r.Text);
            return sb.ToString();
        }

        [AvaloniaFact]
        public async Task Abrir_ListaTodosOsTopicos()
        {
            var janela = new JanelaAjuda();
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var trilha = janela.Encontrar<StackPanel>("TrilhaTopicos");
            Assert.Equal(ManualAjuda.Topicos.Count, trilha.Children.OfType<Button>().Count());
            Assert.NotEmpty(janela.Encontrar<StackPanel>("AreaConteudo").Children);
        }

        [AvaloniaFact]
        public async Task Navegar_TrocaTituloEConteudo()
        {
            var janela = new JanelaAjuda();
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.Navegar("faq");
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.Equal("faq", janela.TopicoAtual);
            Assert.Equal(ManualAjuda.Titulo("faq"), janela.Encontrar<TextBlock>("TxtTitulo").Text);
            Assert.NotEmpty(janela.Encontrar<StackPanel>("AreaConteudo").Children);
        }

        [AvaloniaFact]
        public async Task LinkInterno_MapeiaAPosicaoDoTextoParaOTopico()
        {
            var janela = new JanelaAjuda { Width = 900, Height = 760 };
            janela.Show();
            janela.Navegar("faq");
            await TesteUtil.AguardarAsync(() => false, tentativas: 8);

            const string rotulo = "Senha mestra";
            TextBlock? paragrafo = null;
            int indice = -1;
            foreach (var tb in janela.Encontrar<StackPanel>("AreaConteudo").GetVisualDescendants().OfType<TextBlock>())
            {
                indice = TextoInline(tb).IndexOf(rotulo, StringComparison.Ordinal);
                if (indice >= 0) { paragrafo = tb; break; }
            }
            Assert.NotNull(paragrafo);
            Assert.Contains(paragrafo!.Inlines!.OfType<Run>(), r => r.Text == rotulo && r.TextDecorations != null);

            await TesteUtil.AguardarAsync(() => paragrafo!.Bounds.Width > 0, tentativas: 30);
            var caixas = paragrafo!.TextLayout.HitTestTextRange(indice, rotulo.Length).ToList();
            Assert.NotEmpty(caixas);

            var posicao = paragrafo.TextLayout.HitTestPoint(caixas[0].Center).TextPosition;
            Assert.InRange(posicao, indice, indice + rotulo.Length - 1);
        }

        [AvaloniaFact]
        public async Task AbrirOuFocar_ReaproveitaAMesmaJanela()
        {
            var dono = new Window();
            dono.Show();
            try
            {
                JanelaAjuda.AbrirOuFocar(dono, "faq");
                var primeira = JanelaAjuda.Instancia;
                Assert.NotNull(primeira);

                JanelaAjuda.AbrirOuFocar(dono, "backup");
                Assert.Same(primeira, JanelaAjuda.Instancia);
                Assert.Equal("backup", JanelaAjuda.Instancia!.TopicoAtual);
            }
            finally
            {
                JanelaAjuda.Instancia?.Close();
                await TesteUtil.AguardarAsync(() => JanelaAjuda.Instancia == null, tentativas: 20);
            }
        }
    }
}
