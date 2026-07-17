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
        Task<List<Senha>> ListarLixeiraAsync();
        Task RestaurarAsync(Guid id);
        Task RemoverDefinitivamenteAsync(Guid id);
        Task EsvaziarLixeiraAsync();
        Task SalvarAsync();
    }
}
