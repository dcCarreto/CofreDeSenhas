using System.Data.Common;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace GerenciadorDeSenhas.Repositorios
{
    public class RepositorioSenhaBanco : IRepositorioSenha
    {
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

        private async Task CarregarSeNecessarioAsync()
        {
            if (_carregado) return;

            _senhas = new List<Senha>();
            _mapa.Clear();

            await using var con = _bd.CriarConexao(_cfg);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"SELECT id, usuario, senha, dominio, descricao, totp FROM {_tabela} WHERE excluido = @excluido";
            Parametro(cmd, "@excluido", false);

            await using var leitor = await cmd.ExecuteReaderAsync();
            while (await leitor.ReadAsync())
            {
                var senha = new Senha
                {
                    Id = Guid.NewGuid(),
                    NomeServico = leitor["dominio"] is string dominio ? dominio : "",
                    Usuario = (string)leitor["usuario"],
                    SenhaHash = (string)leitor["senha"],
                    Notas = leitor["descricao"] is string descricao ? descricao : null,
                    TotpSegredo = leitor["totp"] is string totp ? totp : null,
                    Categoria = Categoria.Other
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

            await using var con = _bd.CriarConexao(_cfg);
            await con.OpenAsync();

            long id;
            if (_cfg.Tipo == TipoBanco.PostgreSQL)
            {
                await using var cmd = con.CreateCommand();
                cmd.CommandText = $"INSERT INTO {_tabela} (usuario, senha, dominio, descricao, totp, excluido) " +
                                  "VALUES (@usuario, @senha, @dominio, @descricao, @totp, @excluido) RETURNING id";
                PreencherCampos(cmd, senha);
                id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }
            else
            {
                await using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = $"INSERT INTO {_tabela} (usuario, senha, dominio, descricao, totp, excluido) " +
                                      "VALUES (@usuario, @senha, @dominio, @descricao, @totp, @excluido)";
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

            await using var con = _bd.CriarConexao(_cfg);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE {_tabela} SET usuario = @usuario, senha = @senha, dominio = @dominio, descricao = @descricao, totp = @totp WHERE id = @id";
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
            }
        }

        public async Task RemoverAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();

            if (!_mapa.TryGetValue(id, out var idBanco))
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            await using var con = _bd.CriarConexao(_cfg);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE {_tabela} SET excluido = @excluido WHERE id = @id";
            Parametro(cmd, "@excluido", true);
            Parametro(cmd, "@id", idBanco);
            await cmd.ExecuteNonQueryAsync();

            _senhas.RemoveAll(s => s.Id == id);
            _mapa.Remove(id);
        }

        public async Task<Senha?> ObterPorIdAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();
            return _senhas.FirstOrDefault(s => s.Id == id);
        }

        public async Task<List<Senha>> ListarTodosAsync()
        {
            await CarregarSeNecessarioAsync();
            return _senhas.ToList();
        }

        public async Task<List<Senha>> BuscarPorCategoriaAsync(Categoria categoria)
        {
            await CarregarSeNecessarioAsync();
            return _senhas.Where(s => s.Categoria == categoria).ToList();
        }

        public async Task<List<Senha>> BuscarPorServicoAsync(string nomeServico)
        {
            if (string.IsNullOrWhiteSpace(nomeServico))
                throw new ArgumentException("Nome do serviço não pode ser vazio");

            await CarregarSeNecessarioAsync();
            return _senhas.Where(s =>
                s.NomeServico.Contains(nomeServico, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<Senha>> ListarFavoritosAsync()
        {
            await CarregarSeNecessarioAsync();
            return _senhas.Where(s => s.Favorito).ToList();
        }

        public async Task<int> ContarAsync()
        {
            await CarregarSeNecessarioAsync();
            return _senhas.Count;
        }

        public Task SalvarAsync() => Task.CompletedTask;

        public async Task GravarPorChaveAsync(Senha senha)
        {
            await using var con = _bd.CriarConexao(_cfg);
            await con.OpenAsync();
            await GravarAsync(con, null, senha);
        }

        public async Task GravarVariasPorChaveAsync(IEnumerable<Senha> senhas)
        {
            await using var con = _bd.CriarConexao(_cfg);
            await con.OpenAsync();
            await using var tx = await con.BeginTransactionAsync();
            foreach (var senha in senhas)
                await GravarAsync(con, tx, senha);
            await tx.CommitAsync();
        }

        public async Task ExcluirPorChaveAsync(string dominio, string usuario)
        {
            await using var con = _bd.CriarConexao(_cfg);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE {_tabela} SET excluido = @excluido WHERE dominio = @dominio AND usuario = @usuario";
            Parametro(cmd, "@excluido", true);
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
                cmd.CommandText = $"UPDATE {_tabela} SET senha = @senha, descricao = @descricao, totp = @totp, excluido = @excluido WHERE id = @id";
                Parametro(cmd, "@senha", senha.SenhaHash);
                Parametro(cmd, "@descricao", senha.Notas);
                Parametro(cmd, "@totp", senha.TotpSegredo);
                Parametro(cmd, "@excluido", false);
                Parametro(cmd, "@id", id.Value);
            }
            else
            {
                cmd.CommandText = $"INSERT INTO {_tabela} (usuario, senha, dominio, descricao, totp, excluido) " +
                                  "VALUES (@usuario, @senha, @dominio, @descricao, @totp, @excluido)";
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
            Parametro(cmd, "@excluido", false);
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
