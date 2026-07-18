using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public interface IServicoSenha
    {
        Task<Senha> CriarSenhaAsync(string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null, string? totpSegredo = null,
            IEnumerable<string>? etiquetas = null);

        Task AtualizarSenhaAsync(Guid id, string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null,
            IEnumerable<string>? etiquetas = null);

        Task DefinirTotpAsync(Guid id, string? segredoPlaintext);

        Task AdicionarCodigosRecuperacaoAsync(Guid id, IEnumerable<(string Codigo, bool Usado)> codigos);

        Task MarcarCodigoRecuperacaoAsync(Guid id, Guid codigoId, bool usado);

        Task RemoverCodigoRecuperacaoAsync(Guid id, Guid codigoId);

        Task RemoverSenhaAsync(Guid id);

        Task<List<Senha>> ListarTodosAsync();

        Task<List<Senha>> ListarLixeiraAsync();

        Task RestaurarSenhaAsync(Guid id);

        Task RemoverDefinitivamenteAsync(Guid id);

        Task EsvaziarLixeiraAsync();

        Task MarcarComoFavoritoAsync(Guid id);

        Task RemoverDeFavoritoAsync(Guid id);

        Task MarcarComoFixadoAsync(Guid id);

        Task RemoverFixacaoAsync(Guid id);

        Task RegistrarCopiaAsync(Guid id, TipoCampoCopiado campo);

        Task PersistirAsync();
    }
}
