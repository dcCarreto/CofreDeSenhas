using Avalonia.Controls;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Excecoes;

namespace CofreDeSenhas
{
    internal static class ErrosUi
    {
        public static string MensagemAmigavel(Exception ex)
        {
            Diagnostico.Registrar(ex);

            return ex is ILocalizavel erro
                ? Idioma.Formatar(erro.Chave, erro.Argumentos)
                : PrimeiraLinha(ex.Message);
        }

        public static Task MostrarErroAsync(Window dono, Exception ex, string titulo) =>
            CaixaMensagem.MostrarAsync(dono, MensagemAmigavel(ex), titulo, TipoMensagem.Erro);

        private static string PrimeiraLinha(string texto)
        {
            var quebra = texto.IndexOf('\n');
            return quebra < 0 ? texto : texto[..quebra].TrimEnd('\r');
        }
    }
}
