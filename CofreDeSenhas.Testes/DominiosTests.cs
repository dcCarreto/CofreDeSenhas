using CofreDeSenhas.Nucleo;

namespace CofreDeSenhas.Testes;

public class DominiosTests
{
    [Theory]
    [InlineData("https://login.exemplo.com/entrar", "login.exemplo.com")]
    [InlineData("exemplo.com", "exemplo.com")]
    [InlineData("HTTPS://EXEMPLO.COM", "exemplo.com")]
    [InlineData("", "")]
    public void ExtrairHost(string entrada, string esperado)
    {
        Assert.Equal(esperado, Dominios.ExtrairHost(entrada));
    }

    [Theory]
    [InlineData("https://login.exemplo.com", "exemplo.com")]
    [InlineData("https://www.exemplo.com", "exemplo.com")]
    [InlineData("https://loja.exemplo.com.br", "exemplo.com.br")]
    [InlineData("https://exemplo.co.uk", "exemplo.co.uk")]
    public void Registravel(string entrada, string esperado)
    {
        Assert.Equal(esperado, Dominios.Registravel(entrada));
    }

    [Theory]
    [InlineData("login.exemplo.com", "https://exemplo.com/conta", true)]
    [InlineData("www.exemplo.com", "https://app.exemplo.com", true)]
    [InlineData("exemplo.com", "https://outro.com", false)]
    [InlineData("exemplo.com", null, false)]
    [InlineData("", "https://exemplo.com", false)]
    public void Casa(string dominioAba, string? urlSalva, bool esperado)
    {
        Assert.Equal(esperado, Dominios.Casa(dominioAba, urlSalva));
    }
}
