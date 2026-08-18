using Avalonia.Controls;
using Avalonia.Input.Platform;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas
{
    internal static class AreaTransferenciaFeedback
    {
        public static async Task<(bool Copiado, bool VaiLimpar)> CopiarComAvisoAsync(IClipboard? clipboard, string texto, Control anunciador, string nomeAcao)
        {
            bool copiado = false;
            if (clipboard != null)
            {
                try { await clipboard.SetTextAsync(texto); copiado = true; } catch { }
            }

            if (!copiado)
                return (false, false);

            int segundos = Preferencias.SegundosLimpezaClipboard;
            bool vaiLimpar = segundos > 0;
            if (vaiLimpar)
            {
                Acessibilidade.Anunciar(anunciador, Idioma.Formatar("A11y.CopiedWillClear", nomeAcao, segundos));
                _ = ServicoLimpezaClipboard.ProgramarLimpezaAsync(new AreaTransferenciaAvalonia(clipboard!), texto, segundos);
            }
            else
            {
                Acessibilidade.Anunciar(anunciador, Idioma.Formatar("A11y.Copied", nomeAcao));
            }

            return (true, vaiLimpar);
        }
    }
}
