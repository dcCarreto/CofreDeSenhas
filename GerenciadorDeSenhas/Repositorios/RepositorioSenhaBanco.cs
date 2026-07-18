using System.Data.Common;
using System.Text.Json;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace GerenciadorDeSenhas.Repositorios
{
    public class RepositorioSenhaBanco : IRepositorioSenha
    {
        private const string ColunasInsert = "usuario, senha, dominio, descricao, totp, etiquetas, codigos_recuperacao, excluido, data_criacao, data_atualizacao";
        private const string ParametrosInsert = "@usuario, @senha, @dominio, @descricao, @totp, @etiquetas, @codigos_recuperacao, @excluido, @data_criacao, @data_atualizacao";

        private readonly ConexaoBanco _cfg;
        private readonly ServicoBancoDados _bd = new();
        private readonly string _tabela = ServicoBancoDados.NomeTabela;

        private readonly Dictionary<Guid, long> _mapa = new();
        private List<Senha> _senhas = new();
        private bool _carregado;

        public RepositorioSenhaBanco(ConexaoBanco cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        private async Task<DbConnection> AbrirConexaoAsync()
        {
            var con = _bd.CriarConexao(_cfg);
            await con.OpenAsync();
            return con;
        }

        private async Task CarregarSeNecessarioAsync()
        {
            if (_carregado) return;

            _senhas = new List<Senha>();
            _mapa.Clear();

            await using var con = await AbrirConexaoAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"SELECT id, usuario, senha, dominio, descricao, totp, etiquetas, codigos_recuperacao, excluido, data_exclusao, data_criacao, data_atualizacao, data_ultima_copia_senha, data_ultima_copia_usuario, data_ultima_copia_totp FROM {_tabela}";

            await using var leitor = await cmd.ExecuteReaderAsync();
            while (await leitor.ReadAsync())
            {
                var senha = new Senha
                {
                    Id = Guid.NewGuid(),
                    NomeServico = leitor["dominio"] is string dominio ? dominio : "",
                    Usuario = (string)leitor["usuario"],
                    SenhaHash = (string)leitor["senha"],
                    Notas = leitor[ServicoBancoDados.ColunaDescricao] is string descricao ? descricao : null,
                    TotpSegredo = leitor[ServicoBancoDados.ColunaTotp] is string totp ? totp : null,
                    Etiquetas = DesserializarEtiquetas(leitor[ServicoBancoDados.ColunaEtiquetas]),
                    CodigosRecuperacao = DesserializarCodigosRecuperacao(leitor[ServicoBancoDados.ColunaCodigosRecuperacao]),
                    Categoria = Categoria.Other,
                    NaLixeira = Convert.ToBoolean(leitor["excluido"]),
                    DataExclusao = DesserializarData(leitor[ServicoBancoDados.ColunaDataExclusao]),
                    DataCriacao = DesserializarData(leitor[ServicoBancoDados.ColunaDataCriacao]) ?? DateTime.UtcNow,
                    DataAtualizacao = DesserializarData(leitor[ServicoBancoDados.ColunaDataAtualizacao]) ?? DateTime.UtcNow,
                    DataUltimaCopiaSenha = DesserializarData(leitor[ServicoBancoDados.ColunaDataUltimaCopiaSenha]),
                    DataUltimaCopiaUsuario = DesserializarData(leitor[ServicoBancoDados.ColunaDataUltimaCopiaUsuario]),
                    DataUltimaCopiaTotp = DesserializarData(leitor[ServicoBancoDados.ColunaDataUltimaCopiaTotp])
                };
                _senhas.Add(senha);
                _mapa[senha.Id] = Convert.ToInt64(leitor["id"]);
            }

            _carregado = true;
        }

        public async Task AdicionarAsync(Senha senha)
        {
            if (senha == null) throw new ArgumentNullException(nameof(senha));
            await CarregarSeNecessarioAsync();

            await using var con = await AbrirConexaoAsync();

            long id;
            if (_cfg.Tipo == TipoBanco.PostgreSQL)
            {
                await using var cmd = con.CreateCommand();
                cmd.CommandText = $"INSERT INTO {_tabela} ({ColunasInsert}) VALUES ({ParametrosInsert}) RETURNING id";
                PreencherCampos(cmd, senha);
                id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }
            else
            {
                await using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = $"INSERT INTO {_tabela} ({ColunasInsert}) VALUES ({ParametrosInsert})";
                    PreencherCampos(cmd, senha);
                    await cmd.ExecuteNonQueryAsync();
                }

                await using var cmdId = con.CreateCommand();
                cmdId.CommandText = ServicoBancoDados.ConsultaUltimoId(_cfg.Tipo);
                id = Convert.ToInt64(await cmdId.ExecuteScalarAsync());
            }

            _senhas.Add(senha);
            _mapa[senha.Id] = id;
        }

        public async Task AtualizarAsync(Senha senha)
        {
            if (senha == null) throw new ArgumentNullException(nameof(senha));
            await CarregarSeNecessarioAsync();

            if (!_mapa.TryGetValue(senha.Id, out var id))
                throw new InvalidOperationException($"Senha com ID {senha.Id} não encontrada");

            await using var con = await AbrirConexaoAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE {_tabela} SET usuario = @usuario, senha = @senha, dominio = @dominio, descricao = @descricao, totp = @totp, etiquetas = @etiquetas, codigos_recuperacao = @codigos_recuperacao, data_atualizacao = @data_atualizacao WHERE id = @id";
            PreencherCampos(cmd, senha);
            Parametro(cmd, "@id", id);
            await cmd.ExecuteNonQueryAsync();

            var existente = _senhas.FirstOrDefault(s => s.Id == senha.Id);
            if (existente != null)
            {
                existente.NomeServico = senha.NomeServico;
                existente.Usuario = senha.Usuario;
                existente.SenhaHash = senha.SenhaHash;
                existente.Notas = senha.Notas;
                existente.TotpSegredo = senha.TotpSegredo;
                existente.Etiquetas = senha.Etiquetas;
                existente.CodigosRecuperacao = senha.CodigosRecuperacao;
                existente.DataAtualizacao = senha.DataAtualizacao;
            }
        }

        public async Task RegistrarCopiaAsync(Guid id, TipoCampoCopiado campo)
        {
            await CarregarSeNecessarioAsync();

            if (!_mapa.TryGetValue(id, out var idBanco))
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            var coluna = campo switch
            {
                TipoCampoCopiado.Senha => ServicoBancoDados.ColunaDataUltimaCopiaSenha,
                TipoCampoCopiado.Usuario => ServicoBancoDados.ColunaDataUltimaCopiaUsuario,
                TipoCampoCopiado.Totp => ServicoBancoDados.ColunaDataUltimaCopiaTotp,
                _ => throw new ArgumentOutOfRangeException(nameof(campo))
            };
            var agora = DateTime.UtcNow;

            await using var con = await AbrirConexaoAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE {_tabela} SET {coluna} = @valor WHERE id = @id";
            Parametro(cmd, "@valor", SerializarData(agora));
            Parametro(cmd, "@id", idBanco);
            await cmd.ExecuteNonQueryAsync();

            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha != null)
            {
                switch (campo)
                {
                    case TipoCampoCopiado.Senha: senha.DataUltimaCopiaSenha = agora; break;
                    case TipoCampoCopiado.Usuario: senha.DataUltimaCopiaUsuario = agora; break;
                    case TipoCampoCopiado.Totp: senha.DataUltimaCopiaTotp = agora; break;
                }
            }
        }

        public async Task RemoverAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();

            if (!_mapa.TryGetValue(id, out var idBanco))
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            var agora = DateTime.UtcNow;

            await using var con = await AbrirConexaoAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE {_tabela} SET excluido = @excluido, data_exclusao = @data_exclusao WHERE id = @id";
            Parametro(cmd, "@excluido", true);
            Parametro(cmd, "@data_exclusao", SerializarData(agora));
            Parametro(cmd, "@id", idBanco);
            await cmd.ExecuteNonQueryAsync();

            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha != null)
            {
                senha.NaLixeira = true;
                senha.DataExclusao = agora;
            }
        }

        public async Task<Senha?> ObterPorIdAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();
            return _senhas.FirstOrDefault(s => s.Id == id);
        }

        public async Task<List<Senha>> ListarTodosAsync()
        {
            await CarregarSeNecessarioAsync();
            return _senhas.Where(s => !s.NaLixeira).ToList();
        }

        public async Task<List<Senha>> ListarLixeiraAsync()
        {
            await CarregarSeNecessarioAsync();
            return _senhas.Where(s => s.NaLixeira).ToList();
        }

        public async Task RestaurarAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();

            if (!_mapa.TryGetValue(id, out var idBanco))
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            await using var con = await AbrirConexaoAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE {_tabela} SET excluido = @excluido, data_exclusao = @data_exclusao WHERE id = @id";
            Parametro(cmd, "@excluido", false);
            Parametro(cmd, "@data_exclusao", null);
            Parametro(cmd, "@id", idBanco);
            await cmd.ExecuteNonQueryAsync();

            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha != null)
            {
                senha.NaLixeira = false;
                senha.DataExclusao = null;
            }
        }

        public async Task RemoverDefinitivamenteAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();

            if (!_mapa.TryGetValue(id, out var idBanco))
                return;

            await using var con = await AbrirConexaoAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"DELETE FROM {_tabela} WHERE id = @id";
            Parametro(cmd, "@id", idBanco);
            await cmd.ExecuteNonQueryAsync();

            _senhas.RemoveAll(s => s.Id == id);
            _mapa.Remove(id);
        }

        public async Task EsvaziarLixeiraAsync()
        {
            await CarregarSeNecessarioAsync();

            var idsLixeira = _senhas.Where(s => s.NaLixeira).Select(s => s.Id).ToList();
            foreach (var id in idsLixeira)
                await RemoverDefinitivamenteAsync(id);
        }

        public Task SalvarAsync() => Task.CompletedTask;

        public async Task GravarPorChaveAsync(Senha senha)
        {
            await using var con = await AbrirConexaoAsync();
            await GravarAsync(con, null, senha);
        }

        public async Task GravarVariasPorChaveAsync(IEnumerable<Senha> senhas)
        {
            await using var con = await AbrirConexaoAsync();
            await using var tx = await con.BeginTransactionAsync();
            foreach (var senha in senhas)
                await GravarAsync(con, tx, senha);
            await tx.CommitAsync();
        }

        public async Task ExcluirPorChaveAsync(string dominio, string usuario)
        {
            await using var con = await AbrirConexaoAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE {_tabela} SET excluido = @excluido WHERE dominio = @dominio AND usuario = @usuario";
            Parametro(cmd, "@excluido", true);
            Parametro(cmd, "@dominio", dominio);
            Parametro(cmd, "@usuario", usuario);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ExcluirDefinitivamentePorChaveAsync(string dominio, string usuario)
        {
            await using var con = await AbrirConexaoAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"DELETE FROM {_tabela} WHERE dominio = @dominio AND usuario = @usuario";
            Parametro(cmd, "@dominio", dominio);
            Parametro(cmd, "@usuario", usuario);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task GravarAsync(DbConnection con, DbTransaction? tx, Senha senha)
        {
            long? id = null;
            await using (var busca = con.CreateCommand())
            {
                busca.Transaction = tx;
                busca.CommandText = $"SELECT id FROM {_tabela} WHERE dominio = @dominio AND usuario = @usuario";
                Parametro(busca, "@dominio", senha.NomeServico);
                Parametro(busca, "@usuario", senha.Usuario);
                var r = await busca.ExecuteScalarAsync();
                if (r != null && r != DBNull.Value) id = Convert.ToInt64(r);
            }

            await using var cmd = con.CreateCommand();
            cmd.Transaction = tx;
            if (id.HasValue)
            {
                cmd.CommandText = $"UPDATE {_tabela} SET senha = @senha, descricao = @descricao, totp = @totp, etiquetas = @etiquetas, codigos_recuperacao = @codigos_recuperacao, excluido = @excluido WHERE id = @id";
                Parametro(cmd, "@senha", senha.SenhaHash);
                Parametro(cmd, "@descricao", senha.Notas);
                Parametro(cmd, "@totp", senha.TotpSegredo);
                Parametro(cmd, "@etiquetas", SerializarEtiquetas(senha.Etiquetas));
                Parametro(cmd, "@codigos_recuperacao", SerializarCodigosRecuperacao(senha.CodigosRecuperacao));
                Parametro(cmd, "@excluido", false);
                Parametro(cmd, "@id", id.Value);
            }
            else
            {
                cmd.CommandText = $"INSERT INTO {_tabela} (usuario, senha, dominio, descricao, totp, etiquetas, codigos_recuperacao, excluido) " +
                                  "VALUES (@usuario, @senha, @dominio, @descricao, @totp, @etiquetas, @codigos_recuperacao, @excluido)";
                PreencherCampos(cmd, senha);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        private void PreencherCampos(DbCommand cmd, Senha senha)
        {
            Parametro(cmd, "@usuario", senha.Usuario);
            Parametro(cmd, "@senha", senha.SenhaHash);
            Parametro(cmd, "@dominio", senha.NomeServico);
            Parametro(cmd, "@descricao", senha.Notas);
            Parametro(cmd, "@totp", senha.TotpSegredo);
            Parametro(cmd, "@etiquetas", SerializarEtiquetas(senha.Etiquetas));
            Parametro(cmd, "@codigos_recuperacao", SerializarCodigosRecuperacao(senha.CodigosRecuperacao));
            Parametro(cmd, "@excluido", false);
            Parametro(cmd, "@data_criacao", SerializarData(senha.DataCriacao));
            Parametro(cmd, "@data_atualizacao", SerializarData(senha.DataAtualizacao));
        }

        private static string? SerializarEtiquetas(List<string> etiquetas) =>
            etiquetas.Count == 0 ? null : JsonSerializer.Serialize(etiquetas);

        private static string? SerializarCodigosRecuperacao(List<CodigoRecuperacao> codigos) =>
            codigos.Count == 0 ? null : JsonSerializer.Serialize(codigos);

        private static List<CodigoRecuperacao> DesserializarCodigosRecuperacao(object? valor)
        {
            if (valor is not string texto || string.IsNullOrWhiteSpace(texto))
                return new List<CodigoRecuperacao>();

            try
            {
                return JsonSerializer.Deserialize<List<CodigoRecuperacao>>(texto) ?? new List<CodigoRecuperacao>();
            }
            catch
            {
                return new List<CodigoRecuperacao>();
            }
        }

        private static string? SerializarData(DateTime? data) =>
            data?.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        private static DateTime? DesserializarData(object? valor)
        {
            if (valor is not string texto || string.IsNullOrWhiteSpace(texto))
                return null;

            return DateTime.TryParse(texto, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var data) ? data : null;
        }

        private static List<string> DesserializarEtiquetas(object? valor)
        {
            if (valor is not string texto || string.IsNullOrWhiteSpace(texto))
                return new List<string>();

            try
            {
                return Etiquetas.Normalizar(JsonSerializer.Deserialize<List<string>>(texto));
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void Parametro(DbCommand cmd, string nome, object? valor)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = nome;
            p.Value = valor ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
