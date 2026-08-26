using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoVazamento
    {
        private static readonly TimeSpan TimeoutRequisicao = TimeSpan.FromSeconds(10);

        // A API do Have I Been Pwned exige k-anonymity: só os 5 primeiros
        // caracteres do hash SHA-1 são enviados, nunca a senha nem o hash completo.
        private const int TamanhoPrefixoKAnonymity = 5;

        private static readonly HttpClient _http = CriarClient();

        private static HttpClient CriarClient()
        {
            var c = new HttpClient { Timeout = TimeoutRequisicao };

            c.DefaultRequestHeaders.Add("User-Agent", "GerenciadorDeSenhas-App");
            return c;
        }

        public async Task<int> VerificarAsync(string senha)
        {
            if (string.IsNullOrEmpty(senha)) return 0;

            var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(senha));
            var hash = Convert.ToHexString(hashBytes);

            var prefixo = hash.Substring(0, TamanhoPrefixoKAnonymity);
            var sufixo = hash.Substring(TamanhoPrefixoKAnonymity);

            var resposta = await _http.GetStringAsync($"https://api.pwnedpasswords.com/range/{prefixo}");

            return ContarOcorrencias(resposta, sufixo);
        }

        // Público só pra GerenciadorDeSenhas.Testes exercitar o parsing sem bater na
        // API de verdade.
        public static int ContarOcorrencias(string resposta, string sufixo)
        {
            foreach (var linha in resposta.Split('\n'))
            {
                var partes = linha.Split(':');
                if (partes.Length != 2 || !partes[0].Trim().Equals(sufixo, StringComparison.OrdinalIgnoreCase))
                    continue;

                // O sufixo bateu mas a contagem não é um número — resposta da API em
                // formato inesperado. Continuar o loop como se não tivesse batido faria
                // esta checagem de segurança relatar "senha não vazada" (fail-open)
                // justamente no caso em que ela não conseguiu confirmar nada; melhor
                // estourar e deixar o chamador (que já trata erro de rede) avisar que a
                // verificação falhou.
                if (!int.TryParse(partes[1].Trim(), out int contagem))
                    throw new FormatException("Resposta da API Have I Been Pwned em formato inesperado.");

                return contagem;
            }

            return 0;
        }
    }
}
