using System.Reflection;
using CofreDeSenhas.Nucleo;

namespace CofreDeSenhas.Testes;

public class SessaoCofreTests
{
    private static async Task<(CofreTemporario cofre, SessaoCofre sessao)> DestrancadaAsync()
    {
        var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("GitHub", "dev@exemplo.com", "senha-gh", "https://github.com/login");
        var sessao = new SessaoCofre(cofre.Pasta, TimeSpan.FromMinutes(30));
        Assert.True(await sessao.DestrancarAsync(cofre.SenhaMestra));
        return (cofre, sessao);
    }

    [Fact]
    public async Task Destrancar_ComSenhaCorreta_Abre()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            Assert.True(sessao.Destrancado);
            Assert.Equal(1, sessao.Total);
        }
    }

    [Fact]
    public async Task Destrancar_ComSenhaErrada_NaoAbre()
    {
        using var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("Servico", "usuario", "senha");
        using var sessao = new SessaoCofre(cofre.Pasta, TimeSpan.FromMinutes(30));

        Assert.False(await sessao.DestrancarAsync("senha-errada-999"));
        Assert.False(sessao.Destrancado);
    }

    [Fact]
    public async Task Destrancar_SemCofrePronto_NaoAbre()
    {
        using var cofre = new CofreTemporario();
        using var sessao = new SessaoCofre(cofre.Pasta, TimeSpan.FromMinutes(30));

        Assert.False(await sessao.DestrancarAsync(cofre.SenhaMestra));
    }

    [Fact]
    public async Task Consultar_RetornaCredencialDoDominio()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var itens = sessao.Consultar("www.github.com");
            Assert.Single(itens);
            Assert.Equal("GitHub", itens[0].Servico);
        }
    }

    [Fact]
    public async Task Consultar_IgnoraOutroDominio()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            Assert.Empty(sessao.Consultar("gitlab.com"));
        }
    }

    [Fact]
    public async Task Buscar_EncontraPorServico()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            Assert.Single(sessao.Buscar("github"));
            Assert.Empty(sessao.Buscar("inexistente"));
        }
    }

    [Fact]
    public async Task ObterCredencial_DescriptografaSenha()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var id = sessao.Consultar("github.com")[0].Id;
            var cred = sessao.ObterCredencial(id);
            Assert.NotNull(cred);
            Assert.Equal("dev@exemplo.com", cred!.Value.Usuario);
            Assert.Equal("senha-gh", cred.Value.Senha);
        }
    }

    [Fact]
    public async Task Adicionar_Persiste_ELidoPelaBibliotecaBase()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var r = await sessao.AdicionarCredencialAsync(
                new NovaCredencial("Exemplo", "novo@exemplo.com", "senha-nova", "https://exemplo.com"));
            Assert.True(r.Ok);
            Assert.NotNull(r.Item);

            var todas = await cofre.LerPelaBaseAsync();
            var salva = todas.Single(s => s.Id == r.Item!.Value.Id);
            Assert.Equal("Exemplo", salva.NomeServico);
            Assert.Equal("senha-nova", cofre.SenhaEmClaroPelaBase(salva));
        }
    }

    [Fact]
    public async Task Adicionar_ComCamposObrigatoriosVazios_Falha()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var r = await sessao.AdicionarCredencialAsync(new NovaCredencial("", "u", "s", null));
            Assert.False(r.Ok);
            Assert.Equal("servico_obrigatorio", r.Erro);
        }
    }

    [Fact]
    public async Task Adicionar_Duplicado_Falha()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var r = await sessao.AdicionarCredencialAsync(
                new NovaCredencial("GitHub", "dev@exemplo.com", "outra", "https://github.com"));
            Assert.False(r.Ok);
            Assert.Equal("duplicado", r.Erro);
        }
    }

    [Fact]
    public async Task Editar_AlteraCampos_ELidoPelaBase()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var id = sessao.Consultar("github.com")[0].Id;
            var r = await sessao.EditarCredencialAsync(new EdicaoCredencial(
                id, "GitHub Edit", "novo@exemplo.com", "senha-edit",
                "https://github.com/login", "Work", "anotacao", true));
            Assert.True(r.Ok);

            var salva = (await cofre.LerPelaBaseAsync()).Single(s => s.Id == id);
            Assert.Equal("GitHub Edit", salva.NomeServico);
            Assert.Equal("novo@exemplo.com", salva.Usuario);
            Assert.True(salva.Favorito);
            Assert.Equal("anotacao", salva.Notas);
            Assert.Equal("senha-edit", cofre.SenhaEmClaroPelaBase(salva));
        }
    }

    [Fact]
    public async Task Editar_ComSenhaEmBranco_MantemSenhaAtual()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var id = sessao.Consultar("github.com")[0].Id;
            var r = await sessao.EditarCredencialAsync(new EdicaoCredencial(
                id, "GitHub", "dev@exemplo.com", "", "https://github.com", "Other", null, false));
            Assert.True(r.Ok);

            var salva = (await cofre.LerPelaBaseAsync()).Single(s => s.Id == id);
            Assert.Equal("senha-gh", cofre.SenhaEmClaroPelaBase(salva));
        }
    }

    [Fact]
    public async Task Editar_Inexistente_Falha()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var r = await sessao.EditarCredencialAsync(new EdicaoCredencial(
                Guid.NewGuid(), "X", "y", "z", null, "Other", null, false));
            Assert.False(r.Ok);
            Assert.Equal("nao_encontrado", r.Erro);
        }
    }

    [Fact]
    public async Task Trancar_LimpaSessao()
    {
        var (cofre, sessao) = await DestrancadaAsync();
        using (cofre)
        using (sessao)
        {
            var id = sessao.Consultar("github.com")[0].Id;
            sessao.Trancar();

            Assert.False(sessao.Destrancado);
            Assert.Equal(0, sessao.Total);
            Assert.Null(sessao.ObterCredencial(id));
            Assert.Empty(sessao.Consultar("github.com"));
        }
    }

    [Fact]
    public void SessaoCofre_NaoExpoeOperacaoDeRemocao()
    {
        var nomes = typeof(SessaoCofre)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name);

        Assert.DoesNotContain(nomes, n =>
            n.Contains("Remover", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Excluir", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Apagar", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Delete", StringComparison.OrdinalIgnoreCase));
    }
}
