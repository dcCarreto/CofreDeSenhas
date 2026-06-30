using CofreDeSenhas.Nucleo;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class DominiosTests
{
    [Theory]
    [InlineData("https://www.exemplo.com/login", "www.exemplo.com")]
    [InlineData("exemplo.com", "exemplo.com")]
    [InlineData("HTTP://Exemplo.COM", "exemplo.com")]
    [InlineData("login.exemplo.com", "login.exemplo.com")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void ExtrairHost_NormalizaUrlOuDominio(string entrada, string esperado)
    {
        Assert.Equal(esperado, Dominios.ExtrairHost(entrada));
    }

    [Theory]
    [InlineData("https://www.exemplo.com/login", "exemplo.com")]
    [InlineData("login.exemplo.com", "exemplo.com")]
    [InlineData("exemplo.com", "exemplo.com")]
    [InlineData("github.com", "github.com")]
    [InlineData("a.b.c.exemplo.com", "exemplo.com")]
    public void Registravel_ReduzAosDoisUltimosRotulos(string entrada, string esperado)
    {
        Assert.Equal(esperado, Dominios.Registravel(entrada));
    }

    [Theory]
    [InlineData("login.empresa.com.br", "empresa.com.br")]
    [InlineData("empresa.com.br", "empresa.com.br")]
    [InlineData("www.bbc.co.uk", "bbc.co.uk")]
    [InlineData("bbc.co.uk", "bbc.co.uk")]
    [InlineData("a.b.minhaloja.com.br", "minhaloja.com.br")]
    [InlineData("portal.gov.br", "portal.gov.br")]
    public void Registravel_ConsideraSufixosCompostos(string entrada, string esperado)
    {
        Assert.Equal(esperado, Dominios.Registravel(entrada));
    }

    [Theory]
    [InlineData("conta.empresa.com.br", "https://empresa.com.br")]
    [InlineData("www.bbc.co.uk", "https://bbc.co.uk/news")]
    public void Casa_ComSufixoComposto_RetornaTrue(string aba, string urlSalva)
    {
        Assert.True(Dominios.Casa(aba, urlSalva));
    }

    [Fact]
    public void Casa_DominiosDiferentesSobMesmoSufixoComposto_RetornaFalse()
    {
        Assert.False(Dominios.Casa("empresa.com.br", "https://concorrente.com.br"));
    }

    [Theory]
    [InlineData("github.com", "https://github.com")]
    [InlineData("github.com", "https://www.github.com/login")]
    [InlineData("login.github.com", "https://github.com")]
    [InlineData("www.exemplo.com", "exemplo.com")]
    public void Casa_QuandoMesmoDominioRegistravel_RetornaTrue(string aba, string urlSalva)
    {
        Assert.True(Dominios.Casa(aba, urlSalva));
    }

    [Theory]
    [InlineData("github.com", "https://gitlab.com")]
    [InlineData("exemplo.com", "https://exemplo.org")]
    [InlineData("banco.com", null)]
    [InlineData("banco.com", "")]
    public void Casa_QuandoDominioDiferenteOuSemUrl_RetornaFalse(string aba, string? urlSalva)
    {
        Assert.False(Dominios.Casa(aba, urlSalva));
    }
}
