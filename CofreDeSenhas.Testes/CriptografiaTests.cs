using System.Security.Cryptography;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Testes;

public class CriptografiaTests
{
    [Fact]
    public void Criptografar_Descriptografar_PreservaOTexto()
    {
        var chave = RandomNumberGenerator.GetBytes(EspecificacaoCriptografica.TamanhoChave);
        var cripto = new ServicoCriptografia(chave);

        var cifrado = cripto.Criptografar("senha-secreta-123");

        Assert.Equal("senha-secreta-123", cripto.Descriptografar(cifrado));
    }

    [Fact]
    public void Criptografar_MesmoTexto_GeraCifrasDiferentes()
    {
        var chave = RandomNumberGenerator.GetBytes(EspecificacaoCriptografica.TamanhoChave);
        var cripto = new ServicoCriptografia(chave);

        Assert.NotEqual(cripto.Criptografar("igual"), cripto.Criptografar("igual"));
    }

    [Fact]
    public void Descriptografar_ComChaveErrada_Falha()
    {
        var cifrado = new ServicoCriptografia(
            RandomNumberGenerator.GetBytes(EspecificacaoCriptografica.TamanhoChave)).Criptografar("segredo");
        var outra = new ServicoCriptografia(
            RandomNumberGenerator.GetBytes(EspecificacaoCriptografica.TamanhoChave));

        Assert.ThrowsAny<CryptographicException>(() => outra.Descriptografar(cifrado));
    }

    [Fact]
    public void ChaveComTamanhoInvalido_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new ServicoCriptografia(new byte[16]));
    }

    [Fact]
    public void SenhaMestra_Autentica_ComSenhaCorreta()
    {
        using var cofre = new CofreTemporario();
        var auth = new AutenticacaoMestra(cofre.Pasta);

        var chave = auth.Autenticar(cofre.SenhaMestra);

        Assert.NotNull(chave);
        Assert.Equal(EspecificacaoCriptografica.TamanhoChave, chave!.Length);
        Assert.Equal(cofre.Chave, chave);
    }

    [Fact]
    public void SenhaMestra_ComSenhaErrada_RetornaNull()
    {
        using var cofre = new CofreTemporario();
        Assert.Null(new AutenticacaoMestra(cofre.Pasta).Autenticar("senha-errada-000"));
    }

    [Fact]
    public void SenhaMestra_MuitoCurta_Rejeitada()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "cofre-teste-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Throws<ArgumentException>(() => new AutenticacaoMestra(pasta).CriarSenhaMestra("1234"));
        }
        finally
        {
            if (Directory.Exists(pasta)) Directory.Delete(pasta, true);
        }
    }
}
