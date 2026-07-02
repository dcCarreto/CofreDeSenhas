namespace GerenciadorDeSenhas.Modelos
{
    public sealed class ItemAuditoriaSenha
    {
        public required Senha Senha { get; init; }

        public required IReadOnlyCollection<TipoAchadoAuditoriaSenha> Achados { get; init; }

        public int DiasSemAtualizacao { get; init; }

        public int OcorrenciasSenhaRepetida { get; init; }

        public bool TemAchado(TipoAchadoAuditoriaSenha tipo) => Achados.Contains(tipo);
    }
}
