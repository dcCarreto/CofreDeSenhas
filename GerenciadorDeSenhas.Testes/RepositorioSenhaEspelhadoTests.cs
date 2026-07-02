using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class RepositorioSenhaEspelhadoTests : IDisposable
{
    private readonly string _arquivo;
    private readonly ConexaoBanco _cfg;
    private readonly ServicoBancoDados _bd = new();
    private readonly byte[] _chave;
    private readonly IServicoCriptografia _cripto;

    public RepositorioSenhaEspelhadoTests()
    {
        _arquivo = Path.Combine(Path.GetTempPath(), "GS_Espelho_" + Guid.NewGuid().ToString("N") + ".db");
        _cfg = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = _arquivo };

        _chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(_chave);
        _cripto = new ServicoCriptografia(_chave);

        _bd.CriarTabelaAsync(_cfg).GetAwaiter().GetResult();
    }

    private RepositorioSenha NovoLocal() => new(new PersistenciaEmMemoria(), _chave);
    private RepositorioSenhaBanco NovoBanco() => new(_cfg);

    private Senha Nova(string dominio, string usuario, string plaintext) => new()
    {
        NomeServico = dominio,
        Usuario = usuario,
        SenhaHash = _cripto.Criptografar(plaintext),
        Categoria = Categoria.Other
    };

    [Fact]
    public async Task Mesclar_UneOsDoisLados_LocalVenceEmConflito()
    {
        var local = NovoLocal();
        await local.AdicionarAsync(Nova("gmail", "u1", "local1"));
        await local.AdicionarAsync(Nova("github", "u2", "LOCALvence"));

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("github", "u2", "bancoPerde"));
        await banco.AdicionarAsync(Nova("spotify", "u3", "s3"));

        var espelho = new RepositorioSenhaEspelhado(local, banco);
        var todas = await espelho.ListarTodosAsync();

        Assert.Equal(3, todas.Count);
        Assert.Contains(todas, s => s.NomeServico == "spotify");

        var noBanco = await NovoBanco().ListarTodosAsync();
        Assert.Contains(noBanco, s => s.NomeServico == "gmail");

        var githubBanco = noBanco.First(s => s.NomeServico == "github");
        Assert.Equal("LOCALvence", _cripto.Descriptografar(githubBanco.SenhaHash));
    }

    [Fact]
    public async Task Adicionar_GravaNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());

        await espelho.AdicionarAsync(Nova("netflix", "u4", "n4"));

        Assert.Single(await local.ListarTodosAsync());
        Assert.Contains(await NovoBanco().ListarTodosAsync(), s => s.NomeServico == "netflix");
    }

    [Fact]
    public async Task Remover_ExcluiNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var senha = Nova("app", "u", "s");
        await espelho.AdicionarAsync(senha);

        await espelho.RemoverAsync(senha.Id);

        Assert.Empty(await local.ListarTodosAsync());
        Assert.Empty(await NovoBanco().ListarTodosAsync());
    }

    public void Dispose()
    {
        try { if (File.Exists(_arquivo)) File.Delete(_arquivo); } catch { }
    }
}
