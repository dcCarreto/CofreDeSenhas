namespace GerenciadorDeSenhas.Modelos
{
    public sealed class ResultadoAuditoriaCofre
    {
        public ResultadoAuditoriaCofre(int totalSenhas, int naoAuditadas, IReadOnlyList<ItemAuditoriaSenha> itens)
        {
            TotalSenhas = totalSenhas;
            NaoAuditadas = naoAuditadas;
            Itens = itens;
        }

        public int TotalSenhas { get; }

        public int NaoAuditadas { get; }

        public IReadOnlyList<ItemAuditoriaSenha> Itens { get; }

        public int TotalComAchados => Itens.Count;

        public int TotalFracas => Itens.Count(i => i.TemAchado(TipoAchadoAuditoriaSenha.Fraca));

        public int TotalRepetidas => Itens.Count(i => i.TemAchado(TipoAchadoAuditoriaSenha.Repetida));

        public int TotalAntigas => Itens.Count(i => i.TemAchado(TipoAchadoAuditoriaSenha.Antiga));
    }
}
