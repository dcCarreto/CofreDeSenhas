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

        private readonly List<ConflitoSincronizacao> _ultimosConflitos = new();

        public bool ReconciliacaoRealizadaNestaSessao { get; private set; }
        public IReadOnlyList<ConflitoSincronizacao> UltimosConflitos => _ultimosConflitos;

        public RepositorioSenhaEspelhado(IRepositorioSenha local, RepositorioSenhaBanco banco, bool reconciliacaoJaRealizada = false)
        {
            _local = local ?? throw new ArgumentNullException(nameof(local));
            _banco = banco ?? throw new ArgumentNullException(nameof(banco));
            _reconciliacaoJaRealizada = reconciliacaoJaRealizada;
        }

        private Task SincronizarAsync() => _sincronizacao ??= MesclarAsync();

        private async Task MesclarAsync()
        {
            var locais = await _local.ListarTudoAsync();
            var doBanco = await _banco.ListarTudoAsync();

            if (!_reconciliacaoJaRealizada)
            {
                await ReconciliarIdentidadeLegadaAsync(locais, doBanco);
                ReconciliacaoRealizadaNestaSessao = true;
            }

            var agoraUtc = DateTime.UtcNow;
            foreach (var (id, nomeServico) in _banco.ViolacoesIntegridade)
                _ultimosConflitos.Add(new ConflitoSincronizacao
                {
                    SenhaId = id,
                    NomeServico = nomeServico,
                    Tipo = TipoConflitoSincronizacao.IntegridadeViolada,
                    DetectadoEmUtc = agoraUtc
                });

            var mesclados = MesclaSincronizacao.MesclarSenhas(locais, doBanco);
            var locaisPorId = locais.ToDictionary(s => s.Id);
            var doBancoPorId = doBanco.ToDictionary(s => s.Id);

            foreach (var item in mesclados)
            {
                if (!locaisPorId.TryGetValue(item.Id, out var existenteLocal))
                    await _local.AdicionarAsync(item);
                else if (!ReferenceEquals(existenteLocal, item))
                {
                    await _local.AtualizarAsync(item);
                    if (doBancoPorId.ContainsKey(item.Id))
                        _ultimosConflitos.Add(new ConflitoSincronizacao
                        {
                            SenhaId = item.Id,
                            NomeServico = item.NomeServico,
                            Tipo = TipoConflitoSincronizacao.EdicaoConcorrente,
                            DetectadoEmUtc = agoraUtc
                        });
                }
            }

            await _banco.GravarVariasPorChaveAsync(await _local.ListarTudoAsync());

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

            await _local.AtualizarAsync(senha);
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

            await _local.RemoverAsync(id);
            await _banco.ExcluirPorChaveAsync(id);
        }

        public async Task MoverTudoParaLixeiraAsync()
        {
            await SincronizarAsync();

            var idsAtivos = (await _local.ListarTodosAsync()).Select(s => s.Id).ToList();
            await _local.MoverTudoParaLixeiraAsync();
            foreach (var id in idsAtivos)
                await _banco.ExcluirPorChaveAsync(id);
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

        public async Task<List<Senha>> ListarTudoAsync()
        {
            await SincronizarAsync();
            return await _local.ListarTudoAsync();
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

            await _local.RemoverDefinitivamenteAsync(id);
            await _banco.ExcluirDefinitivamentePorChaveAsync(id);
        }

        public async Task EsvaziarLixeiraAsync()
        {
            await SincronizarAsync();

            var idsLixeira = (await _local.ListarLixeiraAsync()).Select(s => s.Id).ToList();
            await _local.EsvaziarLixeiraAsync();
            foreach (var id in idsLixeira)
                await _banco.ExcluirDefinitivamentePorChaveAsync(id);
        }

        public async Task SalvarAsync()
        {
            await SincronizarAsync();
            await _local.SalvarAsync();
        }
    }
}
