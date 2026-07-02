using System.Data.Common;
using GerenciadorDeSenhas.Modelos;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoBancoDados
    {
        public const string NomeTabela = "CofreDeSenhas";

        public DbConnection CriarConexao(ConexaoBanco cfg) => cfg.Tipo switch
        {
            TipoBanco.SQLite => new SqliteConnection(MontarStringConexao(cfg)),
            TipoBanco.PostgreSQL => new NpgsqlConnection(MontarStringConexao(cfg)),
            TipoBanco.MySQL => new MySqlConnection(MontarStringConexao(cfg)),
            TipoBanco.SqlServer => new SqlConnection(MontarStringConexao(cfg)),
            _ => throw new NotSupportedException($"Banco não suportado: {cfg.Tipo}")
        };

        public string MontarStringConexao(ConexaoBanco cfg) => cfg.Tipo switch
        {
            TipoBanco.SQLite => new SqliteConnectionStringBuilder
            {
                DataSource = cfg.Banco
            }.ConnectionString,

            TipoBanco.PostgreSQL => new NpgsqlConnectionStringBuilder
            {
                Host = cfg.Host,
                Port = cfg.Porta,
                Database = cfg.Banco,
                Username = cfg.Usuario,
                Password = cfg.SenhaServidor
            }.ConnectionString,

            TipoBanco.MySQL => new MySqlConnectionStringBuilder
            {
                Server = cfg.Host,
                Port = (uint)cfg.Porta,
                Database = cfg.Banco,
                UserID = cfg.Usuario,
                Password = cfg.SenhaServidor
            }.ConnectionString,

            TipoBanco.SqlServer => new SqlConnectionStringBuilder
            {
                DataSource = cfg.Porta > 0 ? $"{cfg.Host},{cfg.Porta}" : cfg.Host,
                InitialCatalog = cfg.Banco,
                UserID = cfg.Usuario,
                Password = cfg.SenhaServidor,
                TrustServerCertificate = true
            }.ConnectionString,

            _ => throw new NotSupportedException($"Banco não suportado: {cfg.Tipo}")
        };

        public async Task TestarConexaoAsync(ConexaoBanco cfg)
        {
            await using var con = CriarConexao(cfg);
            await con.OpenAsync();
        }

        public async Task<bool> TabelaExisteAsync(ConexaoBanco cfg)
        {
            await using var con = CriarConexao(cfg);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = ConsultaExistencia(cfg.Tipo);

            var resultado = await cmd.ExecuteScalarAsync();
            return resultado != null && resultado != DBNull.Value && Convert.ToInt64(resultado) > 0;
        }

        public async Task CriarTabelaAsync(ConexaoBanco cfg)
        {
            await using var con = CriarConexao(cfg);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = Ddl(cfg.Tipo);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task GarantirColunasAsync(ConexaoBanco cfg)
        {
            await using var con = CriarConexao(cfg);
            await con.OpenAsync();

            await using (var verifica = con.CreateCommand())
            {
                verifica.CommandText = ConsultaColunaDescricao(cfg.Tipo);
                var resultado = await verifica.ExecuteScalarAsync();
                if (resultado != null && resultado != DBNull.Value && Convert.ToInt64(resultado) > 0)
                    return;
            }

            await using var alterar = con.CreateCommand();
            alterar.CommandText = DdlAdicionarDescricao(cfg.Tipo);
            await alterar.ExecuteNonQueryAsync();
        }

        public static string ConsultaUltimoId(TipoBanco tipo) => tipo switch
        {
            TipoBanco.SQLite => "SELECT last_insert_rowid()",
            TipoBanco.MySQL => "SELECT LAST_INSERT_ID()",
            TipoBanco.SqlServer => "SELECT CAST(SCOPE_IDENTITY() AS INT)",
            _ => throw new NotSupportedException($"Sem consulta de último id para {tipo}")
        };

        private static string ConsultaExistencia(TipoBanco tipo) => tipo switch
        {
            TipoBanco.SQLite =>
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{NomeTabela}'",

            TipoBanco.PostgreSQL =>
                $"SELECT COUNT(*) FROM information_schema.tables WHERE lower(table_name) = lower('{NomeTabela}')",

            TipoBanco.MySQL =>
                $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{NomeTabela}'",

            TipoBanco.SqlServer =>
                $"SELECT COUNT(*) FROM sys.tables WHERE name = '{NomeTabela}'",

            _ => throw new NotSupportedException($"Banco não suportado: {tipo}")
        };

        private static string ConsultaColunaDescricao(TipoBanco tipo) => tipo switch
        {
            TipoBanco.SQLite =>
                $"SELECT COUNT(*) FROM pragma_table_info('{NomeTabela}') WHERE name = 'descricao'",

            TipoBanco.PostgreSQL =>
                $"SELECT COUNT(*) FROM information_schema.columns WHERE lower(table_name) = lower('{NomeTabela}') AND lower(column_name) = 'descricao'",

            TipoBanco.MySQL =>
                $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{NomeTabela}' AND column_name = 'descricao'",

            TipoBanco.SqlServer =>
                $"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('{NomeTabela}') AND name = 'descricao'",

            _ => throw new NotSupportedException($"Banco não suportado: {tipo}")
        };

        private static string DdlAdicionarDescricao(TipoBanco tipo) => tipo switch
        {
            TipoBanco.SQLite => $"ALTER TABLE {NomeTabela} ADD COLUMN descricao TEXT",
            TipoBanco.PostgreSQL => $"ALTER TABLE {NomeTabela} ADD COLUMN descricao TEXT",
            TipoBanco.MySQL => $"ALTER TABLE {NomeTabela} ADD COLUMN descricao TEXT",
            TipoBanco.SqlServer => $"ALTER TABLE {NomeTabela} ADD descricao NVARCHAR(MAX)",
            _ => throw new NotSupportedException($"Banco não suportado: {tipo}")
        };

        private static string Ddl(TipoBanco tipo) => tipo switch
        {
            TipoBanco.SQLite =>
                $@"CREATE TABLE {NomeTabela} (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    usuario TEXT NOT NULL,
                    senha TEXT NOT NULL,
                    dominio TEXT,
                    descricao TEXT,
                    excluido INTEGER NOT NULL DEFAULT 0
                )",

            TipoBanco.PostgreSQL =>
                $@"CREATE TABLE {NomeTabela} (
                    id SERIAL PRIMARY KEY,
                    usuario VARCHAR(255) NOT NULL,
                    senha TEXT NOT NULL,
                    dominio VARCHAR(255),
                    descricao TEXT,
                    excluido BOOLEAN NOT NULL DEFAULT FALSE
                )",

            TipoBanco.MySQL =>
                $@"CREATE TABLE {NomeTabela} (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    usuario VARCHAR(255) NOT NULL,
                    senha TEXT NOT NULL,
                    dominio VARCHAR(255),
                    descricao TEXT,
                    excluido TINYINT(1) NOT NULL DEFAULT 0
                )",

            TipoBanco.SqlServer =>
                $@"CREATE TABLE {NomeTabela} (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    usuario NVARCHAR(255) NOT NULL,
                    senha NVARCHAR(MAX) NOT NULL,
                    dominio NVARCHAR(255),
                    descricao NVARCHAR(MAX),
                    excluido BIT NOT NULL DEFAULT 0
                )",

            _ => throw new NotSupportedException($"Banco não suportado: {tipo}")
        };
    }
}
