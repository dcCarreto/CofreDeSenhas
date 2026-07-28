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
        _bd.GarantirColunasAsync(_cfg).GetAwaiter().GetResult();
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

    private Senha NovaComId(Guid id, string dominio, string usuario, string plaintext, DateTime dataAtualizacao) => new()
    {
        Id = id,
        NomeServico = dominio,
        Usuario = usuario,
        SenhaHash = _cripto.Criptografar(plaintext),
        Categoria = Categoria.Other,
        DataAtualizacao = dataAtualizacao
    };

    [Fact]
    public async Task Mesclar_UneOsDoisLadosPorGuid_ItensSemConflitoTodosSobrevivem()
    {
        var local = NovoLocal();
        await local.AdicionarAsync(Nova("gmail", "u1", "local1"));

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("spotify", "u3", "s3"));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        Assert.Equal(2, todas.Count);
        Assert.Contains(todas, s => s.NomeServico == "spotify");

        var noBanco = await NovoBanco().ListarTodosAsync();
        Assert.Contains(noBanco, s => s.NomeServico == "gmail");
    }

    [Fact]
    public async Task Mesclar_MesmoGuidNosDoisLados_EdicaoMaisRecenteVence()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = NovoLocal();
        await local.AdicionarAsync(NovaComId(id, "github", "u2", "antiga", agora.AddMinutes(-10)));

        var banco = NovoBanco();
        await banco.AdicionarAsync(NovaComId(id, "github", "u2", "nova", agora));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        var item = Assert.Single(todas);
        Assert.Equal("nova", _cripto.Descriptografar(item.SenhaHash));
    }

    [Fact]
    public async Task Mesclar_GuidsDiferentesComMesmaChave_SobrevivemComoItensDistintos()
    {
        var local = NovoLocal();
        await local.AdicionarAsync(Nova("github", "u2", "doLocal"));

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("github", "u2", "doBanco"));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        Assert.Equal(2, todas.Count(s => s.NomeServico == "github"));
        Assert.Equal(2, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
    }

    [Fact]
    public async Task Mesclar_ComReconciliacaoLegadaPendente_AdotaGuidLocalENaoDuplica()
    {
        var local = NovoLocal();
        var itemLocal = Nova("github", "u2", "conteudo");
        await local.AdicionarAsync(itemLocal);

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("github", "u2", "conteudo"));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: false);
        var todas = await espelho.ListarTodosAsync();

        var item = Assert.Single(todas);
        Assert.Equal(itemLocal.Id, item.Id);
        Assert.True(espelho.ReconciliacaoRealizadaNestaSessao);

        var noBanco = await NovoBanco().ListarTodosAsync();
        var itemBanco = Assert.Single(noBanco);
        Assert.Equal(itemLocal.Id, itemBanco.Id);
    }

    [Fact]
    public async Task Mesclar_ComReconciliacaoJaFeita_NaoReconciliaMaisApesarDeChaveCoincidente()
    {
        var local = NovoLocal();
        await local.AdicionarAsync(Nova("github", "u2", "conteudo1"));

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("github", "u2", "conteudo1"));

        var primeiro = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: false);
        await primeiro.ListarTodosAsync();

        await local.AdicionarAsync(Nova("gitlab", "u9", "conteudo2"));
        await NovoBanco().AdicionarAsync(Nova("gitlab", "u9", "conteudo2"));

        var segundo = new RepositorioSenhaEspelhado(local, NovoBanco(), reconciliacaoJaRealizada: true);
        var todas = await segundo.ListarTodosAsync();

        Assert.Equal(2, todas.Count(s => s.NomeServico == "gitlab"));
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
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE excluido = 1"));
    }

    [Fact]
    public async Task Remover_ComDoisEspelhosNoMesmoBanco_ExclusaoNaoEhRevertida()
    {
        var id = Guid.NewGuid();
        var criada = DateTime.UtcNow.AddMinutes(-10);

        var localA = NovoLocal();
        var espelhoA = new RepositorioSenhaEspelhado(localA, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoA.AdicionarAsync(NovaComId(id, "app", "u", "s", criada));

        var localB = NovoLocal();
        var espelhoB1 = new RepositorioSenhaEspelhado(localB, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoB1.ListarTodosAsync();
        Assert.Single(await localB.ListarTodosAsync());

        await espelhoA.RemoverAsync(id);

        var espelhoB2 = new RepositorioSenhaEspelhado(localB, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoB2.ListarTodosAsync();

        Assert.Empty(await localB.ListarTodosAsync());
        Assert.Empty(await NovoBanco().ListarTodosAsync());
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE excluido = 1"));
    }

    [Fact]
    public async Task Restaurar_TrazDeVoltaNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var senha = Nova("app", "u", "s");
        await espelho.AdicionarAsync(senha);
        await espelho.RemoverAsync(senha.Id);

        await espelho.RestaurarAsync(senha.Id);

        Assert.Single(await local.ListarTodosAsync());
        Assert.Contains(await NovoBanco().ListarTodosAsync(), s => s.NomeServico == "app");
        Assert.Equal(0, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE excluido = 1"));
    }

    [Fact]
    public async Task RemoverDefinitivamente_ApagaNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var senha = Nova("app", "u", "s");
        await espelho.AdicionarAsync(senha);
        await espelho.RemoverAsync(senha.Id);

        await espelho.RemoverDefinitivamenteAsync(senha.Id);

        Assert.Empty(await local.ListarLixeiraAsync());
        Assert.Equal(0, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
    }

    [Fact]
    public async Task EsvaziarLixeira_ApagaNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var ativa = Nova("ativa", "u", "s");
        var excluida = Nova("excluida", "u", "s");
        await espelho.AdicionarAsync(ativa);
        await espelho.AdicionarAsync(excluida);
        await espelho.RemoverAsync(excluida.Id);

        await espelho.EsvaziarLixeiraAsync();

        Assert.Empty(await local.ListarLixeiraAsync());
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
    }

    private async Task<long> ContarLinhas(string sql)
    {
        await using var con = _bd.CriarConexao(_cfg);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    public void Dispose()
    {
        try { if (File.Exists(_arquivo)) File.Delete(_arquivo); } catch { }
    }
}
