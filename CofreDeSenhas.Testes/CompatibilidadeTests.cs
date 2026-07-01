using CofreDeSenhas.Nucleo;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas.Testes;

public class CompatibilidadeTests
{
    [Fact]
    public async Task Extensao_LeCredencialCriadaPeloApp()
    {
        using var cofre = new CofreTemporario();
        var original = await cofre.AdicionarPelaBaseAsync(
            "Banco", "cliente@banco.com", "senha-banco", "https://banco.com", Categoria.Finance, "conta", true);

        using var sessao = new SessaoCofre(cofre.Pasta, TimeSpan.FromMinutes(30));
        Assert.True(await sessao.DestrancarAsync(cofre.SenhaMestra));

        var detalhe = sessao.ObterDetalhes(original.Id);
        Assert.NotNull(detalhe);
        Assert.Equal("Banco", detalhe!.Value.Servico);
        Assert.Equal("Finance", detalhe.Value.Categoria);
        Assert.True(detalhe.Value.Favorito);
        Assert.Equal("senha-banco", sessao.ObterCredencial(original.Id)!.Value.Senha);
    }

    [Fact]
    public async Task App_LeItemAdicionadoPelaExtensao_ComTodosOsCampos()
    {
        using var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("Semente", "seed", "s", "https://semente.com");

        using var sessao = new SessaoCofre(cofre.Pasta, TimeSpan.FromMinutes(30));
        await sessao.DestrancarAsync(cofre.SenhaMestra);

        var adicao = await sessao.AdicionarCredencialAsync(
            new NovaCredencial("Loja", "comprador@loja.com", "senha-loja", "https://loja.com.br"));
        Assert.True(adicao.Ok);
        var id = adicao.Item!.Value.Id;

        var edicao = await sessao.EditarCredencialAsync(new EdicaoCredencial(
            id, "Loja Premium", "vip@loja.com", "senha-vip",
            "https://vip.loja.com.br", "Personal", "cliente vip", true));
        Assert.True(edicao.Ok);

        var salva = (await cofre.LerPelaBaseAsync()).Single(s => s.Id == id);
        Assert.Equal("Loja Premium", salva.NomeServico);
        Assert.Equal("vip@loja.com", salva.Usuario);
        Assert.Equal("https://vip.loja.com.br", salva.Url);
        Assert.Equal(Categoria.Personal, salva.Categoria);
        Assert.Equal("cliente vip", salva.Notas);
        Assert.True(salva.Favorito);
        Assert.Equal("senha-vip", cofre.SenhaEmClaroPelaBase(salva));
    }

    [Fact]
    public async Task Sessao_OperaSomenteComArquivos_SemAppRodando()
    {
        string pasta;
        using (var cofre = new CofreTemporario())
        {
            pasta = cofre.Pasta;
            await cofre.AdicionarPelaBaseAsync("Servico", "usuario", "senha", "https://exemplo.com");

            using var sessao = new SessaoCofre(pasta, TimeSpan.FromMinutes(30));
            Assert.True(await sessao.DestrancarAsync(cofre.SenhaMestra));

            var id = sessao.Consultar("exemplo.com")[0].Id;
            Assert.Equal("senha", sessao.ObterCredencial(id)!.Value.Senha);

            var adicao = await sessao.AdicionarCredencialAsync(
                new NovaCredencial("Outro", "u2", "s2", "https://outro.com"));
            Assert.True(adicao.Ok);
        }

        Assert.False(Directory.Exists(pasta));
    }
}
