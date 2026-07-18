using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoSenha : IServicoSenha
    {
        private const int MaxHistorico = 10;

        private readonly IRepositorioSenha _repositorio;
        private readonly IServicoCriptografia _criptografia;
        private readonly ServicoTotp _totp = new();

        public ServicoSenha(IRepositorioSenha repositorio, IServicoCriptografia criptografia)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _criptografia = criptografia ?? throw new ArgumentNullException(nameof(criptografia));
        }

        public async Task<Senha> CriarSenhaAsync(string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null, string? totpSegredo = null,
            IEnumerable<string>? etiquetas = null)
        {
            ValidarEntrada(nomeServico, usuario, senhaPlaintext);

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
                DataCriacao = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };

            await _repositorio.AdicionarAsync(senha);
            return senha;
        }

        public async Task AtualizarSenhaAsync(Guid id, string nomeServico, string usuario, string senhaPlaintext,
            Categoria categoria, string? url = null, string? notas = null, IEnumerable<string>? etiquetas = null)
        {
            ValidarEntrada(nomeServico, usuario, senhaPlaintext);

            var senha = await ObterOuFalharAsync(id);

            RegistrarHistoricoSeMudou(senha, senhaPlaintext);

            senha.NomeServico = nomeServico;
            senha.Usuario = usuario;
            senha.SenhaHash = _criptografia.Criptografar(senhaPlaintext);
            senha.Categoria = categoria;
            if (etiquetas != null)
                senha.Etiquetas = Etiquetas.Normalizar(etiquetas);
            senha.Url = url;
            senha.Notas = notas;
            senha.DataAtualizacao = DateTime.UtcNow;

            await _repositorio.AtualizarAsync(senha);
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

            var novos = (codigos ?? Enumerable.Empty<(string Codigo, bool Usado)>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Codigo))
                .Select(c => new CodigoRecuperacao
                {
                    Codigo = _criptografia.Criptografar(c.Codigo.Trim()),
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
            catch
            {
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
            var senha = await ObterOuFalharAsync(id);

            await _repositorio.RemoverAsync(id);
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

        public async Task PersistirAsync()
        {
            await _repositorio.SalvarAsync();
        }

        private static void ValidarEntrada(string nomeServico, string usuario, string senhaPlaintext)
        {
            if (string.IsNullOrWhiteSpace(nomeServico))
                throw new ArgumentException("Nome do serviço não pode ser vazio");

            if (string.IsNullOrWhiteSpace(usuario))
                throw new ArgumentException("Usuário não pode ser vazio");

            if (string.IsNullOrWhiteSpace(senhaPlaintext))
                throw new ArgumentException("Senha não pode ser vazia");

            if (nomeServico.Length > 100)
                throw new ArgumentException("Nome do serviço não pode exceder 100 caracteres");

            if (usuario.Length > 255)
                throw new ArgumentException("Usuário não pode exceder 255 caracteres");

            if (senhaPlaintext.Length > 1000)
                throw new ArgumentException("Senha não pode exceder 1000 caracteres");
        }
    }
}
