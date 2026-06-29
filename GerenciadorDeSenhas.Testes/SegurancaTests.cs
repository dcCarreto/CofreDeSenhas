using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class SegurancaTests
{
    private static byte[] NovaChave()
    {
        var c = new byte[32];
        RandomNumberGenerator.Fill(c);
        return c;
    }

    [Theory]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(33)]
    public void Construtor_ComChaveDeTamanhoInvalido_LancaExcecao(int tamanho)
    {
        Assert.Throws<ArgumentException>(() => new ServicoCriptografia(new byte[tamanho]));
    }

    [Fact]
    public void Descriptografar_ComChaveDiferente_FalhaAutenticacao()
    {
        var cifrado = new ServicoCriptografia(NovaChave()).Criptografar("dados sensíveis");
        var outroServico = new ServicoCriptografia(NovaChave());

        Assert.ThrowsAny<CryptographicException>(() => outroServico.Descriptografar(cifrado));
    }

    [Fact]
    public void Descriptografar_ComCorpoAdulterado_DetectaViolacao()
    {
        var servico = new ServicoCriptografia(NovaChave());
        var cifrado = servico.Criptografar("dados sensíveis");

        var bytes = Convert.FromBase64String(cifrado);
        bytes[bytes.Length / 2] ^= 0xFF;
        var adulterado = Convert.ToBase64String(bytes);

        Assert.ThrowsAny<CryptographicException>(() => servico.Descriptografar(adulterado));
    }

    [Fact]
    public void Descriptografar_ComAuthTagAdulterada_DetectaViolacao()
    {
        var servico = new ServicoCriptografia(NovaChave());
        var cifrado = servico.Criptografar("dados");

        var bytes = Convert.FromBase64String(cifrado);
        bytes[^1] ^= 0xFF;
        var adulterado = Convert.ToBase64String(bytes);

        Assert.ThrowsAny<CryptographicException>(() => servico.Descriptografar(adulterado));
    }

    [Fact]
    public void Criptografar_MesmoTextoDuasVezes_ProduzCifrasDiferentes()
    {
        var servico = new ServicoCriptografia(NovaChave());

        Assert.NotEqual(servico.Criptografar("igual"), servico.Criptografar("igual"));
    }

    [Fact]
    public async Task CarregarSenhas_ComArquivoAdulterado_FalhaEmVezDeRetornarLixo()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "GS_Seg_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        try
        {
            var chave = NovaChave();
            var cripto = new ServicoCriptografia(chave);
            var persist = new PersistenciaLocal(cripto, pasta);
            await persist.SalvarSenhasAsync(new List<Senha>
            {
                new() { Id = Guid.NewGuid(), NomeServico = "X", Usuario = "u", SenhaHash = cripto.Criptografar("p") }
            }, chave);

            var caminho = Path.Combine(pasta, "senhas.json.enc");
            var bytes = Convert.FromBase64String(await File.ReadAllTextAsync(caminho));
            bytes[bytes.Length / 2] ^= 0xFF;
            await File.WriteAllTextAsync(caminho, Convert.ToBase64String(bytes));

            await Assert.ThrowsAsync<InvalidOperationException>(() => persist.CarregarSenhasAsync(chave));
        }
        finally
        {
            try { Directory.Delete(pasta, recursive: true); } catch { }
        }
    }

    [Theory]
    [InlineData(null, "user", "Senha@123")]
    [InlineData("Servico", null, "Senha@123")]
    [InlineData("Servico", "user", null)]
    [InlineData("", "user", "Senha@123")]
    [InlineData("Servico", "   ", "Senha@123")]
    public async Task CriarSenha_ComEntradaInvalida_LancaArgumentException(string? nome, string? usuario, string? senha)
    {
        var servico = MontarServicoEmMemoria();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servico.CriarSenhaAsync(nome!, usuario!, senha!, Categoria.Personal));
    }

    [Fact]
    public async Task CriarSenha_ComCamposExcedendoLimites_LancaArgumentException()
    {
        var servico = MontarServicoEmMemoria();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servico.CriarSenhaAsync(new string('a', 101), "user", "Senha@123", Categoria.Personal));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            servico.CriarSenhaAsync("Servico", new string('a', 256), "Senha@123", Categoria.Personal));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            servico.CriarSenhaAsync("Servico", "user", new string('a', 1001), Categoria.Personal));
    }

    private static IServicoSenha MontarServicoEmMemoria()
    {
        var chave = NovaChave();
        var cripto = new ServicoCriptografia(chave);
        var repo = new RepositorioSenha(new PersistenciaEmMemoria(), chave);
        return new ServicoSenha(repo, cripto);
    }
}
