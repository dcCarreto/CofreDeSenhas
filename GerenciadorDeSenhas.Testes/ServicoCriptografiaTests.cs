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
}
