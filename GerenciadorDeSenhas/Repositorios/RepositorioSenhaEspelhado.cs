using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Repositorios
{
    public class RepositorioSenhaEspelhado : IRepositorioSenha
    {
        private readonly IRepositorioSenha _local;
        private readonly RepositorioSenhaBanco _banco;
        private Task? _sincronizacao;

        public RepositorioSenhaEspelhado(IRepositorioSenha local, RepositorioSenhaBanco banco)
        {
            _local = local ?? throw new ArgumentNullException(nameof(local));
            _banco = banco ?? throw new ArgumentNullException(nameof(banco));
        }

        private Task SincronizarAsync() => _sincronizacao ??= MesclarAsync();

        private async Task MesclarAsync()
        {
            var locais = await _local.ListarTodosAsync();
            var doBanco = await _banco.ListarTodosAsync();

            var chavesLocais = new HashSet<string>(locais.Select(Chave));

            foreach (var senha in doBanco)
                if (!chavesLocais.Contains(Chave(senha)))
                    await _local.AdicionarAsync(senha);

            await _banco.GravarVariasPorChaveAsync(await _local.ListarTodosAsync());

            await _local.SalvarAsync();
        }

        private static string Chave(Senha s) =>
            (s.NomeServico + " " + s.Usuario).ToLowerInvariant();

        public async Task AdicionarAsync(Senha senha)
        {
            await SincronizarAsync();
            await _local.AdicionarAsync(senha);
            await _banco.GravarPorChaveAsync(senha);
        }

        public async Task AtualizarAsync(Senha senha)
        {
            await SincronizarAsync();

            var antiga = await _local.ObterPorIdAsync(senha.Id);

            await _local.AtualizarAsync(senha);

            if (antiga != null && Chave(antiga) != Chave(senha))
                await _banco.ExcluirPorChaveAsync(antiga.NomeServico, antiga.Usuario);
            await _banco.GravarPorChaveAsync(senha);
        }

        public async Task RemoverAsync(Guid id)
        {
            await SincronizarAsync();

            var senha = await _local.ObterPorIdAsync(id);
            await _local.RemoverAsync(id);
            if (senha != null)
                await _banco.ExcluirPorChaveAsync(senha.NomeServico, senha.Usuario);
        }

        public async Task<Senha?> ObterPorIdAsync(Guid id)
        {
            await SincronizarAsync();
            return await _local.ObterPorIdAsync(id);
        }

        public async Task<List<Senha>> ListarTodosAsync()
        {
            await SincronizarAsync();
            return await _local.ListarTodosAsync();
        }

        public async Task<List<Senha>> BuscarPorCategoriaAsync(Categoria categoria)
        {
            await SincronizarAsync();
            return await _local.BuscarPorCategoriaAsync(categoria);
        }

        public async Task<List<Senha>> BuscarPorServicoAsync(string nomeServico)
        {
            await SincronizarAsync();
            return await _local.BuscarPorServicoAsync(nomeServico);
        }

        public async Task<List<Senha>> ListarFavoritosAsync()
        {
            await SincronizarAsync();
            return await _local.ListarFavoritosAsync();
        }

        public async Task<int> ContarAsync()
        {
            await SincronizarAsync();
            return await _local.ContarAsync();
        }

        public async Task SalvarAsync()
        {
            await SincronizarAsync();
            await _local.SalvarAsync();
        }
    }
}
