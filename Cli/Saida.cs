using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas.Cli
{
    internal static class Saida
    {
        public static void Tabela(IReadOnlyList<Senha> itens)
        {
            if (itens.Count == 0)
            {
                Console.WriteLine("Nenhuma credencial.");
                return;
            }

            var linhas = itens
                .OrderBy(s => s.NomeServico, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Usuario, StringComparer.OrdinalIgnoreCase)
                .Select(s => new[]
                {
                    s.NomeServico,
                    s.Usuario,
                    Formatar.CategoriaLabel(s.Categoria),
                    string.IsNullOrEmpty(s.TotpSegredo) ? "" : "2FA",
                    Formatar.Data(s.DataAtualizacao),
                })
                .ToList();

            var cabecalho = new[] { "SERVIÇO", "USUÁRIO", "CATEGORIA", "2FA", "ATUALIZADO" };
            var largura = new int[cabecalho.Length];
            for (var c = 0; c < cabecalho.Length; c++)
                largura[c] = Math.Max(cabecalho[c].Length, linhas.Max(l => l[c].Length));

            Console.WriteLine(Montar(cabecalho, largura));
            Console.WriteLine(string.Join("  ", largura.Select(w => new string('-', w))));
            foreach (var l in linhas)
                Console.WriteLine(Montar(l, largura));

            Console.WriteLine();
            Console.WriteLine($"{itens.Count} credencial(is). Use \"cofre copiar <serviço>\" para a senha.");
        }

        private static string Montar(IReadOnlyList<string> celulas, int[] largura) =>
            string.Join("  ", celulas.Select((v, i) => v.PadRight(largura[i]))).TrimEnd();

        public static void Detalhe(Senha s)
        {
            Campo("Serviço", s.NomeServico);
            Campo("Usuário", s.Usuario);
            Campo("URL", s.Url);
            Campo("Categoria", Formatar.CategoriaLabel(s.Categoria));
            Campo("Tipo", Formatar.TipoLabel(s.Tipo));
            if (s.Etiquetas.Count > 0)
                Campo("Etiquetas", string.Join(", ", s.Etiquetas));
            Campo("2FA", string.IsNullOrEmpty(s.TotpSegredo) ? null : "configurado (use \"cofre copiar\" para a senha; o código 2FA fica no app)");
            if (s.Favorito) Campo("Favorito", "sim");
            Campo("Criado", Formatar.Data(s.DataCriacao));
            Campo("Atualizado", Formatar.Data(s.DataAtualizacao));
            if (!string.IsNullOrWhiteSpace(s.Notas))
            {
                Console.WriteLine("Notas:");
                foreach (var linha in s.Notas.Replace("\r\n", "\n").Split('\n'))
                    Console.WriteLine("  " + linha);
            }
            Console.WriteLine();
            Console.WriteLine($"Senha: use \"cofre copiar {Aspas(s.NomeServico)}\" — nunca é impressa.");
        }

        public static void ListaCurta(IReadOnlyList<Senha> candidatas)
        {
            Console.Error.WriteLine("Vários resultados — seja mais específico ou use --usuario:");
            foreach (var s in candidatas.OrderBy(s => s.NomeServico, StringComparer.OrdinalIgnoreCase))
                Console.Error.WriteLine($"  {s.NomeServico}  ({s.Usuario})");
        }

        private static void Campo(string rotulo, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
                Console.WriteLine($"{(rotulo + ":").PadRight(12)}{valor}");
        }

        private static string Aspas(string v) => v.Contains(' ') ? $"\"{v}\"" : v;
    }
}
