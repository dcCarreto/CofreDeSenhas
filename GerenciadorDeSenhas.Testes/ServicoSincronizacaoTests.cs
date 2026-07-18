using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoSincronizacaoTests : IDisposable
{
    private readonly string _pastaTemp;

    public ServicoSincronizacaoTests()
    {
        _pastaTemp = Path.Combine(Path.GetTempPath(), "GS_Sync_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pastaTemp);
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

    private static SenhaExportada CriarItem(Guid id, DateTime dataAtualizacao, string nomeServico = "Gmail") => new()
    {
        Id = id,
        NomeServico = nomeServico,
        Usuario = "user@gmail.com",
        Senha = "Senha@123",
        Categoria = Categoria.Personal,
        DataCriacao = dataAtualizacao,
        DataAtualizacao = dataAtualizacao
    };

    [Fact]
    public void MesclarListas_ComMesmoId_ManteRegistroMaisRecente()
    {
        var id = Guid.NewGuid();
        var local = CriarItem(id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Antigo");
        var remoto = CriarItem(id, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), "Novo");

        var resultado = ServicoSincronizacao.MesclarListas(new[] { local }, new[] { remoto });

        var item = Assert.Single(resultado);
        Assert.Equal("Novo", item.NomeServico);
    }

    [Fact]
    public void MesclarListas_RemotoMaisAntigo_MantemLocal()
    {
        var id = Guid.NewGuid();
        var local = CriarItem(id, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), "Local");
        var remoto = CriarItem(id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Remoto");

        var resultado = ServicoSincronizacao.MesclarListas(new[] { local }, new[] { remoto });

        var item = Assert.Single(resultado);
        Assert.Equal("Local", item.NomeServico);
    }

    [Fact]
    public void MesclarListas_ItemSoLocal_Preservado()
    {
        var local = CriarItem(Guid.NewGuid(), DateTime.UtcNow);

        var resultado = ServicoSincronizacao.MesclarListas(new[] { local }, Array.Empty<SenhaExportada>());

        Assert.Single(resultado);
    }

    [Fact]
    public void MesclarListas_ItemSoRemoto_Adicionado()
    {
        var remoto = CriarItem(Guid.NewGuid(), DateTime.UtcNow);

        var resultado = ServicoSincronizacao.MesclarListas(Array.Empty<SenhaExportada>(), new[] { remoto });

        Assert.Single(resultado);
    }

    [Fact]
    public void MesclarListas_ComMultiplosItens_ResolveCadaUmIndependentemente()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();

        var locais = new[]
        {
            CriarItem(idA, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "A-local-vence"),
            CriarItem(idB, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "B-so-local")
        };
        var remotos = new[]
        {
            CriarItem(idA, new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), "A-remoto-perde"),
            CriarItem(idC, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "C-so-remoto")
        };

        var resultado = ServicoSincronizacao.MesclarListas(locais, remotos);

        Assert.Equal(3, resultado.Count);
        Assert.Equal("A-local-vence", resultado.Single(i => i.Id == idA).NomeServico);
        Assert.Equal("B-so-local", resultado.Single(i => i.Id == idB).NomeServico);
        Assert.Equal("C-so-remoto", resultado.Single(i => i.Id == idC).NomeServico);
    }

    [Fact]
    public async Task EscreverELer_ComChaveCorreta_RoundTripPreservaItens()
    {
        var chave = RandomNumberGenerator.GetBytes(32);
        var servico = new ServicoSincronizacao(new ServicoCriptografia(chave));
        var caminho = Path.Combine(_pastaTemp, "sync.dat");
        var salt = ServicoSincronizacao.GerarSalt();
        var itens = new List<SenhaExportada> { CriarItem(Guid.NewGuid(), DateTime.UtcNow) };

        await servico.EscreverAsync(caminho, salt, ServicoSincronizacao.Iteracoes, itens);
        var lidos = await servico.LerAsync(caminho);

        var item = Assert.Single(lidos);
        Assert.Equal(itens[0].Id, item.Id);
        Assert.Equal(itens[0].NomeServico, item.NomeServico);
        Assert.Equal(itens[0].Senha, item.Senha);
    }

    [Fact]
    public async Task Ler_ComChaveErrada_RetornaListaVazia()
    {
        var chaveEscrita = RandomNumberGenerator.GetBytes(32);
        var chaveLeitura = RandomNumberGenerator.GetBytes(32);
        var caminho = Path.Combine(_pastaTemp, "sync.dat");

        var servicoEscrita = new ServicoSincronizacao(new ServicoCriptografia(chaveEscrita));
        await servicoEscrita.EscreverAsync(caminho, ServicoSincronizacao.GerarSalt(), ServicoSincronizacao.Iteracoes,
            new List<SenhaExportada> { CriarItem(Guid.NewGuid(), DateTime.UtcNow) });

        var servicoLeitura = new ServicoSincronizacao(new ServicoCriptografia(chaveLeitura));
        var lidos = await servicoLeitura.LerAsync(caminho);

        Assert.Empty(lidos);
    }

    [Fact]
    public async Task Ler_ArquivoInexistente_RetornaListaVazia()
    {
        var servico = new ServicoSincronizacao(new ServicoCriptografia(RandomNumberGenerator.GetBytes(32)));
        var lidos = await servico.LerAsync(Path.Combine(_pastaTemp, "nao-existe.dat"));

        Assert.Empty(lidos);
    }

    [Fact]
    public async Task LerCabecalho_ArquivoInexistente_RetornaNulo()
    {
        var resultado = await ServicoSincronizacao.LerCabecalhoAsync(Path.Combine(_pastaTemp, "nao-existe.dat"));
        Assert.Null(resultado);
    }

    [Fact]
    public async Task LerCabecalho_ArquivoExistente_RetornaSaltEIteracoes()
    {
        var chave = RandomNumberGenerator.GetBytes(32);
        var servico = new ServicoSincronizacao(new ServicoCriptografia(chave));
        var caminho = Path.Combine(_pastaTemp, "sync.dat");
        var salt = ServicoSincronizacao.GerarSalt();

        await servico.EscreverAsync(caminho, salt, ServicoSincronizacao.Iteracoes, new List<SenhaExportada>());

        var cabecalho = await ServicoSincronizacao.LerCabecalhoAsync(caminho);

        Assert.NotNull(cabecalho);
        Assert.Equal(salt, cabecalho.Value.Salt);
        Assert.Equal(ServicoSincronizacao.Iteracoes, cabecalho.Value.Iteracoes);
    }

    [Fact]
    public void DerivarChave_ComMesmaSenhaESalt_ProduzChaveIgual()
    {
        var salt = ServicoSincronizacao.GerarSalt();
        var chave1 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt);
        var chave2 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt);

        Assert.Equal(chave1, chave2);
    }

    [Fact]
    public void DerivarChave_ComSaltsDiferentes_ProduzChavesDiferentes()
    {
        var chave1 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", ServicoSincronizacao.GerarSalt());
        var chave2 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", ServicoSincronizacao.GerarSalt());

        Assert.NotEqual(chave1, chave2);
    }
}
