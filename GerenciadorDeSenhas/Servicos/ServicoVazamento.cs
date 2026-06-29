using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoVazamento
    {
        private static readonly HttpClient _http = CriarClient();

        private static HttpClient CriarClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            c.DefaultRequestHeaders.Add("User-Agent", "GerenciadorDeSenhas-App");
            return c;
        }

        public async Task<int> VerificarAsync(string senha)
        {
            if (string.IsNullOrEmpty(senha)) return 0;

            var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(senha));
            var hash = Convert.ToHexString(hashBytes);

            var prefixo = hash.Substring(0, 5);
            var sufixo = hash.Substring(5);

            var resposta = await _http.GetStringAsync($"https://api.pwnedpasswords.com/range/{prefixo}");

            foreach (var linha in resposta.Split('\n'))
            {
                var partes = linha.Split(':');
                if (partes.Length == 2 && partes[0].Trim().Equals(sufixo, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(partes[1].Trim(), out int contagem))
                        return contagem;
                }
            }

            return 0;
        }
    }
}
