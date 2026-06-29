using System.Text.RegularExpressions;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoSenha : IServicoSenha
    {
        private readonly IRepositorioSenha _repositorio;
        private readonly IServicoCriptografia _criptografia;

        public ServicoSenha(IRepositorioSenha repositorio, IServicoCriptografia criptografia)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _criptografia = criptografia ?? throw new ArgumentNullException(nameof(criptografia));
        }

        public async Task<Senha> CriarSenhaAsync(string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null)
        {
            ValidarEntrada(nomeServico, usuario, senhaPlaintext);

            var senha = new Senha
            {
                Id = Guid.NewGuid(),
                NomeServico = nomeServico,
                Usuario = usuario,
                SenhaHash = _criptografia.Criptografar(senhaPlaintext),
                Categoria = categoria,
                Url = url,
                Notas = notas,
                Favorito = false,
                DataCriacao = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };

            await _repositorio.AdicionarAsync(senha);
            return senha;
        }

        public async Task AtualizarSenhaAsync(Guid id, string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null)
        {
            ValidarEntrada(nomeServico, usuario, senhaPlaintext);

            var senha = await _repositorio.ObterPorIdAsync(id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            senha.NomeServico = nomeServico;
            senha.Usuario = usuario;
            senha.SenhaHash = _criptografia.Criptografar(senhaPlaintext);
            senha.Categoria = categoria;
            senha.Url = url;
            senha.Notas = notas;
            senha.DataAtualizacao = DateTime.UtcNow;

            await _repositorio.AtualizarAsync(senha);
        }

        public async Task RemoverSenhaAsync(Guid id)
        {
            var senha = await _repositorio.ObterPorIdAsync(id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            await _repositorio.RemoverAsync(id);
        }

        public async Task<Senha?> ObterSenhaAsync(Guid id)
        {
            return await _repositorio.ObterPorIdAsync(id);
        }

        public async Task<List<Senha>> ListarTodosAsync()
        {
            return await _repositorio.ListarTodosAsync();
        }

        public async Task<List<Senha>> BuscarPorServicoAsync(string nomeServico)
        {
            if (string.IsNullOrWhiteSpace(nomeServico))
                throw new ArgumentException("Nome do serviço não pode ser vazio");

            return await _repositorio.BuscarPorServicoAsync(nomeServico);
        }

        public async Task<List<Senha>> ListarPorCategoriaAsync(Categoria categoria)
        {
            return await _repositorio.BuscarPorCategoriaAsync(categoria);
        }

        public async Task<List<Senha>> ListarFavoritosAsync()
        {
            return await _repositorio.ListarFavoritosAsync();
        }

        public async Task MarcarComoFavoritoAsync(Guid id)
        {
            var senha = await _repositorio.ObterPorIdAsync(id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            senha.Favorito = true;
            senha.DataAtualizacao = DateTime.UtcNow;
            await _repositorio.AtualizarAsync(senha);
        }

        public async Task RemoverDeFavoritoAsync(Guid id)
        {
            var senha = await _repositorio.ObterPorIdAsync(id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");

            senha.Favorito = false;
            senha.DataAtualizacao = DateTime.UtcNow;
            await _repositorio.AtualizarAsync(senha);
        }

        public async Task PersistirAsync()
        {
            await _repositorio.SalvarAsync();
        }

        public bool ValidarForteSenha(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
                return false;

            if (senha.Length < 12)
                return false;

            if (!Regex.IsMatch(senha, @"[A-Z]"))
                return false;

            if (!Regex.IsMatch(senha, @"[a-z]"))
                return false;

            if (!Regex.IsMatch(senha, @"\d"))
                return false;

            if (!Regex.IsMatch(senha, @"[!@#$%^&*()_+\-=\[\]{};':""\|,.<>\/?]"))
                return false;

            return true;
        }

        public int ContarSenhas()
        {
            return _repositorio.ContarAsync().Result;
        }

        private static void ValidarEntrada(string nomeServico, string usuario, string senhaPlaintext)
        {
            if (string.IsNullOrWhiteSpace(nomeServico))
                throw new ArgumentException("Nome do serviço não pode ser vazio");

            if (string.IsNullOrWhiteSpace(usuario))
                throw new ArgumentException("Usuário não pode ser vazio");

            if (string.IsNullOrWhiteSpace(senhaPlaintext))
                throw new ArgumentException("Senha não pode ser vazia");

            if (nomeServico.Length > 100)
                throw new ArgumentException("Nome do serviço não pode exceder 100 caracteres");

            if (usuario.Length > 255)
                throw new ArgumentException("Usuário não pode exceder 255 caracteres");

            if (senhaPlaintext.Length > 1000)
                throw new ArgumentException("Senha não pode exceder 1000 caracteres");
        }
    }
}
