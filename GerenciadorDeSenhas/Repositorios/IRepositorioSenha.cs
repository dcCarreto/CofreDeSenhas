using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Repositorios
{
    public interface IRepositorioSenha
    {
        Task AdicionarAsync(Senha senha);
        Task AtualizarAsync(Senha senha);
        Task RemoverAsync(Guid id);
        Task<Senha?> ObterPorIdAsync(Guid id);
        Task<List<Senha>> ListarTodosAsync();
        Task<List<Senha>> BuscarPorCategoriaAsync(Categoria categoria);
        Task<List<Senha>> BuscarPorServicoAsync(string nomeServico);
        Task<List<Senha>> ListarFavoritosAsync();
        Task<int> ContarAsync();
        Task SalvarAsync();
    }
}
