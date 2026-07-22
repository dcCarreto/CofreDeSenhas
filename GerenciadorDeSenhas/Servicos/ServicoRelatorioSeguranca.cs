using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public static class ServicoRelatorioSeguranca
    {
        private const int PesoFraca = 25;
        private const int PesoRepetida = 20;
        private const int PesoAntiga = 15;
        private const int PesoComprometida = 30;
        private const int PesoSemTotp = 5;
        private const int PesoSemUrl = 3;
        private const int PesoSemCategoria = 2;
        private const int PontuacaoMaxima = 100;

        public static RelatorioSegurancaCofre Gerar(IReadOnlyCollection<Senha> senhas,
            ResultadoAuditoriaCofre auditoria, IReadOnlyDictionary<Guid, int>? vazamentosPorId = null)
        {
            if (senhas == null)
                throw new ArgumentNullException(nameof(senhas));
            if (auditoria == null)
                throw new ArgumentNullException(nameof(auditoria));

            int total = senhas.Count;
            int comprometidas = vazamentosPorId == null
                ? 0
                : senhas.Count(s => vazamentosPorId.TryGetValue(s.Id, out var c) && c > 0);
            int semTotp = senhas.Count(s => string.IsNullOrEmpty(s.TotpSegredo));
            int semUrl = senhas.Count(s => string.IsNullOrWhiteSpace(s.Url));
            int semCategoria = senhas.Count(s => s.Categoria == Categoria.Other && s.Etiquetas.Count == 0);

            int pontuacao = CalcularPontuacao(total, auditoria.TotalFracas, auditoria.TotalRepetidas,
                auditoria.TotalAntigas, comprometidas, semTotp, semUrl, semCategoria);

            return new RelatorioSegurancaCofre
            {
                TotalSenhas = total,
                Fracas = auditoria.TotalFracas,
                Repetidas = auditoria.TotalRepetidas,
                Antigas = auditoria.TotalAntigas,
                Comprometidas = comprometidas,
                SemTotp = semTotp,
                SemUrl = semUrl,
                SemCategoria = semCategoria,
                Pontuacao = pontuacao
            };
        }

        private static int CalcularPontuacao(int total, int fracas, int repetidas, int antigas,
            int comprometidas, int semTotp, int semUrl, int semCategoria)
        {
            if (total == 0)
                return PontuacaoMaxima;

            double penalidade =
                PesoFraca * ((double)fracas / total) +
                PesoRepetida * ((double)repetidas / total) +
                PesoAntiga * ((double)antigas / total) +
                PesoComprometida * ((double)comprometidas / total) +
                PesoSemTotp * ((double)semTotp / total) +
                PesoSemUrl * ((double)semUrl / total) +
                PesoSemCategoria * ((double)semCategoria / total);

            int pontuacao = PontuacaoMaxima - (int)Math.Round(penalidade, MidpointRounding.AwayFromZero);
            return Math.Clamp(pontuacao, 0, PontuacaoMaxima);
        }
    }
}
