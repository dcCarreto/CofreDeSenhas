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

        // Memoiza a Task (não só o resultado) pra chamadas concorrentes dentro da
        // mesma sessão reaproveitarem a mesma mesclagem em vez de disparar várias —
        // mas se essa Task falhar (rede instável, banco reiniciando), o "??=" sozinho
        // deixaria _sincronizacao apontando pra sempre pra uma Task já quebrada: todo
        // método público desta classe começa chamando SincronizarAsync(), então a
        // mesma exceção original voltaria a cada chamada futura, mesmo pra operações
        // puramente locais — o cofre inteiro travaria até reiniciar o app. Limpa o
        // cache em caso de falha pra próxima chamada poder tentar de novo.
        private async Task SincronizarAsync()
        {
            _sincronizacao ??= MesclarAsync();
            try
            {
                await _sincronizacao;
            }
            catch
            {
                _sincronizacao = null;
                throw;
            }
        }

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
            foreach (var (id, nomeServico) in _banco.SemVerificacaoIntegridade)
                _ultimosConflitos.Add(new ConflitoSincronizacao
                {
                    SenhaId = id,
                    NomeServico = nomeServico,
                    Tipo = TipoConflitoSincronizacao.IntegridadeAusente,
                    DetectadoEmUtc = agoraUtc
                });

            var mesclados = MesclaSincronizacao.MesclarSenhas(locais, doBanco);
            var locaisPorId = locais.ToDictionary(s => s.Id);
            var doBancoPorId = doBanco.ToDictionary(s => s.Id);

            foreach (var item in mesclados)
            {
                var ehTumba = MesclaSincronizacao.EhTumbaDeExclusaoDefinitiva(item);

                if (!locaisPorId.TryGetValue(item.Id, out var existenteLocal))
                {
                    // Tumba de um item que este dispositivo nunca teve localmente: nada
                    // a fazer, não faz sentido criar uma entrada em branco do zero.
                    if (!ehTumba)
                        await _local.AdicionarAsync(item);
                }
                else if (!ReferenceEquals(existenteLocal, item))
                {
                    if (ehTumba)
                    {
                        // Remove por completo em vez de AtualizarAsync — senão o item
                        // fica sentado na lixeira local como uma cópia em branco em vez
                        // de simplesmente sumir, que é o que "excluído definitivamente
                        // em outro dispositivo" deveria significar aqui.
                        await _local.RemoverDefinitivamenteAsync(item.Id);
                    }
                    else
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
            }

            // Nunca publica por cima de um guid que acabou de falhar a verificação de
            // hmac — sem este filtro, se este dispositivo tiver localmente o mesmo
            // guid, a linha "violada" do banco seria sobrescrita com conteúdo local e
            // um hmac novo e válido antes de qualquer tela mostrar o conflito ao
            // usuário (via UltimosConflitos/JanelaConflitosSincronizacao), apagando o
            // rastro de uma possível adulteração ou corrupção no banco compartilhado.
            var idsComIntegridadeViolada = new HashSet<Guid>(_banco.ViolacoesIntegridade.Select(v => v.Id));
            var paraPublicar = (await _local.ListarTudoAsync())
                .Where(s => !idsComIntegridadeViolada.Contains(s.Id));
            await _banco.GravarVariasPorChaveAsync(paraPublicar);

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

                // Duas credenciais genuinamente diferentes, criadas em dispositivos
                // ainda não sincronizados, podem coincidir em nome de serviço + usuário
                // — reconciliar por esse par é uma aposta deliberada (o caso comum é a
                // mesma conta vista de dois lugares), mas unifica a identidade dos dois
                // lados. A mesclagem seguinte ("mais recente vence") descartaria a senha
                // atual do lado perdedor sem isto: guarda a senha atual de cada lado no
                // próprio histórico antes de unificar, pra sobreviver à mesclagem
                // aditiva de histórico independente de quem "vencer".
                item.Historico.Add(new HistoricoSenha { SenhaHash = item.SenhaHash, DataAlteracao = item.DataAtualizacao });
                correspondente.Historico.Add(new HistoricoSenha { SenhaHash = correspondente.SenhaHash, DataAlteracao = correspondente.DataAtualizacao });

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

            // Melhor esforço: o timestamp de "última cópia" é só informativo, não vale
            // a pena falhar a cópia (ação frequente e de baixa latência) por causa dele.
            try { await _banco.RegistrarCopiaAsync(id, campo); } catch { }
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
