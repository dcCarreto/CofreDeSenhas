using System.Threading;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public class LimiteAnexoExcedidoException : Exception, ILocalizavel
    {
        public string Chave { get; }
        public object?[] Argumentos { get; }

        public LimiteAnexoExcedidoException(string chave, params object?[] argumentos) : base(chave)
        {
            Chave = chave;
            Argumentos = argumentos;
        }
    }

    public class ServicoAnexos
    {
        public const long TamanhoMaximoPorAnexo = 5 * 1024 * 1024;
        public const int QuantidadeMaximaPorCredencial = 5;
        public const long TamanhoMaximoTotalCofre = 100 * 1024 * 1024;

        private readonly IServicoCriptografia _criptografia;
        private readonly string _pastaAnexos;
        private readonly List<string> _avisos = new();

        // Serializa a checagem-e-escrita do limite total do cofre: sem isto, duas
        // chamadas concorrentes a AdicionarAsync (dois anexos grandes soltados quase
        // juntos) podiam ambas passar da checagem de TamanhoTotalAtual() antes de
        // qualquer uma escrever, ultrapassando os 100MB.
        private readonly SemaphoreSlim _travaEscrita = new(1, 1);

        // Falhas ao apagar o arquivo cifrado de um anexo (bloqueado por antivírus,
        // permissão negada) — a referência já foi removida do cofre mesmo assim, mas
        // fica registrado aqui em vez de simplesmente desaparecer sem rastro nenhum.
        public IReadOnlyList<string> UltimosAvisos => _avisos;

        public ServicoAnexos(IServicoCriptografia criptografia, string? pastaApp = null)
        {
            _criptografia = criptografia ?? throw new ArgumentNullException(nameof(criptografia));

            var pasta = pastaApp ?? AmbienteCofre.PastaDados;

            _pastaAnexos = Path.Combine(pasta, "anexos");
        }

        public void ApagarTudo()
        {
            if (Directory.Exists(_pastaAnexos))
                Directory.Delete(_pastaAnexos, recursive: true);
        }

        public long TamanhoTotalAtual() =>
            Directory.Exists(_pastaAnexos)
                ? new DirectoryInfo(_pastaAnexos).GetFiles().Sum(f => f.Length)
                : 0;

        public async Task<AnexoSenha> AdicionarAsync(Senha senha, string nomeArquivo, byte[] conteudo)
        {
            if (senha == null) throw new ArgumentNullException(nameof(senha));
            if (string.IsNullOrWhiteSpace(nomeArquivo)) throw new ErroLocalizavel("Attachment.Error.NameRequired");
            if (conteudo == null || conteudo.Length == 0) throw new ErroLocalizavel("Attachment.Error.EmptyFile");

            if (conteudo.Length > TamanhoMaximoPorAnexo)
                throw new LimiteAnexoExcedidoException("Attachment.Error.FileTooLarge", TamanhoMaximoPorAnexo / 1024 / 1024);

            await _travaEscrita.WaitAsync();
            try
            {
                if (senha.Anexos.Count >= QuantidadeMaximaPorCredencial)
                    throw new LimiteAnexoExcedidoException("Attachment.Error.MaxPerCredential", QuantidadeMaximaPorCredencial);

                if (TamanhoTotalAtual() + conteudo.Length > TamanhoMaximoTotalCofre)
                    throw new LimiteAnexoExcedidoException("Attachment.Error.VaultLimit", TamanhoMaximoTotalCofre / 1024 / 1024);

                if (!Directory.Exists(_pastaAnexos))
                    Directory.CreateDirectory(_pastaAnexos);

                var anexo = new AnexoSenha { NomeArquivo = nomeArquivo.Trim(), TamanhoBytes = conteudo.Length };
                var cifrado = _criptografia.CriptografarBytes(conteudo);
                await EscritaAtomica.EscreverBytesAsync(CaminhoArquivo(anexo.Id), cifrado);

                senha.Anexos.Add(anexo);
                return anexo;
            }
            finally
            {
                _travaEscrita.Release();
            }
        }

        public async Task<byte[]> LerAsync(AnexoSenha anexo)
        {
            if (anexo == null) throw new ArgumentNullException(nameof(anexo));

            var cifrado = await File.ReadAllBytesAsync(CaminhoArquivo(anexo.Id));
            return _criptografia.DescriptografarBytes(cifrado);
        }

        public async Task<Dictionary<Guid, byte[]>> LerTodosBrutosAsync(IEnumerable<Guid> anexoIds)
        {
            var resultado = new Dictionary<Guid, byte[]>();
            foreach (var id in anexoIds)
            {
                var caminho = CaminhoArquivo(id);
                if (File.Exists(caminho))
                    resultado[id] = await File.ReadAllBytesAsync(caminho);
            }
            return resultado;
        }

        public async Task EscreverBrutoAsync(Guid anexoId, byte[] cifrado)
        {
            if (!Directory.Exists(_pastaAnexos))
                Directory.CreateDirectory(_pastaAnexos);

            await EscritaAtomica.EscreverBytesAsync(CaminhoArquivo(anexoId), cifrado);
        }

        public void Remover(Senha senha, Guid anexoId)
        {
            if (senha == null) throw new ArgumentNullException(nameof(senha));

            _avisos.Clear();
            senha.Anexos.RemoveAll(a => a.Id == anexoId);
            try { File.Delete(CaminhoArquivo(anexoId)); }
            catch (Exception ex) { _avisos.Add($"Não foi possível apagar o arquivo cifrado do anexo {anexoId}: {ex.Message}"); }
        }

        public void RemoverTodos(Senha senha)
        {
            if (senha == null) throw new ArgumentNullException(nameof(senha));

            _avisos.Clear();
            RemoverTodosSemLimparAvisos(senha);
        }

        // Sobrecarga em lote pra quem precisa remover os anexos de várias credenciais
        // como uma operação só (ex.: esvaziar a lixeira inteira) — limpa _avisos uma
        // única vez no início e acumula os avisos de todos os itens processados. Sem
        // isto, chamar RemoverTodos(Senha) num loop externo perdia os avisos de cada
        // item anterior a cada nova chamada (só o último item processado sobrevivia),
        // já que RemoverTodos(Senha) limpa _avisos a cada chamada.
        public void RemoverTodos(IEnumerable<Senha> senhas)
        {
            if (senhas == null) throw new ArgumentNullException(nameof(senhas));

            _avisos.Clear();
            foreach (var senha in senhas)
                RemoverTodosSemLimparAvisos(senha);
        }

        private void RemoverTodosSemLimparAvisos(Senha senha)
        {
            foreach (var anexo in senha.Anexos)
            {
                try { File.Delete(CaminhoArquivo(anexo.Id)); }
                catch (Exception ex) { _avisos.Add($"Não foi possível apagar o arquivo cifrado do anexo {anexo.Id}: {ex.Message}"); }
            }

            senha.Anexos.Clear();
        }

        private string CaminhoArquivo(Guid id) => Path.Combine(_pastaAnexos, id.ToString("N") + ".enc");
    }
}
