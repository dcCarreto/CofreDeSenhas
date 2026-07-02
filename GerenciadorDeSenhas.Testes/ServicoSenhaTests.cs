using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoSenhaTests : IDisposable
{
    private readonly byte[] _chave;
    private readonly IServicoCriptografia _criptografia;
    private readonly IPersistenciaLocal _persistencia;
    private readonly IRepositorioSenha _repositorio;
    private readonly IServicoSenha _servico;
    private readonly string _pastaTemp;

    public ServicoSenhaTests()
    {
        _chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(_chave);

        _pastaTemp = Path.Combine(Path.GetTempPath(), "GS_Servico_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pastaTemp);

        _criptografia = new ServicoCriptografia(_chave);
        _persistencia = new PersistenciaLocal(_criptografia, _pastaTemp);
        _repositorio = new RepositorioSenha(_persistencia, _chave);
        _servico = new ServicoSenha(_repositorio, _criptografia);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_pastaTemp))
                Directory.Delete(_pastaTemp, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task CriarSenhaAsync_ComDadosValidos_CriaSenhaEncriptada()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456",
            Categoria.Personal, "https://gmail.com", "Meu email pessoal");

        Assert.NotNull(senha);
        Assert.NotEqual(Guid.Empty, senha.Id);
        Assert.Equal("Gmail", senha.NomeServico);
        Assert.Equal("user@gmail.com", senha.Usuario);
        Assert.NotEqual("Senha@123456", senha.SenhaHash);
        Assert.Equal(Categoria.Personal, senha.Categoria);
        Assert.False(senha.Favorito);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComDadosValidos_AtualizaSenha()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789",
            Categoria.Personal);

        var atualizada = await _servico.ObterSenhaAsync(senha.Id);

        Assert.NotNull(atualizada);
        Assert.Equal("novo@gmail.com", atualizada.Usuario);
    }

    [Fact]
    public async Task RemoverSenhaAsync_ComIdExistente_RemoveSenha()
    {
        var senha = await _servico.CriarSenhaAsync(
            "GitHub", "dev@github.com", "GitHubSenha@123", Categoria.Work);

        await _servico.RemoverSenhaAsync(senha.Id);
        var removida = await _servico.ObterSenhaAsync(senha.Id);

        Assert.Null(removida);
    }

    [Fact]
    public async Task ObterSenhaAsync_ComIdExistente_RetornaSenha()
    {
        var criada = await _servico.CriarSenhaAsync(
            "AWS", "admin@aws.com", "AwsSenha@123", Categoria.Finance);

        var obtida = await _servico.ObterSenhaAsync(criada.Id);

        Assert.NotNull(obtida);
        Assert.Equal(criada.Id, obtida.Id);
        Assert.Equal("AWS", obtida.NomeServico);
    }

    [Fact]
    public async Task ListarTodosAsync_ComMultiplasSenhas_RetornaTodasAsSenhas()
    {
        await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.CriarSenhaAsync("GitHub", "dev@github.com", "GitHubSenha@123", Categoria.Work);
        await _servico.CriarSenhaAsync("AWS", "admin@aws.com", "AwsSenha@123", Categoria.Finance);

        var todas = await _servico.ListarTodosAsync();

        Assert.Equal(3, todas.Count);
    }

    [Fact]
    public async Task BuscarPorServicoAsync_ComNomeServico_RetornaBuscaInsensitiva()
    {
        await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.CriarSenhaAsync("Gmail Business", "business@gmail.com", "Business@123", Categoria.Work);

        var resultado = await _servico.BuscarPorServicoAsync("gmail");

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task ListarPorCategoriaAsync_RetornaApenasCategoriaSolicitada()
    {
        await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.CriarSenhaAsync("GitHub", "dev@github.com", "GitHubSenha@123", Categoria.Work);

        var work = await _servico.ListarPorCategoriaAsync(Categoria.Work);

        Assert.Single(work);
        Assert.Equal("GitHub", work[0].NomeServico);
    }

    [Fact]
    public async Task ListarFavoritosAsync_RetornaApenasMarkedFavorites()
    {
        var senha1 = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        var senha2 = await _servico.CriarSenhaAsync("GitHub", "dev@github.com", "GitHubSenha@123", Categoria.Work);

        await _servico.MarcarComoFavoritoAsync(senha1.Id);

        var favoritos = await _servico.ListarFavoritosAsync();

        Assert.Single(favoritos);
        Assert.Equal("Gmail", favoritos[0].NomeServico);
    }

    [Fact]
    public async Task MarcarComoFavoritoAsync_MarcaSenhaComoFavorita()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.MarcarComoFavoritoAsync(senha.Id);
        var atualizada = await _servico.ObterSenhaAsync(senha.Id);

        Assert.True(atualizada?.Favorito);
    }

    [Fact]
    public async Task RemoverDeFavoritoAsync_RemoveMarcacaoFavorita()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.MarcarComoFavoritoAsync(senha.Id);

        await _servico.RemoverDeFavoritoAsync(senha.Id);
        var atualizada = await _servico.ObterSenhaAsync(senha.Id);

        Assert.False(atualizada?.Favorito);
    }

    [Fact]
    public async Task PersistirAsync_SalvaSenhasEmArquivoEncriptado()
    {
        await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.PersistirAsync();

        var caminhoSenhas = Path.Combine(_pastaTemp, "senhas.json.enc");
        Assert.True(File.Exists(caminhoSenhas));
    }

    [Theory]
    [InlineData("Senha@123456", true)]
    [InlineData("abc123", false)]
    [InlineData("Abc123", false)]
    [InlineData("Abc@", false)]
    [InlineData("Abc@123456!@#", true)]
    [InlineData("abc@123456!@#", false)]
    [InlineData("ABC@123456!@#", false)]
    [InlineData("AbC@abcdef!@#", false)]
    public void ValidarForteSenha_ComVariasCombinaçoes_RetornaResultadoCorreto(string senha, bool esperado)
    {
        var resultado = _servico.ValidarForteSenha(senha);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public async Task ContarSenhas_RetornaQuantidadeCorreta()
    {
        for (int i = 0; i < 5; i++)
        {
            await _servico.CriarSenhaAsync(
                $"Servico{i}", $"user{i}@example.com", "Senha@123456",
                Categoria.Personal);
        }

        var total = _servico.ContarSenhas();

        Assert.Equal(5, total);
    }

    [Fact]
    public async Task CriarSenhaAsync_ComNomeServicoVazio_LancaExcecao()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _servico.CriarSenhaAsync("", "user@example.com", "Senha@123456", Categoria.Personal));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComUsuarioVazio_LancaExcecao()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _servico.CriarSenhaAsync("Gmail", "", "Senha@123456", Categoria.Personal));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComSenhaVazia_LancaExcecao()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _servico.CriarSenhaAsync("Gmail", "user@example.com", "", Categoria.Personal));
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComIdInexistente_LancaExcecao()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servico.AtualizarSenhaAsync(Guid.NewGuid(), "Gmail", "user@example.com", "Senha@123456", Categoria.Personal));
    }

    [Fact]
    public async Task RemoverSenhaAsync_ComIdInexistente_LancaExcecao()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servico.RemoverSenhaAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComTotp_ArmazenaSegredoCifradoNormalizado()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal,
            totpSegredo: "jbsw y3dp ehpk 3pxp");

        Assert.NotNull(senha.TotpSegredo);
        Assert.NotEqual("jbsw y3dp ehpk 3pxp", senha.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(senha.TotpSegredo!));
    }

    [Fact]
    public async Task CriarSenhaAsync_SemTotp_DeixaSegredoNulo()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        Assert.Null(senha.TotpSegredo);
    }

    [Fact]
    public async Task CriarSenhaAsync_ComTotpInvalido_LancaExcecao()
    {
        await Assert.ThrowsAsync<FormatException>(() =>
            _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456",
                Categoria.Personal, totpSegredo: "###"));
    }

    [Fact]
    public async Task DefinirTotpAsync_DefineEDepoisRemove()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.DefinirTotpAsync(senha.Id, "JBSWY3DPEHPK3PXP");
        var comTotp = await _servico.ObterSenhaAsync(senha.Id);
        Assert.NotNull(comTotp!.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(comTotp.TotpSegredo!));

        await _servico.DefinirTotpAsync(senha.Id, "");
        var semTotp = await _servico.ObterSenhaAsync(senha.Id);
        Assert.Null(semTotp!.TotpSegredo);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_PreservaTotpExistente()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal,
            totpSegredo: "JBSWY3DPEHPK3PXP");

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789", Categoria.Personal);

        var atualizada = await _servico.ObterSenhaAsync(senha.Id);

        Assert.NotNull(atualizada!.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(atualizada.TotpSegredo!));
    }
}
