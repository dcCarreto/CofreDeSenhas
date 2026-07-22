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

        public ServicoAnexos(IServicoCriptografia criptografia, string? pastaApp = null)
        {
            _criptografia = criptografia ?? throw new ArgumentNullException(nameof(criptografia));

            var pasta = pastaApp ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GerenciadorSenhas");

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

            if (senha.Anexos.Count >= QuantidadeMaximaPorCredencial)
                throw new LimiteAnexoExcedidoException("Attachment.Error.MaxPerCredential", QuantidadeMaximaPorCredencial);

            if (TamanhoTotalAtual() + conteudo.Length > TamanhoMaximoTotalCofre)
                throw new LimiteAnexoExcedidoException("Attachment.Error.VaultLimit", TamanhoMaximoTotalCofre / 1024 / 1024);

            if (!Directory.Exists(_pastaAnexos))
                Directory.CreateDirectory(_pastaAnexos);

            var anexo = new AnexoSenha { NomeArquivo = nomeArquivo.Trim(), TamanhoBytes = conteudo.Length };
            var cifrado = _criptografia.CriptografarBytes(conteudo);
            await File.WriteAllBytesAsync(CaminhoArquivo(anexo.Id), cifrado);

            senha.Anexos.Add(anexo);
            return anexo;
        }

        public async Task<byte[]> LerAsync(AnexoSenha anexo)
        {
            if (anexo == null) throw new ArgumentNullException(nameof(anexo));

            var cifrado = await File.ReadAllBytesAsync(CaminhoArquivo(anexo.Id));
            return _criptografia.DescriptografarBytes(cifrado);
        }

        public void Remover(Senha senha, Guid anexoId)
        {
            if (senha == null) throw new ArgumentNullException(nameof(senha));

            senha.Anexos.RemoveAll(a => a.Id == anexoId);
            try { File.Delete(CaminhoArquivo(anexoId)); } catch { }
        }

        public void RemoverTodos(Senha senha)
        {
            if (senha == null) throw new ArgumentNullException(nameof(senha));

            foreach (var anexo in senha.Anexos)
                try { File.Delete(CaminhoArquivo(anexo.Id)); } catch { }

            senha.Anexos.Clear();
        }

        private string CaminhoArquivo(Guid id) => Path.Combine(_pastaAnexos, id.ToString("N") + ".enc");
    }
}
