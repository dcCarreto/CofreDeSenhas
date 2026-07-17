using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace CofreDeSenhas
{
    internal static class Scrim
    {
        private sealed class Camada
        {
            public required Border Borda { get; init; }
            public int Contagem;
        }

        private static readonly ConditionalWeakTable<Window, Camada> _camadas = new();

        public static void Mostrar(Window dono)
        {
            var camada = ObterOuCriar(dono);
            camada.Contagem++;
            if (camada.Contagem > 1)
                return;

            camada.Borda.IsHitTestVisible = true;
            camada.Borda.Transitions = null;

            if (Acessibilidade.ReduzirAnimacoes)
            {
                camada.Borda.Opacity = 1;
                return;
            }

            camada.Borda.Opacity = 0;
            Dispatcher.UIThread.Post(() =>
            {
                camada.Borda.Transitions = new Transitions
                {
                    new DoubleTransition
                    {
                        Property = Visual.OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(170),
                        Easing = new CubicEaseOut()
                    }
                };
                camada.Borda.Opacity = 1;
            }, DispatcherPriority.Render);
        }

        public static void Ocultar(Window dono)
        {
            if (!_camadas.TryGetValue(dono, out var camada))
                return;

            camada.Contagem = Math.Max(0, camada.Contagem - 1);
            if (camada.Contagem > 0)
                return;

            camada.Borda.Opacity = 0;
            camada.Borda.IsHitTestVisible = false;
        }

        private static Camada ObterOuCriar(Window dono)
        {
            if (_camadas.TryGetValue(dono, out var existente))
                return existente;

            var conteudoOriginal = dono.Content as Control;
            var borda = new Border
            {
                CornerRadius = ObterRaioExterno(conteudoOriginal),
                Background = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
                Opacity = 0,
                IsHitTestVisible = false
            };

            dono.Content = null;

            var raiz = new Panel();
            if (conteudoOriginal != null)
                raiz.Children.Add(conteudoOriginal);
            raiz.Children.Add(borda);

            dono.Content = raiz;

            var camada = new Camada { Borda = borda };
            _camadas.Add(dono, camada);
            return camada;
        }

        private static CornerRadius ObterRaioExterno(Control? raiz)
        {
            var atual = raiz;
            for (var i = 0; i < 3 && atual != null; i++)
            {
                if (atual is Border borda)
                    return borda.CornerRadius;

                atual = atual switch
                {
                    Panel painel when painel.Children.Count > 0 => painel.Children[0] as Control,
                    ContentControl cc => cc.Content as Control,
                    Decorator dec => dec.Child,
                    _ => null
                };
            }
            return new CornerRadius(0);
        }
    }
}
