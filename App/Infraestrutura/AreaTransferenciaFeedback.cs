using Avalonia.Controls;
using Avalonia.Input.Platform;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas
{
    internal static class AreaTransferenciaFeedback
    {
        public static async Task<bool> CopiarComAvisoAsync(IClipboard? clipboard, string texto, Control anunciador, string nomeAcao)
        {
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(texto); } catch { }
            }

            int segundos = Preferencias.SegundosLimpezaClipboard;
            bool vaiLimpar = segundos > 0 && clipboard != null;
            if (vaiLimpar)
            {
                Acessibilidade.Anunciar(anunciador, Idioma.Formatar("A11y.CopiedWillClear", nomeAcao, segundos));
                _ = ServicoLimpezaClipboard.ProgramarLimpezaAsync(new AreaTransferenciaAvalonia(clipboard!), texto, segundos);
            }
            else
            {
                Acessibilidade.Anunciar(anunciador, Idioma.Formatar("A11y.Copied", nomeAcao));
            }

            return vaiLimpar;
        }
    }
}
