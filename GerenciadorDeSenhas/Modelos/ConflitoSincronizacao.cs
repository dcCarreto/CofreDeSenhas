namespace GerenciadorDeSenhas.Modelos
{
    public enum TipoConflitoSincronizacao
    {
        EdicaoConcorrente,
        IntegridadeViolada
    }

    public sealed class ConflitoSincronizacao
    {
        public required Guid SenhaId { get; init; }
        public required string NomeServico { get; init; }
        public required TipoConflitoSincronizacao Tipo { get; init; }
        public required DateTime DetectadoEmUtc { get; init; }
    }
}
