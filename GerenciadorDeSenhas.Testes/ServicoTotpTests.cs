using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoTotpTests
{
    private const string SegredoRfc = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    private readonly ServicoTotp _totp = new();

    [Theory]
    [InlineData(59, "287082")]
    [InlineData(1111111109, "081804")]
    [InlineData(1111111111, "050471")]
    [InlineData(1234567890, "005924")]
    [InlineData(2000000000, "279037")]
    public void Gerar_ComVetoresRfc6238_ProduzCodigoEsperado(long unix, string esperado)
    {
        var codigo = _totp.Gerar(SegredoRfc, DateTimeOffset.FromUnixTimeSeconds(unix));

        Assert.Equal(esperado, codigo.Codigo);
        Assert.Equal(6, codigo.Codigo.Length);
    }

    [Fact]
    public void Gerar_CalculaSegundosRestantesDentroDoPeriodo()
    {
        var noComeco = _totp.Gerar(SegredoRfc, DateTimeOffset.FromUnixTimeSeconds(30));
        var quaseFim = _totp.Gerar(SegredoRfc, DateTimeOffset.FromUnixTimeSeconds(59));

        Assert.Equal(30, noComeco.SegundosRestantes);
        Assert.Equal(1, quaseFim.SegundosRestantes);
        Assert.Equal(ServicoTotp.Periodo, noComeco.Periodo);
    }

    [Fact]
    public void Gerar_ComEspacosEMinusculas_NormalizaEProduzMesmoCodigo()
    {
        var codigo = _totp.Gerar("gezd gnbv gy3t qojq gezd gnbv gy3t qojq",
            DateTimeOffset.FromUnixTimeSeconds(59));

        Assert.Equal("287082", codigo.Codigo);
    }

    [Fact]
    public void Gerar_ComUriOtpauth_ExtraiSegredoEProduzCodigo()
    {
        var uri = $"otpauth://totp/Exemplo:user@site.com?secret={SegredoRfc}&issuer=Exemplo&digits=6";

        var codigo = _totp.Gerar(uri, DateTimeOffset.FromUnixTimeSeconds(59));

        Assert.Equal("287082", codigo.Codigo);
    }

    [Theory]
    [InlineData(SegredoRfc, true)]
    [InlineData("gezd gnbv gy3t qojq", true)]
    [InlineData("JBSWY3DPEHPK3PXP", true)]
    [InlineData("", false)]
    [InlineData("abc", false)]
    [InlineData("11111111", false)]
    public void SegredoValido_AvaliaEntradas(string? entrada, bool esperado)
    {
        Assert.Equal(esperado, _totp.SegredoValido(entrada));
    }

    [Fact]
    public void SegredoValido_ComNulo_RetornaFalso()
    {
        Assert.False(_totp.SegredoValido(null));
    }

    [Fact]
    public void NormalizarSegredo_RemoveEspacosEMaiusculiza()
    {
        Assert.Equal("GEZDGNBVGY3TQOJQ", _totp.NormalizarSegredo("gezd gnbv gy3t qojq"));
    }

    [Fact]
    public void NormalizarSegredo_ComEntradaInvalida_LancaExcecao()
    {
        Assert.Throws<FormatException>(() => _totp.NormalizarSegredo("!!!"));
    }
}
