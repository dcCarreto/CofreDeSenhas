using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

[CollectionDefinition("IntegracaoBanco", DisableParallelization = true)]
public class ColecaoIntegracaoBanco { }

// Roda contra Postgres/MySQL/SQL Server de verdade, não SQLite — por isso exige os
// service containers do job "testar-bancos" em .github/workflows/ci.yml (localhost,
// credenciais fixas de teste, sem segredo real nenhum aqui). Fora desse job, o filtro
// --filter "Category!=IntegracaoBanco" nos outros jobs mantém esta classe de fora,
// já que os motores não vão estar disponíveis em localhost ali.
//
// Todos os métodos usam a mesma tabela física (ServicoBancoDados.NomeTabela) sem
// nenhum isolamento entre si — [Collection] com DisableParallelization força esses
// testes a rodar em sequência; sem isso, um teste dropando/recriando a tabela no meio
// do INSERT+SCOPE_IDENTITY() de outro corrompia a leitura do id no SQL Server.
[Collection("IntegracaoBanco")]
[Trait("Category", "IntegracaoBanco")]
public class IntegracaoBancoExternoTests
{
    public static TheoryData<TipoBanco> Motores => new()
    {
        TipoBanco.PostgreSQL,
        TipoBanco.MySQL,
        TipoBanco.SqlServer
    };

    // Cada campo aceita override por variável de ambiente (COFRE_TESTE_<MOTOR>_*),
    // sem valor nenhum fixado aqui além dos defaults de CI (localhost, credenciais
    // fixas de teste, sem segredo real). Isso permite apontar um motor pra uma
    // instância de verdade numa rodada manual local (ex.: um MySQL hospedado) sem
    // tocar neste arquivo nem versionar segredo nenhum — só setar a variável na
    // mesma invocação do comando que roda os testes.
    private static ConexaoBanco Conexao(TipoBanco tipo) => tipo switch
    {
        TipoBanco.PostgreSQL => new ConexaoBanco
        {
            Tipo = tipo,
            Host = VariavelOu("COFRE_TESTE_POSTGRES_HOST", "localhost"),
            Porta = VariavelInt("COFRE_TESTE_POSTGRES_PORTA", 5432),
            Banco = VariavelOu("COFRE_TESTE_POSTGRES_BANCO", "cofretest"),
            Usuario = VariavelOu("COFRE_TESTE_POSTGRES_USUARIO", "postgres"),
            SenhaServidor = VariavelOu("COFRE_TESTE_POSTGRES_SENHA", "postgres")
        },
        TipoBanco.MySQL => new ConexaoBanco
        {
            Tipo = tipo,
            Host = VariavelOu("COFRE_TESTE_MYSQL_HOST", "localhost"),
            Porta = VariavelInt("COFRE_TESTE_MYSQL_PORTA", 3306),
            Banco = VariavelOu("COFRE_TESTE_MYSQL_BANCO", "cofretest"),
            Usuario = VariavelOu("COFRE_TESTE_MYSQL_USUARIO", "root"),
            SenhaServidor = VariavelOu("COFRE_TESTE_MYSQL_SENHA", "root")
        },
        TipoBanco.SqlServer => new ConexaoBanco
        {
            Tipo = tipo,
            Host = VariavelOu("COFRE_TESTE_SQLSERVER_HOST", "localhost"),
            Porta = VariavelInt("COFRE_TESTE_SQLSERVER_PORTA", 1433),
            Banco = VariavelOu("COFRE_TESTE_SQLSERVER_BANCO", "master"),
            Usuario = VariavelOu("COFRE_TESTE_SQLSERVER_USUARIO", "sa"),
            SenhaServidor = VariavelOu("COFRE_TESTE_SQLSERVER_SENHA", "CofreDeSenhas!Teste123")
        },
        _ => throw new NotSupportedException($"Sem conexão de teste para {tipo}")
    };

    private static string VariavelOu(string nome, string padrao) =>
        Environment.GetEnvironmentVariable(nome) is string valor && valor.Length > 0 ? valor : padrao;

    private static int VariavelInt(string nome, int padrao) =>
        int.TryParse(Environment.GetEnvironmentVariable(nome), out var valor) ? valor : padrao;

    private static async Task LimparTabelaAsync(ServicoBancoDados bd, ConexaoBanco cfg)
    {
        await using var con = bd.CriarConexao(cfg);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {ServicoBancoDados.NomeTabela}";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task LimparTabelaAuthAsync(ServicoBancoDados bd, ConexaoBanco cfg)
    {
        await using var con = bd.CriarConexao(cfg);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {ServicoBancoDados.NomeTabelaAuth}";
        await cmd.ExecuteNonQueryAsync();
    }

    [Theory]
    [MemberData(nameof(Motores))]
    public async Task CriarTabelaEGarantirColunas_FuncionaContraOMotorReal(TipoBanco tipo)
    {
        var cfg = Conexao(tipo);
        var bd = new ServicoBancoDados();
        await LimparTabelaAsync(bd, cfg);

        Assert.False(await bd.TabelaExisteAsync(cfg));
        await bd.CriarTabelaAsync(cfg);
        Assert.True(await bd.TabelaExisteAsync(cfg));

        await bd.GarantirColunasAsync(cfg);
        // Chamar de novo não pode falhar (colunas já existem).
        await bd.GarantirColunasAsync(cfg);
    }

    [Theory]
    [MemberData(nameof(Motores))]
    public async Task Crud_ArmazenaAtualizaEExcluiContraOMotorReal(TipoBanco tipo)
    {
        var cfg = Conexao(tipo);
        var bd = new ServicoBancoDados();
        await LimparTabelaAsync(bd, cfg);
        await bd.CriarTabelaAsync(cfg);
        await bd.GarantirColunasAsync(cfg);

        var repo = new RepositorioSenhaBanco(cfg);
        var senha = new Senha
        {
            NomeServico = "servico.teste",
            Usuario = "usuario.teste",
            SenhaHash = "cifrado-v1",
            Categoria = Categoria.Personal
        };
        await repo.AdicionarAsync(senha);

        var lida = Assert.Single(await new RepositorioSenhaBanco(cfg).ListarTodosAsync());
        Assert.Equal("servico.teste", lida.NomeServico);
        Assert.Equal(senha.Id, lida.Id);

        var repoAtualizar = new RepositorioSenhaBanco(cfg);
        var paraAtualizar = Assert.Single(await repoAtualizar.ListarTodosAsync());
        paraAtualizar.SenhaHash = "cifrado-v2";
        await repoAtualizar.AtualizarAsync(paraAtualizar);

        var atualizada = Assert.Single(await new RepositorioSenhaBanco(cfg).ListarTodosAsync());
        Assert.Equal("cifrado-v2", atualizada.SenhaHash);

        var repoExcluir = new RepositorioSenhaBanco(cfg);
        await repoExcluir.RemoverAsync(senha.Id);
        Assert.Empty(await new RepositorioSenhaBanco(cfg).ListarTodosAsync());
        Assert.Single(await new RepositorioSenhaBanco(cfg).ListarLixeiraAsync());

        var repoRestaurar = new RepositorioSenhaBanco(cfg);
        await repoRestaurar.RestaurarAsync(senha.Id);
        Assert.Single(await new RepositorioSenhaBanco(cfg).ListarTodosAsync());

        var repoDefinitivo = new RepositorioSenhaBanco(cfg);
        await repoDefinitivo.RemoverDefinitivamenteAsync(senha.Id);
        Assert.Empty(await new RepositorioSenhaBanco(cfg).ListarTodosAsync());
        Assert.Empty(await new RepositorioSenhaBanco(cfg).ListarLixeiraAsync());
    }

    [Theory]
    [MemberData(nameof(Motores))]
    public async Task GravarPorChave_DuasCredenciaisComMesmoDominioEUsuario_NaoSeSobrescrevemNoMotorReal(TipoBanco tipo)
    {
        var cfg = Conexao(tipo);
        var bd = new ServicoBancoDados();
        await LimparTabelaAsync(bd, cfg);
        await bd.CriarTabelaAsync(cfg);
        await bd.GarantirColunasAsync(cfg);

        var repo = new RepositorioSenhaBanco(cfg);
        var primeira = new Senha { NomeServico = "site.com", Usuario = "u", SenhaHash = "v1", Categoria = Categoria.Other };
        var segunda = new Senha { NomeServico = "site.com", Usuario = "u", SenhaHash = "v2", Categoria = Categoria.Other };

        await repo.GravarPorChaveAsync(primeira);
        await repo.GravarPorChaveAsync(segunda);

        var todas = await new RepositorioSenhaBanco(cfg).ListarTodosAsync();
        Assert.Equal(2, todas.Count);
        Assert.Contains(todas, s => s.Id == primeira.Id);
        Assert.Contains(todas, s => s.Id == segunda.Id);
    }

    [Theory]
    [MemberData(nameof(Motores))]
    public async Task TabelaAuth_AntesDePublicar_LerAuthRetornaNull(TipoBanco tipo)
    {
        var cfg = Conexao(tipo);
        var bd = new ServicoBancoDados();
        await LimparTabelaAuthAsync(bd, cfg);

        Assert.False(await bd.TabelaAuthExisteAsync(cfg));
        Assert.Null(await bd.LerAuthAsync(cfg));
    }

    [Theory]
    [MemberData(nameof(Motores))]
    public async Task TabelaAuth_PublicarELer_DevolveExatamenteOQueFoiPublicadoNoMotorReal(TipoBanco tipo)
    {
        var cfg = Conexao(tipo);
        var bd = new ServicoBancoDados();
        await LimparTabelaAuthAsync(bd, cfg);
        await bd.CriarTabelaAuthAsync(cfg);

        var publicado = new AuthBanco(
            Salt: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
            Verificador: Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(),
            Kdf: 1,
            Custo: 3,
            MemoriaKb: 65536,
            Paralelismo: 1);
        await bd.PublicarAuthAsync(cfg, publicado);

        var lido = await bd.LerAuthAsync(cfg);

        Assert.NotNull(lido);
        Assert.Equal(publicado.Salt, lido!.Salt);
        Assert.Equal(publicado.Verificador, lido.Verificador);
        Assert.Equal(publicado.Kdf, lido.Kdf);
        Assert.Equal(publicado.Custo, lido.Custo);
        Assert.Equal(publicado.MemoriaKb, lido.MemoriaKb);
        Assert.Equal(publicado.Paralelismo, lido.Paralelismo);
    }

    [Theory]
    [MemberData(nameof(Motores))]
    public async Task TabelaAuth_PublicarDuasVezes_SegundaChamadaAtualizaOPrimeiroPublicado(TipoBanco tipo)
    {
        // PublicarAuthAsync é um upsert de propósito (ver ServicoBancoDados.cs): o
        // dado mais recente prevalece. É o que permite trocar a senha mestra com o
        // banco já conectado republicar salt/verificador novos por cima do que já
        // estava lá — sem isso, a troca nunca se propagaria pro banco depois da
        // primeira conexão. Mesmo cenário e mesma expectativa de
        // ServicoBancoDadosTests.PublicarAuth_ChamadoDuasVezes_SegundaChamadaAtualizaOPrimeiroPublicado,
        // só que aqui contra os motores reais em vez de SQLite local.
        var cfg = Conexao(tipo);
        var bd = new ServicoBancoDados();
        await LimparTabelaAuthAsync(bd, cfg);
        await bd.CriarTabelaAuthAsync(cfg);

        var primeiro = new AuthBanco(new byte[16], new byte[32], 1, 3, 65536, 1);
        var segundo = new AuthBanco(Enumerable.Repeat((byte)9, 16).ToArray(), Enumerable.Repeat((byte)7, 32).ToArray(), 0, 5, 131072, 2);

        await bd.PublicarAuthAsync(cfg, primeiro);
        await bd.PublicarAuthAsync(cfg, segundo);

        var lido = await bd.LerAuthAsync(cfg);
        Assert.Equal(segundo.Salt, lido!.Salt);
        Assert.Equal(segundo.Verificador, lido.Verificador);
        Assert.Equal(segundo.Kdf, lido.Kdf);
        Assert.Equal(segundo.Custo, lido.Custo);
        Assert.Equal(segundo.MemoriaKb, lido.MemoriaKb);
        Assert.Equal(segundo.Paralelismo, lido.Paralelismo);
    }
}
