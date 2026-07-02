namespace GerenciadorDeSenhas.Modelos
{
    public enum TipoBanco
    {
        SQLite,
        PostgreSQL,
        MySQL,
        SqlServer
    }

    public sealed class ProvedorBanco
    {
        public required TipoBanco Tipo { get; init; }
        public required string Rotulo { get; init; }
        public required string Distintivo { get; init; }
        public required string Cor { get; init; }

        public bool UsaArquivo { get; init; }
        public int PortaPadrao { get; init; }

        public static readonly IReadOnlyList<ProvedorBanco> Todos = new[]
        {
            new ProvedorBanco { Tipo = TipoBanco.SQLite, Rotulo = "SQLite", Distintivo = "SQL", Cor = "#0F80CC", UsaArquivo = true },
            new ProvedorBanco { Tipo = TipoBanco.PostgreSQL, Rotulo = "PostgreSQL", Distintivo = "Pg", Cor = "#336791", PortaPadrao = 5432 },
            new ProvedorBanco { Tipo = TipoBanco.MySQL, Rotulo = "MySQL / MariaDB", Distintivo = "My", Cor = "#00758F", PortaPadrao = 3306 },
            new ProvedorBanco { Tipo = TipoBanco.SqlServer, Rotulo = "SQL Server", Distintivo = "MS", Cor = "#A91D22", PortaPadrao = 1433 },
        };

        public static ProvedorBanco De(TipoBanco tipo) => Todos.First(p => p.Tipo == tipo);
    }
}
