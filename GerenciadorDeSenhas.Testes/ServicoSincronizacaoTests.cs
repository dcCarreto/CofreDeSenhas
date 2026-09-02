using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoSincronizacaoTests : IDisposable
{
    private readonly string _pastaTemp;

    public ServicoSincronizacaoTests()
    {
        _pastaTemp = PastaTemporariaTeste.Criar("GS_Sync");
    }

    public void Dispose() => PastaTemporariaTeste.Apagar(_pastaTemp);

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
    public void MesclarListas_CodigosRecuperacaoDivergentesNosDoisLados_UneEmVezDeDescartar()
    {
        var id = Guid.NewGuid();
        var local = CriarItem(id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Local");
        local.CodigosRecuperacao.Add(new CodigoRecuperacaoExportado { Codigo = "codigo-local" });

        var remoto = CriarItem(id, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), "Remoto");
        remoto.CodigosRecuperacao.Add(new CodigoRecuperacaoExportado { Codigo = "codigo-remoto" });

        var resultado = ServicoSincronizacao.MesclarListas(new[] { local }, new[] { remoto });

        var item = Assert.Single(resultado);
        Assert.Contains(item.CodigosRecuperacao, c => c.Codigo == "codigo-local");
        Assert.Contains(item.CodigosRecuperacao, c => c.Codigo == "codigo-remoto");
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
        var padrao = ServicoSincronizacao.ParametrosPadrao();

        await servico.EscreverAsync(caminho, salt, padrao.Kdf, padrao.Iteracoes, padrao.MemoriaKb, padrao.Paralelismo, itens);
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
        var padrao = ServicoSincronizacao.ParametrosPadrao();

        var servicoEscrita = new ServicoSincronizacao(new ServicoCriptografia(chaveEscrita));
        await servicoEscrita.EscreverAsync(caminho, ServicoSincronizacao.GerarSalt(), padrao.Kdf, padrao.Iteracoes,
            padrao.MemoriaKb, padrao.Paralelismo, new List<SenhaExportada> { CriarItem(Guid.NewGuid(), DateTime.UtcNow) });

        var servicoLeitura = new ServicoSincronizacao(new ServicoCriptografia(chaveLeitura));
        var lidos = await servicoLeitura.LerAsync(caminho);

        Assert.Empty(lidos);
    }

    [Fact]
    public async Task Escrever_ComCaminhoInvalido_LancaErroLocalizavelComCausaOriginal()
    {
        var arquivoConflitante = Path.Combine(_pastaTemp, "nao-e-uma-pasta");
        await File.WriteAllTextAsync(arquivoConflitante, "conteudo");
        var caminhoInvalido = Path.Combine(arquivoConflitante, "sub", "sincronizacao.dat");

        var servico = new ServicoSincronizacao(new ServicoCriptografia(RandomNumberGenerator.GetBytes(32)));

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            servico.EscreverAsync(caminhoInvalido, ServicoSincronizacao.GerarSalt(), null,
                ServicoSincronizacao.Iteracoes, null, null, new List<SenhaExportada>()));

        Assert.Equal("Sync.Error.WriteFailed", ex.Chave);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task Escrever_QuandoDestinoNaoPodeSerSubstituido_NaoDeixaArquivoTmpOrfao()
    {
        var caminho = Path.Combine(_pastaTemp, "sincronizacao.dat");
        // Ocupa o caminho de destino com um diretório, forçando o passo final da
        // escrita atômica (File.Move) a falhar depois que o .tmp já foi criado.
        Directory.CreateDirectory(caminho);

        var servico = new ServicoSincronizacao(new ServicoCriptografia(RandomNumberGenerator.GetBytes(32)));

        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            servico.EscreverAsync(caminho, ServicoSincronizacao.GerarSalt(), null,
                ServicoSincronizacao.Iteracoes, null, null, new List<SenhaExportada>()));

        Assert.Empty(Directory.GetFiles(_pastaTemp, "*.tmp"));
    }

    [Fact]
    public async Task Ler_ArquivoInexistente_RetornaListaVazia()
    {
        var servico = new ServicoSincronizacao(new ServicoCriptografia(RandomNumberGenerator.GetBytes(32)));
        var lidos = await servico.LerAsync(Path.Combine(_pastaTemp, "nao-existe.dat"));

        Assert.Empty(lidos);
    }

    [Fact]
    public async Task Ler_ComArquivoTravadoPorOutroProcesso_LancaEmVezDeRetornarListaVazia()
    {
        // Diferente de "chave errada" ou "arquivo ausente" (que viram lista vazia de
        // propósito): um arquivo que existe mas não pode ser lido agora — pasta de nuvem
        // ainda baixando, outro dispositivo escrevendo nele nesse instante — precisa
        // propagar. SincronizarAsync mescla o retorno com o cofre local e regrava o
        // arquivo remoto por cima; se isso virasse lista vazia como os outros casos, o
        // sync apagaria do remoto qualquer coisa que outro dispositivo ainda não tinha
        // terminado de publicar.
        var caminho = Path.Combine(_pastaTemp, "sincronizacao.dat");
        await File.WriteAllTextAsync(caminho, "conteudo qualquer");

        var servico = new ServicoSincronizacao(new ServicoCriptografia(RandomNumberGenerator.GetBytes(32)));

        using var travamento = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.None);
        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() => servico.LerAsync(caminho));

        Assert.Equal("Sync.Error.ReadFailed", ex.Chave);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task LerAsync_ComCamposNulosExplicitosNoJson_SanitizaEmVezDePropagarNull()
    {
        // EscreverAsync nunca produz "Etiquetas": null (OpcoesJson omite o campo em
        // vez de escrever null) — mas um sincronizacao.dat escrito por outra versão
        // do app, ou corrompido, pode chegar assim mesmo. Forja isso na mão, no
        // mesmo formato de envelope que EscreverAsync produz.
        var chave = RandomNumberGenerator.GetBytes(32);
        var criptografia = new ServicoCriptografia(chave);
        var servico = new ServicoSincronizacao(criptografia);

        var jsonComCamposNulos = "[{\"Id\":\"" + Guid.NewGuid() + "\",\"NomeServico\":\"Gmail\",\"Usuario\":\"u\"," +
            "\"Senha\":\"s\",\"Etiquetas\":null,\"Historico\":null,\"CodigosRecuperacao\":null,\"CamposExtras\":null,\"Anexos\":null}]";
        var bytesCifrados = criptografia.CriptografarBytes(Encoding.UTF8.GetBytes(jsonComCamposNulos));

        var envelopeJson = JsonSerializer.Serialize(new
        {
            Versao = 1,
            Salt = Convert.ToBase64String(ServicoSincronizacao.GerarSalt()),
            Kdf = (string?)null,
            Iteracoes = 0,
            MemoriaKb = (int?)null,
            Paralelismo = (int?)null,
            Dados = Convert.ToBase64String(bytesCifrados)
        });

        var caminho = Path.Combine(_pastaTemp, "sincronizacao.dat");
        await File.WriteAllTextAsync(caminho, envelopeJson);

        var lidos = await servico.LerAsync(caminho);

        var item = Assert.Single(lidos);
        Assert.NotNull(item.Etiquetas);
        Assert.Empty(item.Etiquetas);
        Assert.NotNull(item.Historico);
        Assert.Empty(item.Historico);
        Assert.NotNull(item.CodigosRecuperacao);
        Assert.NotNull(item.CamposExtras);
        Assert.NotNull(item.Anexos);
    }

    [Fact]
    public async Task LerCabecalho_ArquivoInexistente_RetornaNulo()
    {
        var resultado = await ServicoSincronizacao.LerCabecalhoAsync(Path.Combine(_pastaTemp, "nao-existe.dat"));
        Assert.Null(resultado);
    }

    [Fact]
    public async Task LerCabecalho_ArquivoExistente_RetornaSaltKdfEIteracoes()
    {
        var chave = RandomNumberGenerator.GetBytes(32);
        var servico = new ServicoSincronizacao(new ServicoCriptografia(chave));
        var caminho = Path.Combine(_pastaTemp, "sync.dat");
        var salt = ServicoSincronizacao.GerarSalt();
        var padrao = ServicoSincronizacao.ParametrosPadrao();

        await servico.EscreverAsync(caminho, salt, padrao.Kdf, padrao.Iteracoes, padrao.MemoriaKb, padrao.Paralelismo,
            new List<SenhaExportada>());

        var cabecalho = await ServicoSincronizacao.LerCabecalhoAsync(caminho);

        Assert.NotNull(cabecalho);
        Assert.Equal(salt, cabecalho.Value.Salt);
        Assert.Equal(padrao.Kdf, cabecalho.Value.Kdf);
        Assert.Equal(padrao.Iteracoes, cabecalho.Value.Iteracoes);
        Assert.Equal(padrao.MemoriaKb, cabecalho.Value.MemoriaKb);
        Assert.Equal(padrao.Paralelismo, cabecalho.Value.Paralelismo);
    }

    [Fact]
    public void ParametrosPadrao_UsaArgon2id()
    {
        var padrao = ServicoSincronizacao.ParametrosPadrao();

        Assert.Equal(ServicoSincronizacao.KdfArgon2id, padrao.Kdf);
    }

    [Fact]
    public void DerivarChave_Argon2id_ComMesmaSenhaESalt_ProduzChaveIgual()
    {
        var salt = ServicoSincronizacao.GerarSalt();
        var padrao = ServicoSincronizacao.ParametrosPadrao();
        var chave1 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt, padrao.Kdf, padrao.Iteracoes, padrao.MemoriaKb, padrao.Paralelismo);
        var chave2 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt, padrao.Kdf, padrao.Iteracoes, padrao.MemoriaKb, padrao.Paralelismo);

        Assert.Equal(chave1, chave2);
    }

    [Fact]
    public void DerivarChave_Argon2id_ComSaltsDiferentes_ProduzChavesDiferentes()
    {
        var padrao = ServicoSincronizacao.ParametrosPadrao();
        var chave1 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", ServicoSincronizacao.GerarSalt(), padrao.Kdf, padrao.Iteracoes, padrao.MemoriaKb, padrao.Paralelismo);
        var chave2 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", ServicoSincronizacao.GerarSalt(), padrao.Kdf, padrao.Iteracoes, padrao.MemoriaKb, padrao.Paralelismo);

        Assert.NotEqual(chave1, chave2);
    }

    [Fact]
    public void DerivarChave_KdfNulo_UsaPbkdf2Legado()
    {
        var salt = ServicoSincronizacao.GerarSalt();

        var chaveViaKdfNulo = ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt, kdf: null, ServicoSincronizacao.Iteracoes);
        var chaveEsperada = Rfc2898DeriveBytes.Pbkdf2("SenhaMestra@123", salt, ServicoSincronizacao.Iteracoes, HashAlgorithmName.SHA256, 32);

        Assert.Equal(chaveEsperada, chaveViaKdfNulo);
    }

    [Fact]
    public void DerivarChave_Argon2idEPbkdf2_ProduzemChavesDiferentes()
    {
        var salt = ServicoSincronizacao.GerarSalt();
        var padrao = ServicoSincronizacao.ParametrosPadrao();

        var chaveArgon2id = ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt, padrao.Kdf, padrao.Iteracoes, padrao.MemoriaKb, padrao.Paralelismo);
        var chavePbkdf2 = ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt, kdf: null, ServicoSincronizacao.Iteracoes);

        Assert.NotEqual(chaveArgon2id, chavePbkdf2);
    }

    [Fact]
    public void DerivarChave_CabecalhoPedindoCustoAbsurdo_FalhaRapidoEmVezDeTravar()
    {
        var salt = ServicoSincronizacao.GerarSalt();

        var relogio = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt, "Argon2id", 3, int.MaxValue, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ServicoSincronizacao.DerivarChave("SenhaMestra@123", salt, kdf: null, int.MaxValue));
        relogio.Stop();

        Assert.True(relogio.Elapsed < TimeSpan.FromSeconds(5), $"demorou {relogio.Elapsed}");
    }
}
