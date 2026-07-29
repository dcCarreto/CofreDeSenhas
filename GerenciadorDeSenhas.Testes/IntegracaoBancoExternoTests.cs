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

    private static ConexaoBanco Conexao(TipoBanco tipo) => tipo switch
    {
        TipoBanco.PostgreSQL => new ConexaoBanco
        {
            Tipo = tipo, Host = "localhost", Porta = 5432, Banco = "cofretest",
            Usuario = "postgres", SenhaServidor = "postgres"
        },
        TipoBanco.MySQL => new ConexaoBanco
        {
            Tipo = tipo, Host = "localhost", Porta = 3306, Banco = "cofretest",
            Usuario = "root", SenhaServidor = "root"
        },
        TipoBanco.SqlServer => new ConexaoBanco
        {
            Tipo = tipo, Host = "localhost", Porta = 1433, Banco = "master",
            Usuario = "sa", SenhaServidor = "CofreDeSenhas!Teste123"
        },
        _ => throw new NotSupportedException($"Sem conexão de teste para {tipo}")
    };

    private static async Task LimparTabelaAsync(ServicoBancoDados bd, ConexaoBanco cfg)
    {
        await using var con = bd.CriarConexao(cfg);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {ServicoBancoDados.NomeTabela}";
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
}
