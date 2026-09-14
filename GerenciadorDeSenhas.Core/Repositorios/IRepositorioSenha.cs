using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Repositorios
{
    public interface IRepositorioSenha
    {
        Task AdicionarAsync(Senha senha);
        Task AtualizarAsync(Senha senha);
        Task RegistrarCopiaAsync(Guid id, TipoCampoCopiado campo);
        Task RemoverAsync(Guid id);
        Task MoverTudoParaLixeiraAsync();
        Task<Senha?> ObterPorIdAsync(Guid id);
        Task<List<Senha>> ListarTodosAsync();
        Task<List<Senha>> ListarLixeiraAsync();
        Task<List<Senha>> ListarTudoAsync();
        Task RestaurarAsync(Guid id);
        Task RemoverDefinitivamenteAsync(Guid id);
        Task EsvaziarLixeiraAsync();
        Task SalvarAsync();
    }
}
