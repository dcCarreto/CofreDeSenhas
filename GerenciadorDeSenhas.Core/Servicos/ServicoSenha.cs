using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoSenha : IServicoSenha
    {
        // public (mesmo padrão de Etiquetas.QuantidadeMaxima) pra MesclaSincronizacao
        // reaplicar o mesmo teto depois da mesclagem aditiva de histórico — sem isso,
        // dois dispositivos trocando a senha em momentos diferentes entre ciclos de
        // sync somam entradas pra sempre e nunca mais voltam a 10.
        public const int MaxHistorico = 10;
        public const int MaxCodigosRecuperacao = 100;
        private const int MaxComprimentoCodigoRecuperacao = 500;

        private readonly IRepositorioSenha _repositorio;
        private readonly IServicoCriptografia _criptografia;
        private readonly ServicoTotp _totp = new();
        private readonly List<string> _avisos = new();

        public IReadOnlyList<string> UltimosAvisos => _avisos;

        public ServicoSenha(IRepositorioSenha repositorio, IServicoCriptografia criptografia)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _criptografia = criptografia ?? throw new ArgumentNullException(nameof(criptografia));
        }

        public async Task<Senha> CriarSenhaAsync(string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null, string? totpSegredo = null,
            IEnumerable<string>? etiquetas = null, TipoCredencial tipo = TipoCredencial.Login,
            IReadOnlyDictionary<string, string>? camposExtras = null)
        {
            ValidarEntrada(nomeServico, usuario, senhaPlaintext, url, notas, camposExtras);

            var senha = new Senha
            {
                Id = Guid.NewGuid(),
                NomeServico = nomeServico,
                Usuario = usuario,
                SenhaHash = _criptografia.Criptografar(senhaPlaintext),
                Categoria = categoria,
                Etiquetas = Etiquetas.Normalizar(etiquetas),
                Url = url,
                Notas = notas,
                TotpSegredo = CifrarTotp(totpSegredo),
                Favorito = false,
                Tipo = tipo,
                CamposExtras = CifrarCamposExtras(camposExtras),
                DataCriacao = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };

            await _repositorio.AdicionarAsync(senha);
            return senha;
        }

        public async Task AtualizarSenhaAsync(Guid id, string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null, IEnumerable<string>? etiquetas = null,
            TipoCredencial? tipo = null, IReadOnlyDictionary<string, string>? camposExtras = null)
        {
            ValidarEntrada(nomeServico, usuario, senhaPlaintext, url, notas, camposExtras);

            var senha = await ObterOuFalharAsync(id);

            _avisos.Clear();
            RegistrarHistoricoSeMudou(senha, senhaPlaintext);

            senha.NomeServico = nomeServico;
            senha.Usuario = usuario;
            senha.SenhaHash = _criptografia.Criptografar(senhaPlaintext);
            senha.Categoria = categoria;
            if (etiquetas != null)
                senha.Etiquetas = Etiquetas.Normalizar(etiquetas);
            senha.Url = url;
            senha.Notas = notas;
            if (tipo.HasValue)
                senha.Tipo = tipo.Value;
            if (camposExtras != null)
                senha.CamposExtras = CifrarCamposExtras(camposExtras);
            senha.DataAtualizacao = DateTime.UtcNow;

            await _repositorio.AtualizarAsync(senha);
        }

        private Dictionary<string, string> CifrarCamposExtras(IReadOnlyDictionary<string, string>? camposExtras)
        {
            var resultado = new Dictionary<string, string>();
            if (camposExtras == null)
                return resultado;

            foreach (var (chave, valor) in camposExtras)
                if (!string.IsNullOrWhiteSpace(valor))
                    resultado[chave] = _criptografia.Criptografar(valor);

            return resultado;
        }

        public async Task DefinirTotpAsync(Guid id, string? segredoPlaintext)
        {
            var senha = await ObterOuFalharAsync(id);

            senha.TotpSegredo = CifrarTotp(segredoPlaintext);
            senha.DataAtualizacao = DateTime.UtcNow;

            await _repositorio.AtualizarAsync(senha);
        }

        public async Task AdicionarCodigosRecuperacaoAsync(Guid id, IEnumerable<(string Codigo, bool Usado)> codigos)
        {
            var senha = await ObterOuFalharAsync(id);

            var validos = (codigos ?? Enumerable.Empty<(string Codigo, bool Usado)>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Codigo))
                .Select(c => (Codigo: c.Codigo.Trim(), c.Usado))
                .ToList();

            if (validos.Any(c => c.Codigo.Length > MaxComprimentoCodigoRecuperacao))
                throw new ErroLocalizavel("Entry.Error.RecoveryCodeTooLong", MaxComprimentoCodigoRecuperacao);

            if (senha.CodigosRecuperacao.Count + validos.Count > MaxCodigosRecuperacao)
                throw new ErroLocalizavel("Entry.Error.RecoveryCodesTooMany", MaxCodigosRecuperacao);

            var novos = validos.Select(c => new CodigoRecuperacao
            {
                Codigo = _criptografia.Criptografar(c.Codigo),
                Usado = c.Usado
            });

            senha.CodigosRecuperacao.AddRange(novos);
            senha.DataAtualizacao = DateTime.UtcNow;

            await _repositorio.AtualizarAsync(senha);
        }

        public async Task MarcarCodigoRecuperacaoAsync(Guid id, Guid codigoId, bool usado)
        {
            var senha = await ObterOuFalharAsync(id);

            var codigo = senha.CodigosRecuperacao.FirstOrDefault(c => c.Id == codigoId);
            if (codigo == null)
                throw new InvalidOperationException($"Código de recuperação com ID {codigoId} não encontrado");

            codigo.Usado = usado;
            senha.DataAtualizacao = DateTime.UtcNow;

            await _repositorio.AtualizarAsync(senha);
        }

        public async Task RemoverCodigoRecuperacaoAsync(Guid id, Guid codigoId)
        {
            var senha = await ObterOuFalharAsync(id);

            senha.CodigosRecuperacao.RemoveAll(c => c.Id == codigoId);
            senha.DataAtualizacao = DateTime.UtcNow;

            await _repositorio.AtualizarAsync(senha);
        }

        private async Task<Senha> ObterOuFalharAsync(Guid id)
        {
            var senha = await _repositorio.ObterPorIdAsync(id);
            if (senha == null)
                throw new InvalidOperationException($"Senha com ID {id} não encontrada");
            return senha;
        }

        private string? CifrarTotp(string? segredoPlaintext)
        {
            if (string.IsNullOrWhiteSpace(segredoPlaintext))
                return null;

            return _criptografia.Criptografar(_totp.NormalizarSegredo(segredoPlaintext));
        }

        private void RegistrarHistoricoSeMudou(Senha senha, string novaSenhaPlaintext)
        {
            string senhaAnterior;
            try
            {
                senhaAnterior = _criptografia.Descriptografar(senha.SenhaHash);
            }
            catch (Exception ex)
            {
                _avisos.Add($"Não foi possível registrar o histórico de \"{senha.NomeServico}\": a senha anterior está corrompida ({ex.Message}).");
                return;
            }

            if (senhaAnterior == novaSenhaPlaintext)
                return;

            senha.Historico.Add(new HistoricoSenha
            {
                SenhaHash = senha.SenhaHash,
                DataAlteracao = DateTime.UtcNow
            });

            if (senha.Historico.Count > MaxHistorico)
                senha.Historico.RemoveRange(0, senha.Historico.Count - MaxHistorico);
        }

        public async Task RemoverSenhaAsync(Guid id)
        {
            await ObterOuFalharAsync(id);
            await _repositorio.RemoverAsync(id);
        }

        public async Task LimparCofreAsync()
        {
            await _repositorio.MoverTudoParaLixeiraAsync();
        }

        public async Task<List<Senha>> ListarTodosAsync()
        {
            return await _repositorio.ListarTodosAsync();
        }

        public async Task<List<Senha>> ListarLixeiraAsync()
        {
            return await _repositorio.ListarLixeiraAsync();
        }

        public async Task RestaurarSenhaAsync(Guid id)
        {
            await _repositorio.RestaurarAsync(id);
        }

        public async Task RemoverDefinitivamenteAsync(Guid id)
        {
            await _repositorio.RemoverDefinitivamenteAsync(id);
        }

        public async Task EsvaziarLixeiraAsync()
        {
            await _repositorio.EsvaziarLixeiraAsync();
        }

        public async Task MarcarComoFavoritoAsync(Guid id)
        {
            var senha = await ObterOuFalharAsync(id);

            senha.Favorito = true;
            senha.DataAtualizacao = DateTime.UtcNow;
            await _repositorio.AtualizarAsync(senha);
        }

        public async Task RemoverDeFavoritoAsync(Guid id)
        {
            var senha = await ObterOuFalharAsync(id);

            senha.Favorito = false;
            senha.DataAtualizacao = DateTime.UtcNow;
            await _repositorio.AtualizarAsync(senha);
        }

        public async Task MarcarComoFixadoAsync(Guid id)
        {
            var senha = await ObterOuFalharAsync(id);

            senha.Fixado = true;
            senha.DataAtualizacao = DateTime.UtcNow;
            await _repositorio.AtualizarAsync(senha);
        }

        public async Task RemoverFixacaoAsync(Guid id)
        {
            var senha = await ObterOuFalharAsync(id);

            senha.Fixado = false;
            senha.DataAtualizacao = DateTime.UtcNow;
            await _repositorio.AtualizarAsync(senha);
        }

        public async Task RegistrarCopiaAsync(Guid id, TipoCampoCopiado campo)
        {
            await _repositorio.RegistrarCopiaAsync(id, campo);
        }

        public async Task AplicarSincronizadoAsync(SenhaExportada item)
        {
            // Tumba de exclusão definitiva vinda de outro dispositivo (ver
            // JanelaPrincipal.PublicarTumbasNaPastaDeSincronizacaoAsync) — remove por
            // completo em vez de aplicar como uma edição normal, senão o item fica
            // sentado na lixeira local como uma cópia em branco em vez de simplesmente
            // sumir, que é o que "excluído definitivamente em outro dispositivo"
            // deveria significar aqui. RemoverDefinitivamenteAsync já é no-op se o
            // item nunca existiu localmente.
            if (MesclaSincronizacao.EhTumbaDeExclusaoDefinitiva(item))
            {
                await _repositorio.RemoverDefinitivamenteAsync(item.Id);
                return;
            }

            var historico = item.Historico
                .Where(h => !string.IsNullOrEmpty(h.Senha))
                .Select(h => new HistoricoSenha
                {
                    SenhaHash = _criptografia.Criptografar(h.Senha),
                    DataAlteracao = h.DataAlteracao
                })
                .ToList();

            var codigosRecuperacao = item.CodigosRecuperacao
                .Where(c => !string.IsNullOrEmpty(c.Codigo))
                .Select(c => new CodigoRecuperacao
                {
                    Codigo = _criptografia.Criptografar(c.Codigo),
                    Usado = c.Usado
                })
                .ToList();

            var existente = await _repositorio.ObterPorIdAsync(item.Id);
            if (existente != null)
            {
                existente.NomeServico = item.NomeServico;
                existente.Usuario = item.Usuario;
                existente.SenhaHash = _criptografia.Criptografar(item.Senha);
                existente.Url = item.Url;
                existente.Categoria = item.Categoria;
                existente.Etiquetas = Etiquetas.Normalizar(item.Etiquetas);
                existente.Notas = item.Notas;
                existente.Tipo = item.Tipo;
                existente.CamposExtras = CifrarCamposExtras(item.CamposExtras);
                existente.TotpSegredo = CifrarTotp(item.TotpSegredo);
                existente.Historico = historico;
                existente.CodigosRecuperacao = codigosRecuperacao;
                existente.Favorito = item.Favorito;
                existente.Fixado = item.Fixado;
                existente.NaLixeira = item.NaLixeira;
                existente.DataExclusao = item.DataExclusao;
                existente.DataCriacao = item.DataCriacao;
                existente.DataAtualizacao = item.DataAtualizacao;

                await _repositorio.AtualizarAsync(existente);
            }
            else
            {
                var novo = new Senha
                {
                    Id = item.Id,
                    NomeServico = item.NomeServico,
                    Usuario = item.Usuario,
                    SenhaHash = _criptografia.Criptografar(item.Senha),
                    Url = item.Url,
                    Categoria = item.Categoria,
                    Etiquetas = Etiquetas.Normalizar(item.Etiquetas),
                    Notas = item.Notas,
                    Tipo = item.Tipo,
                    CamposExtras = CifrarCamposExtras(item.CamposExtras),
                    TotpSegredo = CifrarTotp(item.TotpSegredo),
                    Historico = historico,
                    CodigosRecuperacao = codigosRecuperacao,
                    Favorito = item.Favorito,
                    Fixado = item.Fixado,
                    NaLixeira = item.NaLixeira,
                    DataExclusao = item.DataExclusao,
                    DataCriacao = item.DataCriacao,
                    DataAtualizacao = item.DataAtualizacao
                };

                await _repositorio.AdicionarAsync(novo);
            }
        }

        public async Task PersistirAsync()
        {
            await _repositorio.SalvarAsync();
        }

        private static void ValidarEntrada(string nomeServico, string usuario, string senhaPlaintext,
            string? url = null, string? notas = null, IReadOnlyDictionary<string, string>? camposExtras = null)
        {
            if (string.IsNullOrWhiteSpace(nomeServico))
                throw new ErroLocalizavel("Entry.Error.ServiceRequired");

            if (string.IsNullOrWhiteSpace(usuario))
                throw new ErroLocalizavel("Entry.Error.UserRequired");

            if (string.IsNullOrWhiteSpace(senhaPlaintext))
                throw new ErroLocalizavel("Entry.Error.PasswordRequired");

            if (nomeServico.Length > 100)
                throw new ErroLocalizavel("Entry.Error.ServiceTooLong", 100);

            if (usuario.Length > 255)
                throw new ErroLocalizavel("Entry.Error.UserTooLong", 255);

            if (senhaPlaintext.Length > 1000)
                throw new ErroLocalizavel("Entry.Error.PasswordTooLong", 1000);

            if (url != null && url.Length > 2048)
                throw new ErroLocalizavel("Entry.Error.UrlTooLong", 2048);

            if (notas != null && notas.Length > 5000)
                throw new ErroLocalizavel("Entry.Error.NotesTooLong", 5000);

            if (camposExtras != null)
                foreach (var valor in camposExtras.Values)
                    if (valor != null && valor.Length > 1000)
                        throw new ErroLocalizavel("Entry.Error.CustomFieldTooLong", 1000);
        }
    }
}
