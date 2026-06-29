using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class IntegracaoCofreTests : IDisposable
{
    private readonly string _pasta;

    public IntegracaoCofreTests()
    {
        _pasta = Path.Combine(Path.GetTempPath(), "GS_Integ_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true); } catch { }
    }

    private (IServicoSenha servico, IServicoCriptografia cripto) MontarCofre(byte[] chave)
    {
        var cripto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(cripto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        return (new ServicoSenha(repo, cripto), cripto);
    }

    private static byte[] NovaChave()
    {
        var c = new byte[32];
        RandomNumberGenerator.Fill(c);
        return c;
    }

    [Fact]
    public async Task FluxoCompleto_CriarPersistirRecarregar_PreservaDadosEDescriptografa()
    {
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.CriarSenhaMestra("SenhaMestra@123");

        var (servico1, _) = MontarCofre(chave);
        var criada = await servico1.CriarSenhaAsync(
            "GitHub", "dev@git.com", "GitHub@Secreta123", Categoria.Work, "https://github.com");
        await servico1.PersistirAsync();

        var chaveReaberta = auth.Autenticar("SenhaMestra@123")!;
        var (servico2, cripto2) = MontarCofre(chaveReaberta);
        var todas = await servico2.ListarTodosAsync();

        Assert.Single(todas);
        var recarregada = todas[0];
        Assert.Equal(criada.Id, recarregada.Id);
        Assert.Equal("GitHub", recarregada.NomeServico);
        Assert.Equal("https://github.com", recarregada.Url);

        Assert.Equal("GitHub@Secreta123", cripto2.Descriptografar(recarregada.SenhaHash));
    }

    [Fact]
    public async Task Recarregar_ComSenhaMestraErrada_FalhaEmVezDeRevelarDados()
    {
        var auth = new AutenticacaoMestra(_pasta);
        var chaveCerta = auth.CriarSenhaMestra("SenhaCorreta@123");

        var (servico1, _) = MontarCofre(chaveCerta);
        await servico1.CriarSenhaAsync("Banco", "cliente", "Banco@Forte123", Categoria.Finance);
        await servico1.PersistirAsync();

        var (servico2, _) = MontarCofre(NovaChave());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servico2.ListarTodosAsync());
    }

    [Fact]
    public async Task FluxoCompleto_AtualizarFavoritarRemover_RefleteNoDiscoAposRecarga()
    {
        var chave = NovaChave();

        var (servico1, _) = MontarCofre(chave);
        var s = await servico1.CriarSenhaAsync("Gmail", "user@gmail.com", "Gmail@Senha123", Categoria.Personal);
        await servico1.MarcarComoFavoritoAsync(s.Id);
        await servico1.AtualizarSenhaAsync(s.Id, "Gmail", "novo@gmail.com", "NovaGmail@456", Categoria.Personal);
        await servico1.PersistirAsync();

        var (servico2, cripto2) = MontarCofre(chave);
        var recarregada = await servico2.ObterSenhaAsync(s.Id);

        Assert.NotNull(recarregada);
        Assert.True(recarregada!.Favorito);
        Assert.Equal("novo@gmail.com", recarregada.Usuario);
        Assert.Equal("NovaGmail@456", cripto2.Descriptografar(recarregada.SenhaHash));

        await servico2.RemoverSenhaAsync(s.Id);
        await servico2.PersistirAsync();

        var (servico3, _) = MontarCofre(chave);
        Assert.Empty(await servico3.ListarTodosAsync());
    }

    [Fact]
    public async Task ArquivoEmDisco_NaoContemSenhaNemJsonEmTextoClaro()
    {
        var chave = NovaChave();
        var (servico, _) = MontarCofre(chave);

        await servico.CriarSenhaAsync("Servico", "user", "SenhaSuperSecreta@123", Categoria.Personal);
        await servico.PersistirAsync();

        var conteudo = await File.ReadAllTextAsync(Path.Combine(_pasta, "senhas.json.enc"));

        Assert.DoesNotContain("SenhaSuperSecreta@123", conteudo);

        Assert.DoesNotContain("NomeServico", conteudo);
    }

    [Fact]
    public async Task BuscaEFiltros_AposRecarregar_FuncionamCorretamente()
    {
        var chave = NovaChave();

        var (servico1, _) = MontarCofre(chave);
        await servico1.CriarSenhaAsync("Gmail", "a@gmail.com", "Senha@Forte123", Categoria.Personal);
        await servico1.CriarSenhaAsync("Gmail Work", "b@gmail.com", "Senha@Forte123", Categoria.Work);
        await servico1.CriarSenhaAsync("GitHub", "c@git.com", "Senha@Forte123", Categoria.Work);
        await servico1.PersistirAsync();

        var (servico2, _) = MontarCofre(chave);

        Assert.Equal(3, (await servico2.ListarTodosAsync()).Count);
        Assert.Equal(2, (await servico2.BuscarPorServicoAsync("gmail")).Count);
        Assert.Equal(2, (await servico2.ListarPorCategoriaAsync(Categoria.Work)).Count);
    }
}
