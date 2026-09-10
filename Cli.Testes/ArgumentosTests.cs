using CofreDeSenhas.Cli;
using GerenciadorDeSenhas.Modelos;

namespace Cli.Testes;

public class ArgumentosTests
{
    [Fact]
    public void SeparaPosicionaisFlagsComValorEFlagsBool()
    {
        var a = new Argumentos(new[] { "GitHub", "--usuario", "denis", "--nao-limpar", "-c", "12" });

        Assert.Equal(new[] { "GitHub" }, a.Posicionais);
        Assert.Equal("denis", a.Valor("--usuario", "-u"));
        Assert.True(a.Tem("--nao-limpar"));
        Assert.False(a.Tem("--favoritas"));
        Assert.Equal(12, a.ValorInt(20, "--comprimento", "-c"));
    }

    [Fact]
    public void FlagNoFimSemValor_EhBool()
    {
        var a = new Argumentos(new[] { "--frase" });
        Assert.True(a.Tem("--frase"));
        Assert.Null(a.Valor("--frase"));
    }

    [Fact]
    public void ValorInt_UsaPadraoQuandoAusenteOuInvalido()
    {
        Assert.Equal(30, new Argumentos(Array.Empty<string>()).ValorInt(30, "--limpar-apos"));
        Assert.Equal(30, new Argumentos(new[] { "--limpar-apos", "abc" }).ValorInt(30, "--limpar-apos"));
    }

    [Fact]
    public void TermoUnido_JuntaVariosPosicionais()
    {
        var a = new Argumentos(new[] { "Banco", "do", "Brasil" });
        Assert.Equal("Banco do Brasil", a.TermoUnido());
    }

    [Fact]
    public void FlagBoolSeguidaDeFlagCurta_NaoEngoleAFlagCurta()
    {
        var a = new Argumentos(new[] { "--sem-simbolos", "-c", "12" });
        Assert.True(a.Tem("--sem-simbolos"));
        Assert.Equal(12, a.ValorInt(20, "-c"));
    }

    [Fact]
    public void FormaComIgual_AceitaValorComTracoNaFrente()
    {
        var a = new Argumentos(new[] { "--separador=-", "--usuario=-corp" });
        Assert.Equal("-", a.Valor("--separador"));
        Assert.Equal("-corp", a.Valor("--usuario"));
    }

    [Fact]
    public void ValorEspacadoQueComecaComTracoUnico_EhAceitoComoValor()
    {
        var a = new Argumentos(new[] { "MyService", "--usuario", "-corp" });
        Assert.Equal(new[] { "MyService" }, a.Posicionais);
        Assert.Equal("-corp", a.Valor("--usuario"));
    }

    [Theory]
    [InlineData("trabalho", Categoria.Work)]
    [InlineData("PESSOAL", Categoria.Personal)]
    [InlineData("finanças", Categoria.Finance)]
    [InlineData("other", Categoria.Other)]
    public void TentarCategoria_AceitaSinonimos(string bruto, Categoria esperada)
    {
        Assert.True(Formatar.TentarCategoria(bruto, out var c));
        Assert.Equal(esperada, c);
    }

    [Fact]
    public void TentarCategoria_RejeitaDesconhecida()
    {
        Assert.False(Formatar.TentarCategoria("jogos", out _));
    }

    [Fact]
    public void Duracao_FormataFaixas()
    {
        Assert.Equal("3 s", Formatar.Duracao(TimeSpan.FromSeconds(3)));
        Assert.Equal("2 min", Formatar.Duracao(TimeSpan.FromMinutes(2)));
        Assert.Equal("2 min 5 s", Formatar.Duracao(TimeSpan.FromSeconds(125)));
    }
}
