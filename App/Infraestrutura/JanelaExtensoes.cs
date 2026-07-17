using Avalonia.Controls;
using Avalonia.Input;

namespace CofreDeSenhas
{
    internal static class JanelaExtensoes
    {
        public static void FecharComEsc(this Window janela) =>
            janela.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) janela.Close(false);
            };

        public static void FecharComEscConfirmarComEnter(this Window janela, Action confirmar) =>
            janela.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) confirmar();
                if (e.Key == Key.Escape) janela.Close(false);
            };

        public static void HabilitarArraste(this Window janela, PointerPressedEventArgs e, Func<object?, bool>? ignorarOrigem = null)
        {
            if (!e.GetCurrentPoint(janela).Properties.IsLeftButtonPressed)
                return;
            if (ignorarOrigem != null && ignorarOrigem(e.Source))
                return;
            janela.BeginMoveDrag(e);
        }
    }
}
