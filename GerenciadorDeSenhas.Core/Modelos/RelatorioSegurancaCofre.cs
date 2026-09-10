namespace GerenciadorDeSenhas.Modelos
{
    public sealed class RelatorioSegurancaCofre
    {
        public required int TotalSenhas { get; init; }
        public required int Fracas { get; init; }
        public required int Repetidas { get; init; }
        public required int Antigas { get; init; }
        public required int Comprometidas { get; init; }
        public required int SemTotp { get; init; }
        public required int SemUrl { get; init; }
        public required int SemCategoria { get; init; }
        public required int Pontuacao { get; init; }
        public bool CertificadoBancoNaoExigido { get; init; }

        public bool SemProblemas =>
            Fracas == 0 && Repetidas == 0 && Antigas == 0 && Comprometidas == 0 &&
            SemTotp == 0 && SemUrl == 0 && SemCategoria == 0 && !CertificadoBancoNaoExigido;

        public int Contagem(CategoriaRelatorioSeguranca categoria) => categoria switch
        {
            CategoriaRelatorioSeguranca.Fraca => Fracas,
            CategoriaRelatorioSeguranca.Repetida => Repetidas,
            CategoriaRelatorioSeguranca.Antiga => Antigas,
            CategoriaRelatorioSeguranca.Comprometida => Comprometidas,
            CategoriaRelatorioSeguranca.SemTotp => SemTotp,
            CategoriaRelatorioSeguranca.SemUrl => SemUrl,
            CategoriaRelatorioSeguranca.SemCategoria => SemCategoria,
            _ => 0
        };
    }
}
