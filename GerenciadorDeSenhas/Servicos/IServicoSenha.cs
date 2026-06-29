using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public interface IServicoSenha
    {
        Task<Senha> CriarSenhaAsync(string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null);

        Task AtualizarSenhaAsync(Guid id, string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null);

        Task RemoverSenhaAsync(Guid id);

        Task<Senha?> ObterSenhaAsync(Guid id);

        Task<List<Senha>> ListarTodosAsync();

        Task<List<Senha>> BuscarPorServicoAsync(string nomeServico);

        Task<List<Senha>> ListarPorCategoriaAsync(Categoria categoria);

        Task<List<Senha>> ListarFavoritosAsync();

        Task MarcarComoFavoritoAsync(Guid id);

        Task RemoverDeFavoritoAsync(Guid id);

        Task PersistirAsync();

        bool ValidarForteSenha(string senha);

        int ContarSenhas();
    }
}
