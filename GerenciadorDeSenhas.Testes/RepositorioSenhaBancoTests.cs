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
        _bd.GarantirColunasAsync(_cfg).GetAwaiter().GetResult();
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

        Assert.Single(await repo.ListarTodosAsync());
    }

    [Fact]
    public async Task Adicionar_PersisteDataCriacaoEDataAtualizacaoEntreInstancias()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("gmail.com", "user@gmail.com", "segredo");
        await repo.AdicionarAsync(senha);

        var outro = new RepositorioSenhaBanco(_cfg);
        var carregada = (await outro.ListarTodosAsync()).Single();

        Assert.True((DateTime.UtcNow - carregada.DataCriacao) < TimeSpan.FromMinutes(1));
        Assert.True((DateTime.UtcNow - carregada.DataAtualizacao) < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Atualizar_AtualizaDataAtualizacaoSemMudarDataCriacao()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("gmail.com", "user@gmail.com", "segredo");
        await repo.AdicionarAsync(senha);

        var original = (await new RepositorioSenhaBanco(_cfg).ListarTodosAsync()).Single();

        senha.DataAtualizacao = DateTime.UtcNow.AddDays(1);
        await repo.AtualizarAsync(senha);

        var atualizada = (await new RepositorioSenhaBanco(_cfg).ListarTodosAsync()).Single();

        Assert.Equal(senha.DataAtualizacao, atualizada.DataAtualizacao, TimeSpan.FromSeconds(1));
        Assert.Equal(original.DataCriacao, atualizada.DataCriacao, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RegistrarCopia_PersisteApenasACampoIndicadoEntreInstancias()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("gmail.com", "user@gmail.com", "segredo");
        await repo.AdicionarAsync(senha);

        await repo.RegistrarCopiaAsync(senha.Id, TipoCampoCopiado.Usuario);

        var carregada = (await new RepositorioSenhaBanco(_cfg).ListarTodosAsync()).Single();

        Assert.NotNull(carregada.DataUltimaCopiaUsuario);
        Assert.Null(carregada.DataUltimaCopiaSenha);
        Assert.Null(carregada.DataUltimaCopiaTotp);
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
    public async Task Adicionar_PersisteSegredoTotp()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("app.com", "u", "s");
        senha.TotpSegredo = _criptografia.Criptografar("JBSWY3DPEHPK3PXP");
        await repo.AdicionarAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();

        Assert.Single(todas);
        Assert.NotNull(todas[0].TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(todas[0].TotpSegredo!));
    }

    [Fact]
    public async Task Adicionar_PersisteCodigosRecuperacaoEntreInstancias()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("app.com", "u", "s");
        senha.CodigosRecuperacao.Add(new CodigoRecuperacao { Codigo = _criptografia.Criptografar("ABCD-1234") });
        senha.CodigosRecuperacao.Add(new CodigoRecuperacao { Codigo = _criptografia.Criptografar("EFGH-5678"), Usado = true });
        await repo.AdicionarAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();

        Assert.Single(todas);
        Assert.Equal(2, todas[0].CodigosRecuperacao.Count);
        Assert.Equal("ABCD-1234", _criptografia.Descriptografar(todas[0].CodigosRecuperacao[0].Codigo));
        Assert.False(todas[0].CodigosRecuperacao[0].Usado);
        Assert.Equal("EFGH-5678", _criptografia.Descriptografar(todas[0].CodigosRecuperacao[1].Codigo));
        Assert.True(todas[0].CodigosRecuperacao[1].Usado);
    }

    [Fact]
    public async Task Atualizar_PersisteMudancaNosCodigosRecuperacao()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("app.com", "u", "s");
        await repo.AdicionarAsync(senha);

        senha.CodigosRecuperacao.Add(new CodigoRecuperacao { Codigo = _criptografia.Criptografar("ABCD-1234") });
        await repo.AtualizarAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();
        Assert.Single(todas[0].CodigosRecuperacao);
        Assert.Equal("ABCD-1234", _criptografia.Descriptografar(todas[0].CodigosRecuperacao[0].Codigo));
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

        Assert.Empty(await repo.ListarTodosAsync());
        Assert.Empty(await new RepositorioSenhaBanco(_cfg).ListarTodosAsync());

        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE excluido = 1"));
    }

    [Fact]
    public async Task Remover_ApareceNaLixeiraComDataDeExclusao()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("site.com", "u", "s");
        await repo.AdicionarAsync(senha);

        await repo.RemoverAsync(senha.Id);

        var lixeira = await repo.ListarLixeiraAsync();
        Assert.Single(lixeira);
        Assert.True(lixeira[0].NaLixeira);
        Assert.NotNull(lixeira[0].DataExclusao);

        var novaInstancia = await new RepositorioSenhaBanco(_cfg).ListarLixeiraAsync();
        Assert.Single(novaInstancia);
        Assert.NotNull(novaInstancia[0].DataExclusao);
    }

    [Fact]
    public async Task Restaurar_TrazDeVoltaEApagaDataDeExclusao()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("site.com", "u", "s");
        await repo.AdicionarAsync(senha);
        await repo.RemoverAsync(senha.Id);

        await repo.RestaurarAsync(senha.Id);

        Assert.Single(await repo.ListarTodosAsync());
        Assert.Empty(await repo.ListarLixeiraAsync());

        var novaInstancia = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();
        Assert.Single(novaInstancia);
        Assert.Null(novaInstancia[0].DataExclusao);
    }

    [Fact]
    public async Task RemoverDefinitivamente_ApagaALinhaDoBanco()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("site.com", "u", "s");
        await repo.AdicionarAsync(senha);
        await repo.RemoverAsync(senha.Id);

        await repo.RemoverDefinitivamenteAsync(senha.Id);

        Assert.Empty(await repo.ListarLixeiraAsync());
        Assert.Equal(0, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
    }

    [Fact]
    public async Task EsvaziarLixeira_RemoveSomenteAsLinhasExcluidas()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var ativa = NovaSenha("ativa.com", "u", "s");
        var excluida = NovaSenha("excluida.com", "u", "s");
        await repo.AdicionarAsync(ativa);
        await repo.AdicionarAsync(excluida);
        await repo.RemoverAsync(excluida.Id);

        await repo.EsvaziarLixeiraAsync();

        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
        Assert.Single(await repo.ListarTodosAsync());
    }

    [Fact]
    public async Task ExcluirDefinitivamentePorChave_ApagaALinhaDoBanco()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("x.com", "u", "p");
        await repo.GravarPorChaveAsync(senha);

        await repo.ExcluirDefinitivamentePorChaveAsync(senha.Id);

        Assert.Equal(0, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
    }

    [Fact]
    public async Task GravarPorChave_InsereEDepoisAtualiza()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("site.com", "u", "v1");
        await repo.GravarPorChaveAsync(senha);
        Assert.Single(await new RepositorioSenhaBanco(_cfg).ListarTodosAsync());

        senha.SenhaHash = _criptografia.Criptografar("v2");
        await repo.GravarPorChaveAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();
        Assert.Single(todas);
        Assert.Equal(senha.SenhaHash, todas[0].SenhaHash);
    }

    [Fact]
    public async Task GravarPorChave_DuasCredenciaisComMesmoDominioEUsuario_NaoSeSobrescrevem()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var primeira = NovaSenha("site.com", "u", "v1");
        var segunda = NovaSenha("site.com", "u", "v2");

        await repo.GravarPorChaveAsync(primeira);
        await repo.GravarPorChaveAsync(segunda);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();
        Assert.Equal(2, todas.Count);
        Assert.Contains(todas, s => s.Id == primeira.Id);
        Assert.Contains(todas, s => s.Id == segunda.Id);
    }

    [Fact]
    public async Task ExcluirPorChave_FazExclusaoLogica()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("x.com", "u", "p");
        await repo.GravarPorChaveAsync(senha);

        await repo.ExcluirPorChaveAsync(senha.Id);

        Assert.Empty(await new RepositorioSenhaBanco(_cfg).ListarTodosAsync());
    }

    [Fact]
    public async Task Adicionar_PersisteUrlCategoriaTipoFavoritoEFixadoEntreInstancias()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("banco.com", "u", "s");
        senha.Url = "https://banco.com/login";
        senha.Categoria = Categoria.Finance;
        senha.Tipo = TipoCredencial.Cartao;
        senha.Favorito = true;
        senha.Fixado = true;
        await repo.AdicionarAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();

        Assert.Single(todas);
        Assert.Equal("https://banco.com/login", todas[0].Url);
        Assert.Equal(Categoria.Finance, todas[0].Categoria);
        Assert.Equal(TipoCredencial.Cartao, todas[0].Tipo);
        Assert.True(todas[0].Favorito);
        Assert.True(todas[0].Fixado);
    }

    [Fact]
    public async Task Adicionar_PersisteCamposExtrasCifradosEntreInstancias()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("banco.com", "u", "s");
        senha.CamposExtras["cvv"] = _criptografia.Criptografar("123");
        await repo.AdicionarAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();

        Assert.Single(todas);
        Assert.Equal("123", _criptografia.Descriptografar(todas[0].CamposExtras["cvv"]));
    }

    [Fact]
    public async Task Adicionar_PersisteHistoricoEntreInstancias()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("banco.com", "u", "s");
        senha.Historico.Add(new HistoricoSenha { SenhaHash = _criptografia.Criptografar("antiga"), DataAlteracao = DateTime.UtcNow });
        await repo.AdicionarAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();

        Assert.Single(todas);
        var anterior = Assert.Single(todas[0].Historico);
        Assert.Equal("antiga", _criptografia.Descriptografar(anterior.SenhaHash));
    }

    [Fact]
    public async Task GravarPorChave_InsereComUrlCategoriaTipoFavoritoEFixado()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("wifi.com", "u", "s");
        senha.Url = "https://wifi.com";
        senha.Categoria = Categoria.Work;
        senha.Tipo = TipoCredencial.WiFi;
        senha.Favorito = true;
        senha.Fixado = true;
        await repo.GravarPorChaveAsync(senha);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarTodosAsync();

        Assert.Single(todas);
        Assert.Equal("https://wifi.com", todas[0].Url);
        Assert.Equal(Categoria.Work, todas[0].Categoria);
        Assert.Equal(TipoCredencial.WiFi, todas[0].Tipo);
        Assert.True(todas[0].Favorito);
        Assert.True(todas[0].Fixado);
    }

    [Fact]
    public async Task Adicionar_PreservaOMesmoGuidEntreInstancias()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("estavel.com", "u", "s");
        await repo.AdicionarAsync(senha);

        var carregada = (await new RepositorioSenhaBanco(_cfg).ListarTodosAsync()).Single();

        Assert.Equal(senha.Id, carregada.Id);
    }

    [Fact]
    public async Task SubstituirGuid_TrocaOIdentificadorEPreservaALinha()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("legado.com", "u", "s");
        await repo.AdicionarAsync(senha);

        var novoGuid = Guid.NewGuid();
        await repo.SubstituirGuidAsync(senha.Id, novoGuid);

        var carregada = (await new RepositorioSenhaBanco(_cfg).ListarTodosAsync()).Single();
        Assert.Equal(novoGuid, carregada.Id);
        Assert.Equal("legado.com", carregada.NomeServico);
    }

    [Fact]
    public async Task GravarPorChave_AtualizarPreservaDataAtualizacaoEExcluidoENovosCampos()
    {
        var repo = new RepositorioSenhaBanco(_cfg);
        var senha = NovaSenha("site.com", "u", "v1");
        await repo.GravarPorChaveAsync(senha);

        var atualizada = NovaSenha("site.com", "u", "v2");
        atualizada.Id = senha.Id;
        atualizada.NaLixeira = true;
        atualizada.DataExclusao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        atualizada.DataAtualizacao = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        atualizada.Favorito = true;
        atualizada.Fixado = true;
        await repo.GravarPorChaveAsync(atualizada);

        var todas = await new RepositorioSenhaBanco(_cfg).ListarLixeiraAsync();

        Assert.Single(todas);
        Assert.True(todas[0].NaLixeira);
        Assert.Equal(atualizada.DataExclusao, todas[0].DataExclusao);
        Assert.Equal(atualizada.DataAtualizacao, todas[0].DataAtualizacao);
        Assert.True(todas[0].Favorito);
        Assert.True(todas[0].Fixado);
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
