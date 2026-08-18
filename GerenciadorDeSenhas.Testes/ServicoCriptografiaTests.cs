using System.Security.Cryptography;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoCriptografiaTests
{
    private readonly ServicoCriptografia _servico;

    public ServicoCriptografiaTests()
    {
        var chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(chave);

        _servico = new ServicoCriptografia(chave);
    }

    [Fact]
    public void Criptografar_ComTextoValido_RetornaCiphertext()
    {
        var plaintext = "MinhaSenh@123";

        var ciphertext = _servico.Criptografar(plaintext);

        Assert.NotNull(ciphertext);
        Assert.NotEqual(plaintext, ciphertext);
        Assert.True(ciphertext.Length > plaintext.Length);
    }

    [Fact]
    public void DescriptografarBytes_ComDadosMenoresQueNonceMaisTag_LancaCryptographicException()
    {
        var dadosCurtos = new byte[10];

        Assert.Throws<CryptographicException>(() => _servico.DescriptografarBytes(dadosCurtos));
    }

    [Fact]
    public void Descriptografar_ComCiphertextValido_RetornaPlaintextOriginal()
    {
        var original = "MinhaSenh@123";
        var encrypted = _servico.Criptografar(original);

        var decrypted = _servico.Descriptografar(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Criptografar_DuasVezes_ProduzemResultadosDiferentes()
    {
        var plaintext = "MinhaSenh@123";

        var encrypted1 = _servico.Criptografar(plaintext);
        var encrypted2 = _servico.Criptografar(plaintext);

        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Criptografar_ComTextoDiferente_RetornaResultadosDiferentes()
    {
        var encrypted1 = _servico.Criptografar("Senha1");
        var encrypted2 = _servico.Criptografar("Senha2");

        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Descriptografar_ComTextoMuitoLongo_FuncionaCorretamente()
    {
        var original = new string('a', 1000);

        var encrypted = _servico.Criptografar(original);
        var decrypted = _servico.Descriptografar(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void CriptografarBytes_EDescriptografarBytes_RoundTripPreservaConteudo()
    {
        var original = new byte[1024];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(original);

        var cifrado = _servico.CriptografarBytes(original);
        var decifrado = _servico.DescriptografarBytes(cifrado);

        Assert.Equal(original, decifrado);
        Assert.NotEqual(original, cifrado);
    }

    [Fact]
    public void CriptografarBytes_DuasVezes_ProduzResultadosDiferentes()
    {
        var dados = new byte[] { 1, 2, 3, 4, 5 };

        var cifrado1 = _servico.CriptografarBytes(dados);
        var cifrado2 = _servico.CriptografarBytes(dados);

        Assert.NotEqual(cifrado1, cifrado2);
    }

    [Fact]
    public void ZerarChave_AposChamada_TornaOperacoesSeguintesInvalidas()
    {
        var cifrado = _servico.Criptografar("dados sensíveis");

        _servico.ZerarChave();

        Assert.Throws<ObjectDisposedException>(() => _servico.Descriptografar(cifrado));
    }

    [Fact]
    public void Criptografar_AposZerarChave_LancaObjectDisposedEmVezDeCifrarComChaveZerada()
    {
        _servico.ZerarChave();

        Assert.Throws<ObjectDisposedException>(() => _servico.Criptografar("dados sensíveis"));
    }

    [Fact]
    public void CalcularHmacIntegridade_AposZerarChave_LancaObjectDisposedException()
    {
        _servico.ZerarChave();

        Assert.Throws<ObjectDisposedException>(() => _servico.CalcularHmacIntegridade("dados"));
    }

    [Fact]
    public void VerificarHmacIntegridade_ComHmacCalculadoParaOsMesmosDados_RetornaTrue()
    {
        var hmac = _servico.CalcularHmacIntegridade("dados-do-registro");

        Assert.True(_servico.VerificarHmacIntegridade("dados-do-registro", hmac));
    }

    [Fact]
    public void VerificarHmacIntegridade_ComDadosAlterados_RetornaFalse()
    {
        var hmac = _servico.CalcularHmacIntegridade("dados-do-registro");

        Assert.False(_servico.VerificarHmacIntegridade("dados-adulterados", hmac));
    }

    [Fact]
    public void VerificarHmacIntegridade_ComHmacQueNaoEhBase64Valido_RetornaFalseSemLancar()
    {
        Assert.False(_servico.VerificarHmacIntegridade("dados-do-registro", "@@@nao-e-base64@@@"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void VerificarHmacIntegridade_ComHmacNuloOuVazio_RetornaFalse(string? hmacBase64)
    {
        Assert.False(_servico.VerificarHmacIntegridade("dados-do-registro", hmacBase64));
    }
}
