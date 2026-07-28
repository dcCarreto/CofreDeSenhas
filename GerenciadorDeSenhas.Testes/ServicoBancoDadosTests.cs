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
    public void MontarStringConexao_PostgreSQL_PorPadraoAceitaCertificadoAutoassinado()
    {
        var cfg = ConexaoServidor(TipoBanco.PostgreSQL, exigirCertificado: false);

        var str = _bd.MontarStringConexao(cfg);

        Assert.Contains("Prefer", str);
    }

    [Fact]
    public void MontarStringConexao_PostgreSQL_ComExigirCertificado_ValidaCertificado()
    {
        var cfg = ConexaoServidor(TipoBanco.PostgreSQL, exigirCertificado: true);

        var str = _bd.MontarStringConexao(cfg);

        Assert.Contains("VerifyFull", str);
    }

    [Fact]
    public void MontarStringConexao_MySQL_PorPadraoAceitaCertificadoAutoassinado()
    {
        var cfg = ConexaoServidor(TipoBanco.MySQL, exigirCertificado: false);

        var str = _bd.MontarStringConexao(cfg);

        Assert.Contains("Preferred", str);
    }

    [Fact]
    public void MontarStringConexao_MySQL_ComExigirCertificado_ValidaCertificado()
    {
        var cfg = ConexaoServidor(TipoBanco.MySQL, exigirCertificado: true);

        var str = _bd.MontarStringConexao(cfg);

        Assert.Contains("VerifyFull", str);
    }

    [Fact]
    public void MontarStringConexao_SqlServer_PorPadraoConfiaNoCertificadoSemValidar()
    {
        var cfg = ConexaoServidor(TipoBanco.SqlServer, exigirCertificado: false);

        var str = _bd.MontarStringConexao(cfg);

        Assert.Contains("Trust Server Certificate=True", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Encrypt=True", str, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MontarStringConexao_SqlServer_ComExigirCertificado_NaoConfiaSemValidar()
    {
        var cfg = ConexaoServidor(TipoBanco.SqlServer, exigirCertificado: true);

        var str = _bd.MontarStringConexao(cfg);

        Assert.Contains("Trust Server Certificate=False", str, StringComparison.OrdinalIgnoreCase);
    }

    private static ConexaoBanco ConexaoServidor(TipoBanco tipo, bool exigirCertificado) => new()
    {
        Tipo = tipo,
        Host = "meuhost",
        Porta = 1234,
        Banco = "meubanco",
        Usuario = "meuusuario",
        SenhaServidor = "segredo",
        ExigirCertificadoValido = exigirCertificado
    };

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

    [Theory]
    [InlineData("url")]
    [InlineData("categoria")]
    [InlineData("tipo")]
    [InlineData("campos_extras")]
    [InlineData("historico")]
    [InlineData("favorito")]
    [InlineData("fixado")]
    public async Task GarantirColunas_AdicionaCamposDoFechamentoDeLacunaEmTabelaAntiga(string coluna)
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
        Assert.False(await ColunaExiste(coluna));

        await _bd.GarantirColunasAsync(_sqlite);

        Assert.True(await ColunaExiste(coluna));
    }

    [Fact]
    public async Task GarantirColunas_ChamadaDuasVezesNaoFalha()
    {
        await _bd.CriarTabelaAsync(_sqlite);

        await _bd.GarantirColunasAsync(_sqlite);
        await _bd.GarantirColunasAsync(_sqlite);

        Assert.True(await ColunaExiste("url"));
    }

    [Fact]
    public async Task GarantirColunas_PreencheGuidIdEmLinhaAntigaEDevolveOId()
    {
        long idInserido;
        await using (var con = _bd.CriarConexao(_sqlite))
        {
            await con.OpenAsync();
            await using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE CofreDeSenhas (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                  "usuario TEXT NOT NULL, senha TEXT NOT NULL, dominio TEXT, " +
                                  "excluido INTEGER NOT NULL DEFAULT 0)";
                await cmd.ExecuteNonQueryAsync();
            }
            await using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO CofreDeSenhas (usuario, senha, dominio) VALUES ('u', 's', 'legado.com')";
                await cmd.ExecuteNonQueryAsync();
            }
            await using var busca = con.CreateCommand();
            busca.CommandText = "SELECT last_insert_rowid()";
            idInserido = Convert.ToInt64(await busca.ExecuteScalarAsync());
        }

        var preenchidos = await _bd.GarantirColunasAsync(_sqlite);

        Assert.Contains(idInserido, preenchidos);

        await using var conFinal = _bd.CriarConexao(_sqlite);
        await conFinal.OpenAsync();
        await using var cmdFinal = conFinal.CreateCommand();
        cmdFinal.CommandText = "SELECT guid_id FROM CofreDeSenhas WHERE id = @id";
        var p = cmdFinal.CreateParameter();
        p.ParameterName = "@id";
        p.Value = idInserido;
        cmdFinal.Parameters.Add(p);
        var guidTexto = (string?)await cmdFinal.ExecuteScalarAsync();

        Assert.NotNull(guidTexto);
        Assert.True(Guid.TryParse(guidTexto, out _));
    }

    [Fact]
    public async Task GarantirColunas_ChamadaDuasVezes_NaoPreencheDeNovoOQueJaTemGuid()
    {
        await _bd.CriarTabelaAsync(_sqlite);
        var primeiraChamada = await _bd.GarantirColunasAsync(_sqlite);

        await using (var con = _bd.CriarConexao(_sqlite))
        {
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO CofreDeSenhas (usuario, senha, dominio) VALUES ('u', 's', 'novo.com')";
            await cmd.ExecuteNonQueryAsync();
        }

        var segundaChamada = await _bd.GarantirColunasAsync(_sqlite);

        Assert.Empty(primeiraChamada);
        Assert.Single(segundaChamada);
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
