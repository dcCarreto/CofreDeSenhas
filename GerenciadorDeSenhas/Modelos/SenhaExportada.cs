namespace GerenciadorDeSenhas.Modelos
{
    public class SenhaExportada
    {
        public string NomeServico { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? Url { get; set; }
        public Categoria Categoria { get; set; }
        public string? Notas { get; set; }
        public string? TotpSegredo { get; set; }
        public bool Favorito { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }
}
