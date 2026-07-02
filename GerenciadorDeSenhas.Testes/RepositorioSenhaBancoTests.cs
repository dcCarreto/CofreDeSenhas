using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class RepositorioSenhaBancoTests : IDisposable
{
    private readonly string _arquivo;
    private readonly ConexaoBanco _cfg;
    private readonly ServicoBancoDados _bd = new();
    private readonly IServicoCriptografia _criptografia;

    public RepositorioSenhaBancoTests()
    {
        _arquivo = Path.Combine(Path.GetTempPath(), "GS_RepoBanco_" + Guid.NewGuid().ToString("N") + ".db");
        _cfg = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = _arquivo };

        var chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(chave);
        _criptografia = new ServicoCriptografia(chave);

        _bd.CriarTabelaAsync(_cfg).GetAwaiter().GetResult();
    }

    private Senha NovaSenha(string dominio, string usuario, string plaintext) => new()
    {
        NomeServico = dominio,
        Usuario = usuario,
        SenhaHash = _criptografia.Criptografar(plaintext),
        Categoria = Categoria.Other
    };

    [Fact]
    public async Task Adicionar_GravaEConta()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        await repo.AdicionarAsync(NovaSenha("gmail.com", "user@gmail.com", "segredo"));

        Assert.Equal(1, await repo.ContarAsync());
    }

    [Fact]
    public async Task Adicionar_PersisteEntreInstancias_ComSenhaCifradaIntacta()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        await repo.AdicionarAsync(NovaSenha("github.com", "dev", "minhaSenha!"));

        var outro = new RepositorioSenhaBanco(_cfg);
        var todas = await outro.ListarTodosAsync();

        Assert.Single(todas);
        Assert.Equal("github.com", todas[0].NomeServico);
        Assert.Equal("dev", todas[0].Usuario);
        Assert.Equal("minhaSenha!", _criptografia.Descriptografar(todas[0].SenhaHash));
    }

    [Fact]
    public async Task Adicionar_PersisteDescricaoNasNotas()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("app.com", "u", "s");
        senha.Notas = "conta principal";
        await repo.AdicionarAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();

        Assert.Single(todas);
        Assert.Equal("conta principal", todas[0].Notas);
    }

    [Fact]
    public async Task Atualizar_MudaDominioEUsuario()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("antigo.com", "antigo", "x");
        await repo.AdicionarAsync(senha);

        senha.NomeServico = "novo.com";
        senha.Usuario = "novo";
        await repo.AtualizarAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();
        Assert.Single(todas);
        Assert.Equal("novo.com", todas[0].NomeServico);
        Assert.Equal("novo", todas[0].Usuario);
    }

    [Fact]
    public async Task Remover_FazExclusaoLogica()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("site.com", "u", "s");
        await repo.AdicionarAsync(senha);

        await repo.RemoverAsync(senha.Id);

        Assert.Equal(0, await repo.ContarAsync());
        Assert.Empty(await new RepositorioSenhaBanco(_cfg).ListarTodosAsync());

        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE excluido = 1"));
    }

    [Fact]
    public async Task BuscarPorServico_EncontraPorDominio()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        await repo.AdicionarAsync(NovaSenha("Netflix", "a", "1"));
        await repo.AdicionarAsync(NovaSenha("Spotify", "b", "2"));

        var achados = await repo.BuscarPorServicoAsync("flix");

        Assert.Single(achados);
        Assert.Equal("Netflix", achados[0].NomeServico);
    }

    [Fact]
    public async Task GravarPorChave_InsereEDepoisAtualiza()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        await repo.GravarPorChaveAsync(NovaSenha("site.com", "u", "v1"));
        Assert.Single(await new RepositorioSenhaBanco(_cfg).ListarTodosAsync());

        var atualizada = NovaSenha("site.com", "u", "v2");
        await repo.GravarPorChaveAsync(atualizada);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();
        Assert.Single(todas);
        Assert.Equal(atualizada.SenhaHash, todas[0].SenhaHash);
    }

    [Fact]
    public async Task ExcluirPorChave_FazExclusaoLogica()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        await repo.GravarPorChaveAsync(NovaSenha("x.com", "u", "p"));

        await repo.ExcluirPorChaveAsync("x.com", "u");

        Assert.Empty(await new RepositorioSenhaBanco(_cfg).ListarTodosAsync());
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
