using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoBancoDadosTests : IDisposable
{
    private readonly ServicoBancoDados _bd = new();
    private readonly string _arquivo;
    private readonly ConexaoBanco _sqlite;

    public ServicoBancoDadosTests()
    {
        _arquivo = Path.Combine(Path.GetTempPath(), "GS_BD_" + Guid.NewGuid().ToString("N") + ".db");
        _sqlite = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = _arquivo };
    }

    [Fact]
    public async Task CriarTabela_QuandoNaoExiste_PassaAExistir()
    {
        Assert.False(await _bd.TabelaExisteAsync(_sqlite));

        await _bd.CriarTabelaAsync(_sqlite);

        Assert.True(await _bd.TabelaExisteAsync(_sqlite));
    }

    [Theory]
    [InlineData(TipoBanco.PostgreSQL)]
    [InlineData(TipoBanco.MySQL)]
    [InlineData(TipoBanco.SqlServer)]
    public void MontarStringConexao_Servidor_IncluiHostPortaBancoUsuario(TipoBanco tipo)
    {
        var cfg = new ConexaoBanco
        {
            Tipo = tipo,
            Host = "meuhost",
            Porta = 1234,
            Banco = "meubanco",
            Usuario = "meuusuario",
            SenhaServidor = "segredo"
        };

        var str = _bd.MontarStringConexao(cfg);

        Assert.Contains("meuhost", str);
        Assert.Contains("1234", str);
        Assert.Contains("meubanco", str);
        Assert.Contains("meuusuario", str);
    }

    [Fact]
    public void MontarStringConexao_SQLite_IncluiCaminhoDoArquivo()
    {
        var str = _bd.MontarStringConexao(_sqlite);

        Assert.Contains(_arquivo, str);
    }

    [Fact]
    public async Task CriarTabela_JaIncluiColunaDescricao()
    {
        await _bd.CriarTabelaAsync(_sqlite);

        Assert.True(await ColunaExiste("descricao"));
    }

    [Fact]
    public async Task CriarTabela_JaIncluiColunaTotp()
    {
        await _bd.CriarTabelaAsync(_sqlite);

        Assert.True(await ColunaExiste("totp"));
    }

    [Fact]
    public async Task GarantirColunas_AdicionaDescricaoETotpEmTabelaAntiga()
    {
        await using (var con = _bd.CriarConexao(_sqlite))
        {
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "CREATE TABLE CofreDeSenhas (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                              "usuario TEXT NOT NULL, senha TEXT NOT NULL, dominio TEXT, " +
                              "excluido INTEGER NOT NULL DEFAULT 0)";
            await cmd.ExecuteNonQueryAsync();
        }
        Assert.False(await ColunaExiste("descricao"));
        Assert.False(await ColunaExiste("totp"));

        await _bd.GarantirColunasAsync(_sqlite);

        Assert.True(await ColunaExiste("descricao"));
        Assert.True(await ColunaExiste("totp"));
    }

    private async Task<bool> ColunaExiste(string coluna)
    {
        await using var con = _bd.CriarConexao(_sqlite);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('CofreDeSenhas') WHERE name = '{coluna}'";
        return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
    }

    public void Dispose()
    {
        try { if (File.Exists(_arquivo)) File.Delete(_arquivo); } catch { }
    }
}
