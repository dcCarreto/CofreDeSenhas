using CofreDeSenhas.Nucleo;

namespace CofreDeSenhas.Testes;

public class DescobertaCofreLocalTests
{
    [Fact]
    public void SemArquivos_RetornaNaoEncontrado()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "cofre-teste-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        try
        {
            var estado = new DescobertaCofreLocal(pasta).Verificar();
            Assert.False(estado.Encontrado);
            Assert.False(estado.Pronto);
            Assert.Equal("nao_encontrado", estado.Estado);
        }
        finally
        {
            Directory.Delete(pasta, true);
        }
    }

    [Fact]
    public void SoAuthSemCofre_RetornaIncompleto()
    {
        using var cofre = new CofreTemporario();
        var estado = new DescobertaCofreLocal(cofre.Pasta).Verificar();
        Assert.False(estado.Pronto);
        Assert.Equal("incompleto", estado.Estado);
    }

    [Fact]
    public async Task AuthMaisCofreValido_RetornaPronto()
    {
        using var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("Servico", "usuario", "senha", "https://exemplo.com");

        var estado = new DescobertaCofreLocal(cofre.Pasta).Verificar();

        Assert.True(estado.Encontrado);
        Assert.True(estado.Pronto);
        Assert.Equal("pronto", estado.Estado);
    }

    [Fact]
    public async Task CofreComConteudoInvalido_RetornaFormatoInvalido()
    {
        using var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("Servico", "usuario", "senha");
        File.WriteAllText(Path.Combine(cofre.Pasta, "senhas.json.enc"), "isto-nao-e-base64-@@@");

        var estado = new DescobertaCofreLocal(cofre.Pasta).Verificar();

        Assert.False(estado.Pronto);
        Assert.Equal("formato_invalido", estado.Estado);
    }
}
