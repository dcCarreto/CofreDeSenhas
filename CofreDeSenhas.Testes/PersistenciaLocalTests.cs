using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Testes;

public class PersistenciaLocalTests
{
    [Fact]
    public async Task SalvarComSeguranca_ComEstadoAtual_Grava()
    {
        using var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("A", "a", "x");

        var cripto = new ServicoCriptografia(cofre.Chave);
        var persistencia = new PersistenciaLocal(cripto, cofre.Pasta);
        var lista = await persistencia.CarregarSenhasAsync(cofre.Chave);

        var estadoAtual = persistencia.ObterEstadoArquivo();
        var novoEstado = await persistencia.SalvarSenhasComSegurancaAsync(lista, cofre.Chave, estadoAtual);

        Assert.True(novoEstado.Existe);
        Assert.NotEqual(estadoAtual.Hash, novoEstado.Hash);
    }

    [Fact]
    public async Task SalvarComSeguranca_ComEstadoDesatualizado_DetectaConflito()
    {
        using var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("A", "a", "x");

        var cripto = new ServicoCriptografia(cofre.Chave);
        var persistencia = new PersistenciaLocal(cripto, cofre.Pasta);
        var lista = await persistencia.CarregarSenhasAsync(cofre.Chave);
        var estadoAntigo = persistencia.ObterEstadoArquivo();

        await cofre.AdicionarPelaBaseAsync("B", "b", "y");

        await Assert.ThrowsAsync<ConflitoGravacaoCofreException>(() =>
            persistencia.SalvarSenhasComSegurancaAsync(lista, cofre.Chave, estadoAntigo));
    }
}
