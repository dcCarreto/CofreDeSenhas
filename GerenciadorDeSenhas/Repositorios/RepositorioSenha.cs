using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace GerenciadorDeSenhas.Repositorios
{
    public class RepositorioSenha : IRepositorioSenha
    {
        private readonly IPersistenciaLocal _persistencia;
        private readonly byte[] _chave;
        private List<Senha> _senhas = new();
        private bool _carregado = false;

        public RepositorioSenha(IPersistenciaLocal persistencia, byte[] chave)
        {
            _persistencia = persistencia ?? throw new ArgumentNullException(nameof(persistencia));
            _chave = chave ?? throw new ArgumentNullException(nameof(chave));
        }

        private async Task CarregarSeNecessarioAsync()
        {
            if (!_carregado)
            {
                _senhas = await _persistencia.CarregarSenhasAsync(_chave);
                _carregado = true;
            }
        }

        public async Task AdicionarAsync(Senha senha)
        {
            if (senha == null)
                throw new ArgumentNullException(nameof(senha));

            await CarregarSeNecessarioAsync();

            if (_senhas.Any(s => s.Id == senha.Id))
                throw new InvalidOperationException($"Senha com ID {senha.Id} já existe");

            _senhas.Add(senha);
        }

        public async Task AtualizarAsync(Senha senha)
        {
            if (senha == null)
                throw new ArgumentNullException(nameof(senha));

            await CarregarSeNecessarioAsync();

            var existente = _senhas.FirstOrDefault(s => s.Id == senha.Id);
            if (existente == null)
                throw new InvalidOperationException($"Senha com ID {senha.Id} não encontrada");

            existente.NomeServico = senha.NomeServico;
            existente.Usuario = senha.Usuario;
            existente.SenhaHash = senha.SenhaHash;
            existente.Url = senha.Url;
            existente.Categoria = senha.Categoria;
            existente.Etiquetas = senha.Etiquetas;
            existente.Notas = senha.Notas;
            existente.TotpSegredo = senha.TotpSegredo;
            existente.Favorito = senha.Favorito;
            existente.Fixado = senha.Fixado;
            existente.DataAtualizacao = DateTime.UtcNow;
        }

        public async Task RegistrarCopiaAsync(Guid id, TipoCampoCopiado campo)
        {
            await CarregarSeNecessarioAsync();

            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            var agora = DateTime.UtcNow;
            switch (campo)
            {
                case TipoCampoCopiado.Senha: senha.DataUltimaCopiaSenha = agora; break;
                case TipoCampoCopiado.Usuario: senha.DataUltimaCopiaUsuario = agora; break;
                case TipoCampoCopiado.Totp: senha.DataUltimaCopiaTotp = agora; break;
            }
        }

        public async Task RemoverAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();

            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            senha.NaLixeira = true;
            senha.DataExclusao = DateTime.UtcNow;
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

            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            senha.NaLixeira = false;
            senha.DataExclusao = null;
        }

        public async Task RemoverDefinitivamenteAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();
            _senhas.RemoveAll(s => s.Id == id);
        }

        public async Task EsvaziarLixeiraAsync()
        {
            await CarregarSeNecessarioAsync();
            _senhas.RemoveAll(s => s.NaLixeira);
        }

        public async Task SalvarAsync()
        {
            await CarregarSeNecessarioAsync();
            await _persistencia.SalvarSenhasAsync(_senhas, _chave);
        }
    }
}
