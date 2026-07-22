using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace GerenciadorDeSenhas.Repositorios
{
    public class RepositorioSenhaEspelhado : IRepositorioSenha
    {
        private readonly IRepositorioSenha _local;
        private readonly RepositorioSenhaBanco _banco;
        private readonly bool _reconciliacaoJaRealizada;
        private Task? _sincronizacao;

        public bool ReconciliacaoRealizadaNestaSessao { get; private set; }

        public RepositorioSenhaEspelhado(IRepositorioSenha local, RepositorioSenhaBanco banco, bool reconciliacaoJaRealizada = false)
        {
            _local = local ?? throw new ArgumentNullException(nameof(local));
            _banco = banco ?? throw new ArgumentNullException(nameof(banco));
            _reconciliacaoJaRealizada = reconciliacaoJaRealizada;
        }

        private Task SincronizarAsync() => _sincronizacao ??= MesclarAsync();

        private async Task MesclarAsync()
        {
            var locais = await _local.ListarTodosAsync();
            var doBanco = await _banco.ListarTodosAsync();

            if (!_reconciliacaoJaRealizada)
            {
                await ReconciliarIdentidadeLegadaAsync(locais, doBanco);
                ReconciliacaoRealizadaNestaSessao = true;
            }

            var mesclados = MesclaSincronizacao.Mesclar(locais, doBanco, s => s.Id, s => s.DataAtualizacao);
            var locaisPorId = locais.ToDictionary(s => s.Id);

            foreach (var item in mesclados)
            {
                if (!locaisPorId.TryGetValue(item.Id, out var existenteLocal))
                    await _local.AdicionarAsync(item);
                else if (!ReferenceEquals(existenteLocal, item))
                    await _local.AtualizarAsync(item);
            }

            await _banco.GravarVariasPorChaveAsync(await _local.ListarTodosAsync());

            await _local.SalvarAsync();
        }

        private async Task ReconciliarIdentidadeLegadaAsync(List<Senha> locais, List<Senha> doBanco)
        {
            var locaisPorChave = new Dictionary<string, Senha>();
            foreach (var item in locais)
                locaisPorChave.TryAdd(Chave(item), item);

            var idsLocais = new HashSet<Guid>(locais.Select(s => s.Id));
            var jaReconciliados = new HashSet<Guid>();

            foreach (var item in doBanco)
            {
                if (idsLocais.Contains(item.Id))
                    continue;

                if (!locaisPorChave.TryGetValue(Chave(item), out var correspondente))
                    continue;

                if (!jaReconciliados.Add(correspondente.Id))
                    continue;

                await _banco.SubstituirGuidAsync(item.Id, correspondente.Id);
            }
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

        public async Task RegistrarCopiaAsync(Guid id, TipoCampoCopiado campo)
        {
            await SincronizarAsync();
            await _local.RegistrarCopiaAsync(id, campo);
        }

        public async Task RemoverAsync(Guid id)
        {
            await SincronizarAsync();

            var senha = await _local.ObterPorIdAsync(id);
            await _local.RemoverAsync(id);
            if (senha != null)
                await _banco.ExcluirPorChaveAsync(senha.NomeServico, senha.Usuario);
        }

        public async Task MoverTudoParaLixeiraAsync()
        {
            await SincronizarAsync();

            var ativos = await _local.ListarTodosAsync();
            await _local.MoverTudoParaLixeiraAsync();
            foreach (var senha in ativos)
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

        public async Task<List<Senha>> ListarLixeiraAsync()
        {
            await SincronizarAsync();
            return await _local.ListarLixeiraAsync();
        }

        public async Task RestaurarAsync(Guid id)
        {
            await SincronizarAsync();

            await _local.RestaurarAsync(id);

            var senha = await _local.ObterPorIdAsync(id);
            if (senha != null)
                await _banco.GravarPorChaveAsync(senha);
        }

        public async Task RemoverDefinitivamenteAsync(Guid id)
        {
            await SincronizarAsync();

            var senha = await _local.ObterPorIdAsync(id);
            await _local.RemoverDefinitivamenteAsync(id);
            if (senha != null)
                await _banco.ExcluirDefinitivamentePorChaveAsync(senha.NomeServico, senha.Usuario);
        }

        public async Task EsvaziarLixeiraAsync()
        {
            await SincronizarAsync();

            var lixeira = await _local.ListarLixeiraAsync();
            await _local.EsvaziarLixeiraAsync();
            foreach (var senha in lixeira)
                await _banco.ExcluirDefinitivamentePorChaveAsync(senha.NomeServico, senha.Usuario);
        }

        public async Task SalvarAsync()
        {
            await SincronizarAsync();
            await _local.SalvarAsync();
        }
    }
}
