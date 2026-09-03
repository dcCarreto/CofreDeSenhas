using System.Diagnostics;
using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;
using Xunit.Abstractions;

namespace GerenciadorDeSenhas.Testes;

public class PerformanceTests : IDisposable
{
    // Margens largas de propósito: isto é um alarme contra regressão grosseira
    // (ex.: um loop O(n²) introduzido por acidente), não um SLA de performance.
    // Não apertar com base em uma máquina rápida específica.
    private const long LimiteCriarMs = 15000;
    private const long LimiteCarregarMs = 15000;

    private readonly string _pasta;
    private readonly ITestOutputHelper _saida;

    public PerformanceTests(ITestOutputHelper saida)
    {
        _saida = saida;
        _pasta = PastaTemporariaTeste.Criar("GS_Perf");
    }

    public void Dispose() => PastaTemporariaTeste.Apagar(_pasta);

    private (IServicoSenha servico, IServicoCriptografia cripto) MontarCofre(byte[] chave)
    {
        var cripto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(cripto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        return (new ServicoSenha(repo, cripto), cripto);
    }

    [Fact]
    public async Task PersistirERecarregar_1000Senhas_DentroDoLimiteDeTempo()
    {
        const int total = 1000;
        var chave = new byte[32];
        RandomNumberGenerator.Fill(chave);

        var (servico1, _) = MontarCofre(chave);

        var swCriar = Stopwatch.StartNew();
        for (int i = 0; i < total; i++)
            await servico1.CriarSenhaAsync($"Servico{i}", $"user{i}@mail.com", $"Senha@Forte{i}", (Categoria)(i % 5));
        await servico1.PersistirAsync();
        swCriar.Stop();

        var (servico2, cripto2) = MontarCofre(chave);
        var swCarregar = Stopwatch.StartNew();
        var todas = await servico2.ListarTodosAsync();
        swCarregar.Stop();

        _saida.WriteLine($"Criar+persistir {total}: {swCriar.ElapsedMilliseconds} ms | Recarregar: {swCarregar.ElapsedMilliseconds} ms");

        Assert.Equal(total, todas.Count);

        var amostra = todas[total / 2];
        var indice = int.Parse(amostra.NomeServico.Replace("Servico", ""));
        Assert.Equal($"Senha@Forte{indice}", cripto2.Descriptografar(amostra.SenhaHash));

        Assert.True(swCriar.ElapsedMilliseconds < LimiteCriarMs, $"Criação/persistência demorou {swCriar.ElapsedMilliseconds} ms");
        Assert.True(swCarregar.ElapsedMilliseconds < LimiteCarregarMs, $"Recarga demorou {swCarregar.ElapsedMilliseconds} ms");
    }
}
