using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoGeracaoSenha
    {
        private const string Maiusculas = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Minusculas = "abcdefghijklmnopqrstuvwxyz";
        private const string Numeros = "0123456789";
        private const string Especiais = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        public List<string> GerarSenhas(int quantidade, int tamanho, bool incluirMaiusculas,
            bool incluirMinusculas, bool incluirNumeros, bool incluirEspeciais)
        {
            ValidarQuantidade(quantidade);

            var senhas = new List<string>(quantidade);
            for (int i = 0; i < quantidade; i++)
            {
                senhas.Add(GerarSenha(tamanho, incluirMaiusculas, incluirMinusculas,
                    incluirNumeros, incluirEspeciais));
            }

            return senhas;
        }

        public string GerarSenha(int tamanho, bool incluirMaiusculas, bool incluirMinusculas,
            bool incluirNumeros, bool incluirEspeciais)
        {
            if (tamanho < 4 || tamanho > 1000)
                throw new ArgumentException("O tamanho da senha deve ficar entre 4 e 1000 caracteres.");

            var opcoes = new StringBuilder();
            if (incluirMaiusculas) opcoes.Append(Maiusculas);
            if (incluirMinusculas) opcoes.Append(Minusculas);
            if (incluirNumeros) opcoes.Append(Numeros);
            if (incluirEspeciais) opcoes.Append(Especiais);

            if (opcoes.Length == 0)
                throw new ArgumentException("Selecione pelo menos um tipo de caractere.");

            var senha = new StringBuilder(tamanho);
            for (int i = 0; i < tamanho; i++)
                senha.Append(opcoes[RandomNumberGenerator.GetInt32(opcoes.Length)]);

            return senha.ToString();
        }

        public List<string> GerarFrasesSenha(int quantidade, int quantidadePalavras,
            string separador = "-", bool capitalizar = false, bool incluirNumero = true)
        {
            return GerarFrasesSenha(PalavrasPassphrase.Padrao, quantidade, quantidadePalavras,
                separador, capitalizar, incluirNumero);
        }

        public List<string> GerarFrasesSenha(IReadOnlyList<string> palavras, int quantidade,
            int quantidadePalavras, string separador = "-", bool capitalizar = false,
            bool incluirNumero = true)
        {
            ValidarQuantidade(quantidade);

            var frases = new List<string>(quantidade);
            for (int i = 0; i < quantidade; i++)
            {
                frases.Add(GerarFraseSenha(palavras, quantidadePalavras, separador,
                    capitalizar, incluirNumero));
            }

            return frases;
        }

        public string GerarFraseSenha(int quantidadePalavras = 5, string separador = "-",
            bool capitalizar = false, bool incluirNumero = true)
        {
            return GerarFraseSenha(PalavrasPassphrase.Padrao, quantidadePalavras,
                separador, capitalizar, incluirNumero);
        }

        public string GerarFraseSenha(IReadOnlyList<string> palavras, int quantidadePalavras,
            string separador = "-", bool capitalizar = false, bool incluirNumero = true)
        {
            if (quantidadePalavras < 3 || quantidadePalavras > 12)
                throw new ArgumentException("A frase-senha deve ter entre 3 e 12 palavras.");

            if (separador == null)
                throw new ArgumentNullException(nameof(separador));

            if (separador.Length > 3)
                throw new ArgumentException("O separador deve ter no máximo 3 caracteres.");

            var palavrasValidas = NormalizarPalavras(palavras);
            var partes = new List<string>(quantidadePalavras + (incluirNumero ? 1 : 0));

            for (int i = 0; i < quantidadePalavras; i++)
            {
                var palavra = palavrasValidas[RandomNumberGenerator.GetInt32(palavrasValidas.Count)];
                partes.Add(capitalizar ? Capitalizar(palavra) : palavra);
            }

            if (incluirNumero)
                partes.Add(RandomNumberGenerator.GetInt32(10, 100).ToString(CultureInfo.InvariantCulture));

            return string.Join(separador, partes);
        }

        private static List<string> NormalizarPalavras(IReadOnlyList<string> palavras)
        {
            if (palavras == null)
                throw new ArgumentNullException(nameof(palavras));

            var palavrasValidas = palavras
                .Select(p => p?.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (palavrasValidas.Count < 2)
                throw new ArgumentException("Informe uma lista com pelo menos duas palavras.");

            return palavrasValidas;
        }

        private static void ValidarQuantidade(int quantidade)
        {
            if (quantidade < 1 || quantidade > 50)
                throw new ArgumentException("A quantidade deve ficar entre 1 e 50.");
        }

        private static string Capitalizar(string palavra)
        {
            return palavra.Length == 1
                ? palavra.ToUpperInvariant()
                : char.ToUpperInvariant(palavra[0]) + palavra[1..];
        }
    }
}
