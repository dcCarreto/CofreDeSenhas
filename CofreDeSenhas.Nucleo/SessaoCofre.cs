using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Nucleo;

public readonly record struct ItemConsulta(Guid Id, string Servico, string Usuario);

public readonly record struct Credencial(string Usuario, string Senha);

public sealed class SessaoCofre : IDisposable
{
    private readonly object _trava = new();
    private readonly string? _pastaApp;
    private readonly AutenticacaoMestra _auth;
    private readonly TimeSpan _tempoInatividade;
    private readonly Timer _timer;

    private byte[]? _chave;
    private ServicoCriptografia? _cripto;
    private List<Senha> _senhas = new();
    private DateTime _ultimaAtividade = DateTime.UtcNow;

    public SessaoCofre(string? pastaApp = null, TimeSpan? tempoInatividade = null)
    {
        _pastaApp = pastaApp;
        _auth = new AutenticacaoMestra(pastaApp);
        _tempoInatividade = tempoInatividade ?? TimeSpan.FromMinutes(15);
        _timer = new Timer(_ => VerificarInatividade(), null,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public bool Destrancado
    {
        get { lock (_trava) return _chave != null; }
    }

    public int Total
    {
        get { lock (_trava) return _chave == null ? 0 : _senhas.Count; }
    }

    public async Task<bool> DestrancarAsync(string senhaMestra)
    {
        var chave = _auth.Autenticar(senhaMestra);
        if (chave == null)
            return false;

        var cripto = new ServicoCriptografia(chave);
        var persistencia = new PersistenciaLocal(cripto, _pastaApp);
        var repo = new RepositorioSenha(persistencia, chave);
        var lista = await repo.ListarTodosAsync();

        lock (_trava)
        {
            ApagarChave();
            _chave = chave;
            _cripto = cripto;
            _senhas = lista;
            _ultimaAtividade = DateTime.UtcNow;
        }
        return true;
    }

    public void Trancar()
    {
        lock (_trava) TrancarInterno();
    }

    public IReadOnlyList<ItemConsulta> Consultar(string dominio)
    {
        lock (_trava)
        {
            if (_chave == null)
                return Array.Empty<ItemConsulta>();

            _ultimaAtividade = DateTime.UtcNow;
            return _senhas
                .Where(s => Dominios.Casa(dominio, s.Url))
                .Select(s => new ItemConsulta(s.Id, s.NomeServico, s.Usuario))
                .ToList();
        }
    }

    public Credencial? ObterCredencial(Guid id)
    {
        lock (_trava)
        {
            if (_chave == null || _cripto == null)
                return null;

            _ultimaAtividade = DateTime.UtcNow;
            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha == null)
                return null;

            return new Credencial(senha.Usuario, _cripto.Descriptografar(senha.SenhaHash));
        }
    }

    private void VerificarInatividade()
    {
        lock (_trava)
        {
            if (_chave != null && DateTime.UtcNow - _ultimaAtividade > _tempoInatividade)
                TrancarInterno();
        }
    }

    private void TrancarInterno()
    {
        ApagarChave();
        _cripto = null;
        _senhas = new List<Senha>();
    }

    private void ApagarChave()
    {
        if (_chave != null)
        {
            CryptographicOperations.ZeroMemory(_chave);
            _chave = null;
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        Trancar();
    }
}
