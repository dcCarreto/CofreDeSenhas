namespace GerenciadorDeSenhas.Modelos
{
    public class SenhaExportada
    {
        public string NomeServico { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? Url { get; set; }
        public Categoria Categoria { get; set; }
        public List<string> Etiquetas { get; set; } = new();
        public string? Notas { get; set; }
        public string? TotpSegredo { get; set; }
        public List<HistoricoSenhaExportada> Historico { get; set; } = new();
        public List<CodigoRecuperacaoExportado> CodigosRecuperacao { get; set; } = new();
        public bool Favorito { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }

    public class HistoricoSenhaExportada
    {
        public string Senha { get; set; } = string.Empty;
        public DateTime DataAlteracao { get; set; }
    }

    public class CodigoRecuperacaoExportado
    {
        public string Codigo { get; set; } = string.Empty;
        public bool Usado { get; set; }
    }
}
