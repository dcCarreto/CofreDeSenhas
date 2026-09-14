namespace GerenciadorDeSenhas.Modelos
{
    public class AnexoSenha
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string NomeArquivo { get; set; }
        public long TamanhoBytes { get; set; }
        public DateTime DataAdicao { get; set; } = DateTime.UtcNow;
    }
}
