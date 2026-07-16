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

    private async Task<Senha?> ObterAsync(Guid id) =>
        (await _servico.ListarTodosAsync()).FirstOrDefault(s => s.Id == id);

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

        var atualizada = await ObterAsync(senha.Id);

        Assert.NotNull(atualizada);
        Assert.Equal("novo@gmail.com", atualizada.Usuario);
    }

    [Fact]
    public async Task RemoverSenhaAsync_ComIdExistente_RemoveSenha()
    {
        var senha = await _servico.CriarSenhaAsync(
            "GitHub", "dev@github.com", "GitHubSenha@123", Categoria.Work);

        await _servico.RemoverSenhaAsync(senha.Id);
        var removida = await ObterAsync(senha.Id);

        Assert.Null(removida);
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
    public async Task CriarSenhaAsync_ComEtiquetas_Normaliza()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Portal Cliente", "cliente@example.com", "Senha@123456",
            Categoria.Work, etiquetas: new[] { " Clientes ", "Projetos", "clientes", "" });

        Assert.Equal(new[] { "Clientes", "Projetos" }, senha.Etiquetas);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_SemEtiquetas_PreservaEtiquetasExistentes()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456",
            Categoria.Personal, etiquetas: new[] { "Pessoal", "Email" });

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789", Categoria.Personal);

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(new[] { "Pessoal", "Email" }, atualizada!.Etiquetas);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComListaVazia_RemoveEtiquetas()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456",
            Categoria.Personal, etiquetas: new[] { "Pessoal" });

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789",
            Categoria.Personal, etiquetas: Array.Empty<string>());

        var atualizada = await ObterAsync(senha.Id);

        Assert.Empty(atualizada!.Etiquetas);
    }

    [Fact]
    public async Task MarcarComoFavoritoAsync_MarcaSenhaComoFavorita()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.MarcarComoFavoritoAsync(senha.Id);
        var atualizada = await ObterAsync(senha.Id);

        Assert.True(atualizada?.Favorito);
    }

    [Fact]
    public async Task RemoverDeFavoritoAsync_RemoveMarcacaoFavorita()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.MarcarComoFavoritoAsync(senha.Id);

        await _servico.RemoverDeFavoritoAsync(senha.Id);
        var atualizada = await ObterAsync(senha.Id);

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
        var comTotp = await ObterAsync(senha.Id);
        Assert.NotNull(comTotp!.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(comTotp.TotpSegredo!));

        await _servico.DefinirTotpAsync(senha.Id, "");
        var semTotp = await ObterAsync(senha.Id);
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

        var atualizada = await ObterAsync(senha.Id);

        Assert.NotNull(atualizada!.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(atualizada.TotpSegredo!));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComecaSemHistorico()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        Assert.Empty(senha.Historico);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComSenhaDiferente_GuardaSenhaAnteriorNoHistorico()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "user@gmail.com", "NovaSenha@789", Categoria.Personal);

        var atualizada = await ObterAsync(senha.Id);

        var anterior = Assert.Single(atualizada!.Historico);
        Assert.Equal("Senha@123456", _criptografia.Descriptografar(anterior.SenhaHash));
        Assert.Equal("NovaSenha@789", _criptografia.Descriptografar(atualizada.SenhaHash));
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComMesmaSenha_NaoRegistraHistorico()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "Senha@123456", Categoria.Work);

        var atualizada = await ObterAsync(senha.Id);

        Assert.Empty(atualizada!.Historico);
        Assert.Equal("novo@gmail.com", atualizada.Usuario);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_MultiplasTrocas_MantemOrdemCronologica()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Primeira@111", Categoria.Personal);

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "user@gmail.com", "Segunda@222", Categoria.Personal);
        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "user@gmail.com", "Terceira@333", Categoria.Personal);

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(2, atualizada!.Historico.Count);
        Assert.Equal("Primeira@111", _criptografia.Descriptografar(atualizada.Historico[0].SenhaHash));
        Assert.Equal("Segunda@222", _criptografia.Descriptografar(atualizada.Historico[1].SenhaHash));
    }

    [Fact]
    public async Task AtualizarSenhaAsync_AcimaDoLimite_MantemApenasAsMaisRecentes()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@000000", Categoria.Personal);

        for (int i = 1; i <= 15; i++)
        {
            await _servico.AtualizarSenhaAsync(
                senha.Id, "Gmail", "user@gmail.com", $"Senha@{i:000000}", Categoria.Personal);
        }

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(10, atualizada!.Historico.Count);
        Assert.Equal("Senha@000005", _criptografia.Descriptografar(atualizada.Historico[0].SenhaHash));
        Assert.Equal("Senha@000014", _criptografia.Descriptografar(atualizada.Historico[9].SenhaHash));
    }
}
