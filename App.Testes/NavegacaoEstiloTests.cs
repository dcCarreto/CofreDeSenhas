using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace App.Testes
{
    public class NavegacaoEstiloTests
    {
        private static void Avancar(int quadros = 1)
        {
            for (int i = 0; i < quadros; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
            }
        }

        private static IBrush? FundoVisivel(Button botao) =>
            botao.GetVisualDescendants().OfType<ContentPresenter>()
                .First(c => c.Name == "PART_ContentPresenter").Background;

        private static bool Transparente(IBrush? pincel) =>
            pincel is null || pincel is ISolidColorBrush s && s.Color.A == 0;

        // O usuário passa o mouse e clica nas abas, uma depois da outra (como na
        // navegação das Configurações). Itens já visitados não podem ficar com o
        // fundo de hover/pressionado preso depois que o mouse sai.
        [AvaloniaFact]
        public void ItensDeNavegacao_DepoisDeHoverEClique_VoltamAoFundoTransparente()
        {
            var botoes = Enumerable.Range(0, 4).Select(i =>
            {
                var b = new Button { Content = "Item " + i, Height = 40, HorizontalAlignment = HorizontalAlignment.Stretch };
                b.Classes.Add("nav-item");
                return b;
            }).ToArray();
            var pilha = new StackPanel();
            foreach (var b in botoes)
                pilha.Children.Add(b);

            var janela = new Window { Width = 300, Height = 260, Content = pilha };
            janela.Show();
            Avancar(2);

            for (int i = 0; i < botoes.Length; i++)
            {
                var ponto = new Point(100, i * 40 + 20);
                janela.MouseMove(ponto, RawInputModifiers.None);
                Avancar(3);
                janela.MouseDown(ponto, MouseButton.Left, RawInputModifiers.None);
                Avancar(1);
                janela.MouseUp(ponto, MouseButton.Left, RawInputModifiers.None);
                for (int j = 0; j < botoes.Length; j++)
                    botoes[j].Classes.Set("ativo", j == i);
                Avancar(1);
            }

            janela.MouseMove(new Point(100, 250), RawInputModifiers.None);
            Avancar(60);

            for (int i = 0; i < botoes.Length - 1; i++)
                Assert.True(Transparente(FundoVisivel(botoes[i])),
                    $"item {i} ficou com fundo {FundoVisivel(botoes[i])}");
            Assert.False(Transparente(FundoVisivel(botoes[^1])), "o item ativo deve manter o realce");

            janela.Close();
        }
    }
}
