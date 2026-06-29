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
            existente.Notas = senha.Notas;
            existente.Favorito = senha.Favorito;
            existente.IV = senha.IV;
            existente.AuthTag = senha.AuthTag;
            existente.DataAtualizacao = DateTime.UtcNow;
        }

        public async Task RemoverAsync(Guid id)
        {
            await CarregarSeNecessarioAsync();

            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            _senhas.Remove(senha);
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
                s.NomeServico.Contains(nomeServico, StringComparison.OrdinalIgnoreCase))
                .ToList();
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

        public async Task SalvarAsync()
        {
            await CarregarSeNecessarioAsync();
            await _persistencia.SalvarSenhasAsync(_senhas, _chave);
        }
    }
}
