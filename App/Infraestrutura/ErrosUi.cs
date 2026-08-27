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

            if (ex is ILocalizavel erro)
                return Idioma.Formatar(erro.Chave, erro.Argumentos);

            // Exceções que não passam pelo padrão ILocalizavel deste app (IOException,
            // erros de driver de banco, timeout de rede etc.) não têm como ser
            // traduzidas — a mensagem técnica em si continua no idioma que o .NET/o
            // driver produziu. Mas sem este prefixo traduzido, o diálogo inteiro
            // aparecia cru em inglês pra quem usa o app em qualquer outro idioma,
            // quebrando a promessa de mensagens traduzidas. Redigir aqui também, mesma
            // razão do log: não repetir o caminho do perfil do Windows (com o nome de
            // usuário) numa mensagem que o usuário pode print e mandar pra suporte.
            return Idioma.Formatar("Common.UnexpectedError", Diagnostico.Redigir(PrimeiraLinha(ex.Message)));
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
