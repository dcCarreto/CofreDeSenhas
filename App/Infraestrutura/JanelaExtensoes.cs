using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace CofreDeSenhas
{
    internal static class JanelaExtensoes
    {
        public static void MostrarErroInline(this Window janela, TextBlock rotulo, string mensagem, TextBox? focoAposErro = null)
        {
            rotulo.Text = mensagem;
            AutomationProperties.SetName(rotulo, mensagem);

            // Limpa qualquer destaque de erro anterior antes de aplicar o atual — evita
            // borda vermelha "presa" num campo que já não é mais o problema.
            foreach (var campo in janela.GetVisualDescendants().OfType<TextBox>())
                campo.Classes.Remove("erro");

            if (!string.IsNullOrWhiteSpace(mensagem))
            {
                focoAposErro?.Classes.Add("erro");
                Acessibilidade.Anunciar(janela, mensagem, assertivo: true);
            }

            if (focoAposErro != null)
            {
                focoAposErro.Focus();
                focoAposErro.SelectAll();
            }
        }

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
