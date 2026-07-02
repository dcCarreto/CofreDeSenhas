namespace GerenciadorDeSenhas.Modelos
{
    public sealed class ConexaoBanco
    {
        public TipoBanco Tipo { get; set; }

        public string? Host { get; set; }
        public int Porta { get; set; }

        public string? Banco { get; set; }

        public string? Usuario { get; set; }

        public string? SenhaServidor { get; set; }

        public string Descricao => Tipo == TipoBanco.SQLite
            ? $"SQLite — {Path.GetFileName(Banco)}"
            : $"{ProvedorBanco.De(Tipo).Rotulo} — {Banco}";
    }
}
