using System.Text.RegularExpressions;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public sealed class ServicoAuditoriaSenha
    {
        public const int DiasSenhaAntigaPadrao = 365;

        public ResultadoAuditoriaCofre Auditar(IEnumerable<Senha> senhas,
            Func<Senha, string?> obterSenhaPlaintext,
            DateTime? referenciaUtc = null,
            int diasSenhaAntiga = DiasSenhaAntigaPadrao)
        {
            if (senhas == null)
                throw new ArgumentNullException(nameof(senhas));
            if (obterSenhaPlaintext == null)
                throw new ArgumentNullException(nameof(obterSenhaPlaintext));
            if (diasSenhaAntiga <= 0)
                throw new ArgumentOutOfRangeException(nameof(diasSenhaAntiga), "O limite deve ser maior que zero.");

            var lista = senhas.ToList();
            var referencia = ParaUtc(referenciaUtc ?? DateTime.UtcNow);
            var plaintextPorId = new Dictionary<Guid, string>();
            int naoAuditadas = 0;

            foreach (var senha in lista)
            {
                string? plaintext;
                try
                {
                    plaintext = obterSenhaPlaintext(senha);
                }
                catch
                {
                    plaintext = null;
                }

                if (string.IsNullOrEmpty(plaintext))
                {
                    naoAuditadas++;
                    continue;
                }

                plaintextPorId[senha.Id] = plaintext;
            }

            var repeticoesPorId = MapearRepeticoes(plaintextPorId);
            var itens = new List<ItemAuditoriaSenha>();

            foreach (var senha in lista)
            {
                var achados = new List<TipoAchadoAuditoriaSenha>();
                int ocorrenciasRepetidas = 0;

                if (plaintextPorId.TryGetValue(senha.Id, out var plaintext))
                {
                    if (!SenhaForteParaAuditoria(plaintext))
                        achados.Add(TipoAchadoAuditoriaSenha.Fraca);

                    if (repeticoesPorId.TryGetValue(senha.Id, out ocorrenciasRepetidas))
                        achados.Add(TipoAchadoAuditoriaSenha.Repetida);
                }

                int diasSemAtualizacao = CalcularDiasSemAtualizacao(senha.DataAtualizacao, referencia);
                if (diasSemAtualizacao >= diasSenhaAntiga)
                    achados.Add(TipoAchadoAuditoriaSenha.Antiga);

                if (achados.Count == 0)
                    continue;

                itens.Add(new ItemAuditoriaSenha
                {
                    Senha = senha,
                    Achados = achados,
                    DiasSemAtualizacao = diasSemAtualizacao,
                    OcorrenciasSenhaRepetida = ocorrenciasRepetidas
                });
            }

            return new ResultadoAuditoriaCofre(lista.Count, naoAuditadas, itens);
        }

        private static Dictionary<Guid, int> MapearRepeticoes(Dictionary<Guid, string> plaintextPorId)
        {
            var repeticoes = new Dictionary<Guid, int>();

            foreach (var grupo in plaintextPorId.GroupBy(p => p.Value, StringComparer.Ordinal))
            {
                var entradas = grupo.ToList();
                if (entradas.Count <= 1)
                    continue;

                foreach (var entrada in entradas)
                    repeticoes[entrada.Key] = entradas.Count;
            }

            return repeticoes;
        }

        private static bool SenhaForteParaAuditoria(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
                return false;

            if (EhPassphraseForte(senha))
                return true;

            return senha.Length >= 12
                && Regex.IsMatch(senha, @"[A-Z]")
                && Regex.IsMatch(senha, @"[a-z]")
                && Regex.IsMatch(senha, @"\d")
                && Regex.IsMatch(senha, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
        }

        private static bool EhPassphraseForte(string senha)
        {
            if (senha.Length < 20)
                return false;

            var partes = senha.Split(new[] { '-', '_', '.', ' ' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int palavras = partes.Count(p => p.Length >= 3 && p.All(char.IsLetter));

            return palavras >= 4;
        }

        private static int CalcularDiasSemAtualizacao(DateTime dataAtualizacao, DateTime referenciaUtc)
        {
            var atualizacaoUtc = ParaUtc(dataAtualizacao);
            var dias = (referenciaUtc - atualizacaoUtc).TotalDays;
            return Math.Max(0, (int)Math.Floor(dias));
        }

        private static DateTime ParaUtc(DateTime data)
        {
            return data.Kind switch
            {
                DateTimeKind.Local => data.ToUniversalTime(),
                DateTimeKind.Utc => data,
                _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
            };
        }
    }
}
