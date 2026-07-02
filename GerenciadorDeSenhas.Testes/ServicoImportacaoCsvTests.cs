using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoImportacaoCsvTests
{
    private readonly ServicoImportacaoCsv _servico = new();

    [Fact]
    public void Importar_FormatoBitwarden_MapeiaColunasEDetectaFormato()
    {
        var csv = "folder,favorite,type,name,notes,fields,reprompt,login_uri,login_username,login_password,login_totp\n" +
                  "Work,1,login,GitHub,minha nota,,0,https://github.com,octocat,S3cr3t!,JBSWY3DPEHPK3PXP";

        var resultado = _servico.Importar(csv);

        Assert.Equal("Bitwarden", resultado.FormatoDetectado);
        var item = Assert.Single(resultado.Itens);
        Assert.Equal("GitHub", item.NomeServico);
        Assert.Equal("octocat", item.Usuario);
        Assert.Equal("S3cr3t!", item.Senha);
        Assert.Equal("https://github.com", item.Url);
        Assert.Equal("minha nota", item.Notas);
        Assert.Equal("JBSWY3DPEHPK3PXP", item.TotpSegredo);
        Assert.Equal(Categoria.Work, item.Categoria);
        Assert.True(item.Favorito);
    }

    [Fact]
    public void Importar_FormatoLastPass_MapeiaColunasEDetectaFormato()
    {
        var csv = "url,username,password,totp,extra,name,grouping,fav\n" +
                  "https://site.com,alice,pw123,,alguma nota,Meu Site,Personal,1";

        var resultado = _servico.Importar(csv);

        Assert.Equal("LastPass", resultado.FormatoDetectado);
        var item = Assert.Single(resultado.Itens);
        Assert.Equal("Meu Site", item.NomeServico);
        Assert.Equal("alice", item.Usuario);
        Assert.Equal("pw123", item.Senha);
        Assert.Equal("alguma nota", item.Notas);
        Assert.Equal(Categoria.Personal, item.Categoria);
        Assert.True(item.Favorito);
    }

    [Fact]
    public void Importar_FormatoChrome_DetectaFormato()
    {
        var csv = "name,url,username,password,note\n" +
                  "Example,https://example.com,user@x.com,secret,uma nota";

        var resultado = _servico.Importar(csv);

        Assert.Equal("Google Chrome / Edge", resultado.FormatoDetectado);
        var item = Assert.Single(resultado.Itens);
        Assert.Equal("Example", item.NomeServico);
        Assert.Equal("user@x.com", item.Usuario);
        Assert.Equal("secret", item.Senha);
        Assert.Equal("uma nota", item.Notas);
    }

    [Fact]
    public void Importar_FormatoFirefox_DerivaNomeDaUrl()
    {
        var csv = "\"url\",\"username\",\"password\",\"httpRealm\",\"formActionOrigin\",\"guid\",\"timeCreated\",\"timeLastUsed\",\"timePasswordChanged\"\n" +
                  "\"https://www.example.com/login\",\"alice\",\"pw\",,\"https://www.example.com\",\"{abc}\",\"1\",\"2\",\"3\"";

        var resultado = _servico.Importar(csv);

        Assert.Equal("Firefox", resultado.FormatoDetectado);
        var item = Assert.Single(resultado.Itens);
        Assert.Equal("example.com", item.NomeServico);
        Assert.Equal("alice", item.Usuario);
        Assert.Equal("pw", item.Senha);
    }

    [Fact]
    public void Importar_FormatoKeePass_DetectaFormato()
    {
        var csv = "Group,Title,Username,Password,URL,Notes\n" +
                  "General,Meu Banco,me,pw,https://bank.com,nota";

        var resultado = _servico.Importar(csv);

        Assert.Equal("KeePass", resultado.FormatoDetectado);
        var item = Assert.Single(resultado.Itens);
        Assert.Equal("Meu Banco", item.NomeServico);
        Assert.Equal("me", item.Usuario);
        Assert.Equal("https://bank.com", item.Url);
    }

    [Fact]
    public void Importar_CamposComVirgulaAspasEQuebraDeLinha_RespeitaRfc4180()
    {
        var csv = "name,username,password,notes\n" +
                  "\"Acme, Inc.\",bob,\"p@ss\"\"word\",\"linha1\nlinha2\"";

        var resultado = _servico.Importar(csv);

        var item = Assert.Single(resultado.Itens);
        Assert.Equal("Acme, Inc.", item.NomeServico);
        Assert.Equal("bob", item.Usuario);
        Assert.Equal("p@ss\"word", item.Senha);
        Assert.Equal("linha1\nlinha2", item.Notas);
    }

    [Fact]
    public void Importar_DelimitadorPontoEVirgula_DetectadoAutomaticamente()
    {
        var csv = "name;username;password\n" +
                  "Site;user;secret";

        var resultado = _servico.Importar(csv);

        var item = Assert.Single(resultado.Itens);
        Assert.Equal("Site", item.NomeServico);
        Assert.Equal("user", item.Usuario);
        Assert.Equal("secret", item.Senha);
    }

    [Fact]
    public void Importar_DelimitadorTabulacao_DetectadoAutomaticamente()
    {
        var csv = "name\tusername\tpassword\n" +
                  "Site\tuser\tsecret";

        var resultado = _servico.Importar(csv);

        var item = Assert.Single(resultado.Itens);
        Assert.Equal("Site", item.NomeServico);
        Assert.Equal("secret", item.Senha);
    }

    [Fact]
    public void Importar_TotpComOtpauth_PreservaSegredoValido()
    {
        var csv = "name,username,password,otpauth,title\n" +
                  "Acme,alice,pw,\"otpauth://totp/Acme:alice?secret=JBSWY3DPEHPK3PXP&issuer=Acme\",Acme";

        var resultado = _servico.Importar(csv);

        var item = Assert.Single(resultado.Itens);
        Assert.Equal("1Password", resultado.FormatoDetectado);
        Assert.NotNull(item.TotpSegredo);
        Assert.True(new ServicoTotp().SegredoValido(item.TotpSegredo));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("yes")]
    [InlineData("sim")]
    public void Importar_ValoresDeFavorito_SaoInterpretados(string valor)
    {
        var csv = "name,username,password,favorite\n" +
                  $"Site,user,pw,{valor}";

        var item = Assert.Single(_servico.Importar(csv).Itens);
        Assert.True(item.Favorito);
    }

    [Fact]
    public void Importar_LinhasSemSenhaOuNome_SaoIgnoradas()
    {
        var csv = "name,username,password\n" +
                  "Valida,user,pw\n" +
                  "SemSenha,user2,\n" +
                  ",user3,pw3";

        var resultado = _servico.Importar(csv);

        Assert.Single(resultado.Itens);
        Assert.Equal(2, resultado.LinhasIgnoradas);
    }

    [Fact]
    public void Importar_SemColunaDeSenha_LancaInvalidOperation()
    {
        var csv = "name,username,url\n" +
                  "Site,user,https://x";

        Assert.Throws<InvalidOperationException>(() => _servico.Importar(csv));
    }

    [Fact]
    public void Importar_ConteudoVazio_LancaInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => _servico.Importar(""));
    }

    [Fact]
    public void Importar_ApenasCabecalho_LancaInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => _servico.Importar("name,username,password"));
    }

    [Fact]
    public void ImportarArquivo_ComBomUtf8_LeCorretamente()
    {
        var caminho = Path.Combine(Path.GetTempPath(), "GS_Csv_" + Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            File.WriteAllText(caminho, "name,username,password\nSite,user,secret", new System.Text.UTF8Encoding(true));
            var resultado = _servico.ImportarArquivo(caminho);
            var item = Assert.Single(resultado.Itens);
            Assert.Equal("Site", item.NomeServico);
        }
        finally
        {
            try { File.Delete(caminho); } catch { }
        }
    }

    [Fact]
    public void ImportarArquivo_Inexistente_LancaInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _servico.ImportarArquivo(Path.Combine(Path.GetTempPath(), "nao_existe_" + Guid.NewGuid().ToString("N") + ".csv")));
    }
}
