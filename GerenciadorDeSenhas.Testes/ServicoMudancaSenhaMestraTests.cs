using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoMudancaSenhaMestraTests : IDisposable
{
    private readonly string _pasta;

    public ServicoMudancaSenhaMestraTests()
    {
        _pasta = Path.Combine(Path.GetTempPath(), "GS_MudSenha_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true); } catch { }
    }

    private async Task PrepararCofre(string senhaMestra, params (string nome, string usuario, string senha)[] itens)
    {
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.CriarSenhaMestra(senhaMestra);
        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        var servico = new ServicoSenha(repo, crypto);
        foreach (var (nome, usuario, senha) in itens)
            await servico.CriarSenhaAsync(nome, usuario, senha, Categoria.Personal);
        await servico.PersistirAsync();
    }

    [Fact]
    public async Task AlterarAsync_ComSenhaCorreta_TrocaChaveEPreservaSenhas()
    {
        await PrepararCofre("SenhaAntiga@123",
            ("GitHub", "dev@git.com", "GitHub@Secreta1"),
            ("Gmail", "user@gmail.com", "Gmail@Secreta2"));

        await new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        var auth = new AutenticacaoMestra(_pasta);
        Assert.Null(auth.Autenticar("SenhaAntiga@123"));
        var chaveNova = auth.Autenticar("SenhaNova@456");
        Assert.NotNull(chaveNova);

        var crypto = new ServicoCriptografia(chaveNova!);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var senhas = await persist.CarregarSenhasAsync(chaveNova!);
        Assert.Equal(2, senhas.Count);
        var git = senhas.Single(s => s.NomeServico == "GitHub");
        Assert.Equal("GitHub@Secreta1", crypto.Descriptografar(git.SenhaHash));
    }

    [Fact]
    public async Task AlterarAsync_ComSenhaAtualErrada_LancaInvalidOperationENaoAltera()
    {
        await PrepararCofre("SenhaAntiga@123", ("Svc", "u", "Senha@Forte1"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaErrada@999", "SenhaNova@456"));

        Assert.NotNull(new AutenticacaoMestra(_pasta).Autenticar("SenhaAntiga@123"));
    }

    [Fact]
    public async Task AlterarAsync_ComNovaSenhaCurta_LancaArgumentException()
    {
        await PrepararCofre("SenhaAntiga@123", ("Svc", "u", "Senha@Forte1"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaAntiga@123", "curta"));
    }

    [Fact]
    public async Task AlterarAsync_CofreVazio_FuncionaSemErro()
    {
        new AutenticacaoMestra(_pasta).CriarSenhaMestra("SenhaAntiga@123");

        await new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        var auth = new AutenticacaoMestra(_pasta);
        Assert.Null(auth.Autenticar("SenhaAntiga@123"));
        Assert.NotNull(auth.Autenticar("SenhaNova@456"));
    }
}
