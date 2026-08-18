using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Microsoft.Data.Sqlite;
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

    [Fact]
    public async Task CriarTabela_QuandoJaExiste_LancaErroLocalizavelComCausaOriginal()
    {
        await _bd.CriarTabelaAsync(_sqlite);

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() => _bd.CriarTabelaAsync(_sqlite));

        Assert.Equal("Db.Error.SchemaFailed", ex.Chave);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task TestarConexao_ComDiretorioInexistente_LancaErroLocalizavelComCausaOriginal()
    {
        var caminhoInvalido = Path.Combine(
            Path.GetTempPath(), "GS_BD_dir_inexistente_" + Guid.NewGuid().ToString("N"), "cofre.db");
        var cfgInvalida = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = caminhoInvalido };

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() => _bd.TestarConexaoAsync(cfgInvalida));

        Assert.Equal("Db.Error.ConnectionFailed", ex.Chave);
        Assert.NotNull(ex.InnerException);
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

    [Fact]
    public async Task GarantirColunas_DuasChamadasConcorrentesNaMesmaLinha_ConvergemParaUmUnicoGuid()
    {
        await _bd.CriarTabelaAsync(_sqlite);
        await _bd.GarantirColunasAsync(_sqlite);

        long idInserido;
        await using (var con = _bd.CriarConexao(_sqlite))
        {
            await con.OpenAsync();
            await using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO CofreDeSenhas (usuario, senha, dominio) VALUES ('u', 's', 'corrida.com')";
                await cmd.ExecuteNonQueryAsync();
            }
            await using var busca = con.CreateCommand();
            busca.CommandText = "SELECT last_insert_rowid()";
            idInserido = Convert.ToInt64(await busca.ExecuteScalarAsync());
        }

        // Simula dois dispositivos chamando GarantirColunasAsync quase ao mesmo tempo
        // sobre a mesma linha legada (guid_id NULL) — sem a guarda otimista (WHERE
        // guid_id IS NULL na UPDATE), os dois gerariam um GUID diferente e o UPDATE
        // que rodasse por último decidiria a identidade "oficial" sem o outro
        // dispositivo saber, invalidando a reconciliação que ele já tinha feito.
        var bdA = new ServicoBancoDados();
        var bdB = new ServicoBancoDados();
        var resultados = await Task.WhenAll(bdA.GarantirColunasAsync(_sqlite), bdB.GarantirColunasAsync(_sqlite));

        // Só uma das duas chamadas pode ter de fato atribuído o guid — a outra vira
        // no-op e não deve reivindicar a linha no próprio retorno.
        var totalAssumido = resultados.Count(r => r.Contains(idInserido));
        Assert.Equal(1, totalAssumido);

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
    public async Task PublicarAuth_SemTabelaCriada_LancaErroLocalizavelEmVezDeEngolirSilenciosamente()
    {
        var dados = new AuthBanco(new byte[16], new byte[32], 1, 3, 65536, 1);

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() => _bd.PublicarAuthAsync(_sqlite, dados));

        Assert.Equal("Db.Error.SchemaFailed", ex.Chave);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task LerAuth_SemTabelaCriada_RetornaNulo()
    {
        Assert.False(await _bd.TabelaAuthExisteAsync(_sqlite));

        Assert.Null(await _bd.LerAuthAsync(_sqlite));
    }

    [Fact]
    public async Task LerAuth_ComTabelaExistenteMasEsquemaQuebrado_LancaErroLocalizavelEmVezDeTratarComoTabelaInexistente()
    {
        await _bd.CriarTabelaAuthAsync(_sqlite);

        // Tabela existe (TabelaAuthExisteAsync deve continuar enxergando isso), mas
        // uma coluna que o SELECT espera foi perdida — simula corrupção de esquema
        // real, diferente de "tabela nunca chegou a ser criada".
        await using (var con = _bd.CriarConexao(_sqlite))
        {
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"ALTER TABLE {ServicoBancoDados.NomeTabelaAuth} DROP COLUMN salt";
            await cmd.ExecuteNonQueryAsync();
        }

        Assert.True(await _bd.TabelaAuthExisteAsync(_sqlite));

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() => _bd.LerAuthAsync(_sqlite));

        Assert.Equal("Db.Error.SchemaFailed", ex.Chave);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task PublicarAuth_ChamadoDuasVezes_SegundaChamadaAtualizaOPrimeiroPublicado()
    {
        // Antes, a segunda chamada era ignorada silenciosamente (assumia que só podia
        // ser outro dispositivo publicando a mesma coisa). Isso é o que impedia trocar
        // a senha mestra de propagar salt/verificador novos para um banco já conectado
        // — a linha id=1 nunca mudava depois da primeira publicação. Precisa ser um
        // upsert de verdade: o dado mais recente prevalece.
        await _bd.CriarTabelaAuthAsync(_sqlite);

        var primeiro = new AuthBanco(Enumerable.Repeat((byte)1, 16).ToArray(), new byte[32], 1, 3, 65536, 1);
        var segundo = new AuthBanco(Enumerable.Repeat((byte)9, 16).ToArray(), Enumerable.Repeat((byte)7, 32).ToArray(), 0, 5, 131072, 2);

        await _bd.PublicarAuthAsync(_sqlite, primeiro);
        await _bd.PublicarAuthAsync(_sqlite, segundo);

        var lido = await _bd.LerAuthAsync(_sqlite);
        Assert.Equal(segundo.Salt, lido!.Salt);
        Assert.Equal(segundo.Verificador, lido.Verificador);
        Assert.Equal(segundo.Kdf, lido.Kdf);
        Assert.Equal(segundo.Custo, lido.Custo);
        Assert.Equal(segundo.MemoriaKb, lido.MemoriaKb);
        Assert.Equal(segundo.Paralelismo, lido.Paralelismo);
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
        // ClearPool escopado à connection string deste arquivo — ClearAllPools
        // derrubaria conexões de outras classes de teste rodando em paralelo.
        using (var con = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _arquivo }.ConnectionString))
            SqliteConnection.ClearPool(con);
        try { if (File.Exists(_arquivo)) File.Delete(_arquivo); } catch { }
    }
}
