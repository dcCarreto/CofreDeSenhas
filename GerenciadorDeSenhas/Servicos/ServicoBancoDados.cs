using System.Data.Common;
using GerenciadorDeSenhas.Excecoes;
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
        public const string NomeTabelaAuth = "CofreDeSenhasAuth";

        public const string ColunaDescricao = "descricao";
        public const string ColunaTotp = "totp";
        public const string ColunaEtiquetas = "etiquetas";
        public const string ColunaDataExclusao = "data_exclusao";
        public const string ColunaCodigosRecuperacao = "codigos_recuperacao";
        public const string ColunaDataCriacao = "data_criacao";
        public const string ColunaDataAtualizacao = "data_atualizacao";
        public const string ColunaDataUltimaCopiaSenha = "data_ultima_copia_senha";
        public const string ColunaDataUltimaCopiaUsuario = "data_ultima_copia_usuario";
        public const string ColunaDataUltimaCopiaTotp = "data_ultima_copia_totp";
        public const string ColunaUrl = "url";
        public const string ColunaCategoria = "categoria";
        public const string ColunaTipo = "tipo";
        public const string ColunaCamposExtras = "campos_extras";
        public const string ColunaHistorico = "historico";
        public const string ColunaFavorito = "favorito";
        public const string ColunaFixado = "fixado";
        public const string ColunaGuidId = "guid_id";
        public const string ColunaHmac = "hmac";

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
                Password = cfg.SenhaServidor,
                SslMode = cfg.ExigirCertificadoValido ? SslMode.VerifyFull : SslMode.Prefer
            }.ConnectionString,

            TipoBanco.MySQL => new MySqlConnectionStringBuilder
            {
                Server = cfg.Host,
                Port = (uint)cfg.Porta,
                Database = cfg.Banco,
                UserID = cfg.Usuario,
                Password = cfg.SenhaServidor,
                SslMode = cfg.ExigirCertificadoValido ? MySqlSslMode.VerifyFull : MySqlSslMode.Preferred
            }.ConnectionString,

            TipoBanco.SqlServer => new SqlConnectionStringBuilder
            {
                DataSource = cfg.Porta > 0 ? $"{cfg.Host},{cfg.Porta}" : cfg.Host,
                InitialCatalog = cfg.Banco,
                UserID = cfg.Usuario,
                Password = cfg.SenhaServidor,
                Encrypt = true,
                TrustServerCertificate = !cfg.ExigirCertificadoValido
            }.ConnectionString,

            _ => throw new NotSupportedException($"Banco não suportado: {cfg.Tipo}")
        };

        private async Task<DbConnection> AbrirConexaoAsync(ConexaoBanco cfg)
        {
            try
            {
                var con = CriarConexao(cfg);
                await con.OpenAsync();
                return con;
            }
            catch (Exception ex)
            {
                throw new ErroLocalizavel("Db.Error.ConnectionFailed", ex);
            }
        }

        public async Task TestarConexaoAsync(ConexaoBanco cfg)
        {
            await using var con = await AbrirConexaoAsync(cfg);
        }

        public async Task<bool> TabelaExisteAsync(ConexaoBanco cfg)
        {
            await using var con = await AbrirConexaoAsync(cfg);

            await using var cmd = con.CreateCommand();
            cmd.CommandText = ConsultaExistencia(cfg.Tipo);

            var resultado = await cmd.ExecuteScalarAsync();
            return resultado != null && resultado != DBNull.Value && Convert.ToInt64(resultado) > 0;
        }

        public async Task CriarTabelaAsync(ConexaoBanco cfg)
        {
            await using var con = await AbrirConexaoAsync(cfg);

            await using var cmd = con.CreateCommand();
            cmd.CommandText = Ddl(cfg.Tipo);
            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new ErroLocalizavel("Db.Error.SchemaFailed", ex);
            }
        }

        public async Task<IReadOnlySet<long>> GarantirColunasAsync(ConexaoBanco cfg)
        {
            await using var con = await AbrirConexaoAsync(cfg);

            await GarantirColunaAsync(con, cfg.Tipo, ColunaDescricao);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaTotp);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaEtiquetas);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaDataExclusao);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaCodigosRecuperacao);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaDataCriacao);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaDataAtualizacao);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaDataUltimaCopiaSenha);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaDataUltimaCopiaUsuario);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaDataUltimaCopiaTotp);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaUrl);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaCategoria);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaTipo);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaCamposExtras);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaHistorico);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaFavorito);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaFixado);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaGuidId);
            await GarantirColunaAsync(con, cfg.Tipo, ColunaHmac);

            return await PreencherGuidsFaltantesAsync(con);
        }

        private async Task<IReadOnlySet<long>> PreencherGuidsFaltantesAsync(DbConnection con)
        {
            var pendentes = new List<long>();
            await using (var busca = con.CreateCommand())
            {
                busca.CommandText = $"SELECT id FROM {NomeTabela} WHERE {ColunaGuidId} IS NULL";
                await using var leitor = await busca.ExecuteReaderAsync();
                while (await leitor.ReadAsync())
                    pendentes.Add(Convert.ToInt64(leitor[0]));
            }

            foreach (var id in pendentes)
            {
                await using var cmd = con.CreateCommand();
                cmd.CommandText = $"UPDATE {NomeTabela} SET {ColunaGuidId} = @guid WHERE id = @id";

                var guidParam = cmd.CreateParameter();
                guidParam.ParameterName = "@guid";
                guidParam.Value = Guid.NewGuid().ToString();
                cmd.Parameters.Add(guidParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }

            return pendentes.ToHashSet();
        }

        public async Task<bool> TabelaAuthExisteAsync(ConexaoBanco cfg)
        {
            await using var con = await AbrirConexaoAsync(cfg);

            await using var cmd = con.CreateCommand();
            cmd.CommandText = ConsultaExistenciaTabela(cfg.Tipo, NomeTabelaAuth);

            var resultado = await cmd.ExecuteScalarAsync();
            return resultado != null && resultado != DBNull.Value && Convert.ToInt64(resultado) > 0;
        }

        public async Task CriarTabelaAuthAsync(ConexaoBanco cfg)
        {
            await using var con = await AbrirConexaoAsync(cfg);

            await using var cmd = con.CreateCommand();
            cmd.CommandText = DdlAuth(cfg.Tipo);
            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new ErroLocalizavel("Db.Error.SchemaFailed", ex);
            }
        }

        public async Task<AuthBanco?> LerAuthAsync(ConexaoBanco cfg)
        {
            await using var con = await AbrirConexaoAsync(cfg);

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"SELECT salt, verificador, kdf, custo, memoria_kb, paralelismo FROM {NomeTabelaAuth} WHERE id = 1";

            try
            {
                await using var leitor = await cmd.ExecuteReaderAsync();
                if (!await leitor.ReadAsync())
                    return null;

                return new AuthBanco(
                    Convert.FromBase64String((string)leitor[0]),
                    Convert.FromBase64String((string)leitor[1]),
                    Convert.ToByte(leitor[2]),
                    Convert.ToInt32(leitor[3]),
                    Convert.ToInt32(leitor[4]),
                    Convert.ToInt32(leitor[5]));
            }
            catch (DbException)
            {
                // Tabela ainda não existe neste banco.
                return null;
            }
        }

        public async Task PublicarAuthAsync(ConexaoBanco cfg, AuthBanco dados)
        {
            await using var con = await AbrirConexaoAsync(cfg);

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $@"INSERT INTO {NomeTabelaAuth} (id, salt, verificador, kdf, custo, memoria_kb, paralelismo)
                VALUES (1, @salt, @verificador, @kdf, @custo, @memoriaKb, @paralelismo)";

            AdicionarParametro(cmd, "@salt", Convert.ToBase64String(dados.Salt));
            AdicionarParametro(cmd, "@verificador", Convert.ToBase64String(dados.Verificador));
            AdicionarParametro(cmd, "@kdf", dados.Kdf);
            AdicionarParametro(cmd, "@custo", dados.Custo);
            AdicionarParametro(cmd, "@memoriaKb", dados.MemoriaKb);
            AdicionarParametro(cmd, "@paralelismo", dados.Paralelismo);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (DbException)
            {
                // Outro dispositivo publicou a linha id=1 entre a checagem de
                // existência da tabela e este INSERT — a linha já está lá, que é
                // exatamente o estado desejado.
            }
        }

        private static void AdicionarParametro(DbCommand cmd, string nome, object valor)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = nome;
            p.Value = valor;
            cmd.Parameters.Add(p);
        }

        private async Task GarantirColunaAsync(DbConnection con, TipoBanco tipo, string coluna)
        {
            if (await ColunaExisteAsync(con, tipo, coluna))
                return;

            await using var alterar = con.CreateCommand();
            alterar.CommandText = DdlAdicionarColuna(tipo, coluna);
            try
            {
                await alterar.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Outro cliente pode ter adicionado a mesma coluna entre a checagem
                // acima e este ALTER TABLE (dois clientes inicializando o schema ao
                // mesmo tempo); se a coluna já existe agora, o resultado é o mesmo
                // que se este ALTER tivesse funcionado — não é um erro de verdade.
                if (await ColunaExisteAsync(con, tipo, coluna))
                    return;

                throw new ErroLocalizavel("Db.Error.SchemaFailed", ex);
            }
        }

        private static async Task<bool> ColunaExisteAsync(DbConnection con, TipoBanco tipo, string coluna)
        {
            await using var verifica = con.CreateCommand();
            verifica.CommandText = ConsultaColunaExiste(tipo, coluna);
            var resultado = await verifica.ExecuteScalarAsync();
            return resultado != null && resultado != DBNull.Value && Convert.ToInt64(resultado) > 0;
        }

        // SqlServer não usa esta consulta: AdicionarAsync lê o id via OUTPUT INSERTED.id,
        // atômico com o próprio INSERT — SCOPE_IDENTITY() numa consulta separada chegou a
        // devolver DBNull sob concorrência real.
        public static string ConsultaUltimoId(TipoBanco tipo) => tipo switch
        {
            TipoBanco.SQLite => "SELECT last_insert_rowid()",
            TipoBanco.MySQL => "SELECT LAST_INSERT_ID()",
            _ => throw new NotSupportedException($"Sem consulta de último id para {tipo}")
        };

        private static string ConsultaExistencia(TipoBanco tipo) => ConsultaExistenciaTabela(tipo, NomeTabela);

        private static string ConsultaExistenciaTabela(TipoBanco tipo, string nomeTabela) => tipo switch
        {
            TipoBanco.SQLite =>
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{nomeTabela}'",

            TipoBanco.PostgreSQL =>
                $"SELECT COUNT(*) FROM information_schema.tables WHERE lower(table_name) = lower('{nomeTabela}')",

            TipoBanco.MySQL =>
                $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{nomeTabela}'",

            TipoBanco.SqlServer =>
                $"SELECT COUNT(*) FROM sys.tables WHERE name = '{nomeTabela}'",

            _ => throw new NotSupportedException($"Banco não suportado: {tipo}")
        };

        private static string ConsultaColunaExiste(TipoBanco tipo, string coluna) => tipo switch
        {
            TipoBanco.SQLite =>
                $"SELECT COUNT(*) FROM pragma_table_info('{NomeTabela}') WHERE name = '{coluna}'",

            TipoBanco.PostgreSQL =>
                $"SELECT COUNT(*) FROM information_schema.columns WHERE lower(table_name) = lower('{NomeTabela}') AND lower(column_name) = '{coluna}'",

            TipoBanco.MySQL =>
                $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{NomeTabela}' AND column_name = '{coluna}'",

            TipoBanco.SqlServer =>
                $"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('{NomeTabela}') AND name = '{coluna}'",

            _ => throw new NotSupportedException($"Banco não suportado: {tipo}")
        };

        private static string DdlAdicionarColuna(TipoBanco tipo, string coluna) => tipo switch
        {
            TipoBanco.SQLite => $"ALTER TABLE {NomeTabela} ADD COLUMN {coluna} TEXT",
            TipoBanco.PostgreSQL => $"ALTER TABLE {NomeTabela} ADD COLUMN {coluna} TEXT",
            TipoBanco.MySQL => $"ALTER TABLE {NomeTabela} ADD COLUMN {coluna} TEXT",
            TipoBanco.SqlServer => $"ALTER TABLE {NomeTabela} ADD {coluna} NVARCHAR(MAX)",
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
                    totp TEXT,
                    etiquetas TEXT,
                    codigos_recuperacao TEXT,
                    excluido INTEGER NOT NULL DEFAULT 0
                )",

            TipoBanco.PostgreSQL =>
                $@"CREATE TABLE {NomeTabela} (
                    id SERIAL PRIMARY KEY,
                    usuario VARCHAR(255) NOT NULL,
                    senha TEXT NOT NULL,
                    dominio VARCHAR(255),
                    descricao TEXT,
                    totp TEXT,
                    etiquetas TEXT,
                    codigos_recuperacao TEXT,
                    excluido BOOLEAN NOT NULL DEFAULT FALSE
                )",

            TipoBanco.MySQL =>
                $@"CREATE TABLE {NomeTabela} (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    usuario VARCHAR(255) NOT NULL,
                    senha TEXT NOT NULL,
                    dominio VARCHAR(255),
                    descricao TEXT,
                    totp TEXT,
                    etiquetas TEXT,
                    codigos_recuperacao TEXT,
                    excluido TINYINT(1) NOT NULL DEFAULT 0
                )",

            TipoBanco.SqlServer =>
                $@"CREATE TABLE {NomeTabela} (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    usuario NVARCHAR(255) NOT NULL,
                    senha NVARCHAR(MAX) NOT NULL,
                    dominio NVARCHAR(255),
                    descricao NVARCHAR(MAX),
                    totp NVARCHAR(MAX),
                    etiquetas NVARCHAR(MAX),
                    codigos_recuperacao NVARCHAR(MAX),
                    excluido BIT NOT NULL DEFAULT 0
                )",

            _ => throw new NotSupportedException($"Banco não suportado: {tipo}")
        };

        // Linha única (id sempre 1): metadados de derivação de chave (salt, KDF,
        // verificador) publicados por um dispositivo já autenticado, pra permitir
        // restaurar o cofre local a partir do banco caso o dispositivo o perca.
        // Sem autoincrement/serial — o id nunca é gerado, sempre inserido como 1.
        private static string DdlAuth(TipoBanco tipo) => tipo switch
        {
            TipoBanco.SQLite =>
                $@"CREATE TABLE {NomeTabelaAuth} (
                    id INTEGER PRIMARY KEY,
                    salt TEXT NOT NULL,
                    verificador TEXT NOT NULL,
                    kdf INTEGER NOT NULL,
                    custo INTEGER NOT NULL,
                    memoria_kb INTEGER NOT NULL,
                    paralelismo INTEGER NOT NULL
                )",

            TipoBanco.PostgreSQL =>
                $@"CREATE TABLE {NomeTabelaAuth} (
                    id INTEGER PRIMARY KEY,
                    salt TEXT NOT NULL,
                    verificador TEXT NOT NULL,
                    kdf INTEGER NOT NULL,
                    custo INTEGER NOT NULL,
                    memoria_kb INTEGER NOT NULL,
                    paralelismo INTEGER NOT NULL
                )",

            TipoBanco.MySQL =>
                $@"CREATE TABLE {NomeTabelaAuth} (
                    id INT PRIMARY KEY,
                    salt TEXT NOT NULL,
                    verificador TEXT NOT NULL,
                    kdf INTEGER NOT NULL,
                    custo INTEGER NOT NULL,
                    memoria_kb INTEGER NOT NULL,
                    paralelismo INTEGER NOT NULL
                )",

            TipoBanco.SqlServer =>
                $@"CREATE TABLE {NomeTabelaAuth} (
                    id INT PRIMARY KEY,
                    salt NVARCHAR(MAX) NOT NULL,
                    verificador NVARCHAR(MAX) NOT NULL,
                    kdf INTEGER NOT NULL,
                    custo INTEGER NOT NULL,
                    memoria_kb INTEGER NOT NULL,
                    paralelismo INTEGER NOT NULL
                )",

            _ => throw new NotSupportedException($"Banco não suportado: {tipo}")
        };
    }
}
