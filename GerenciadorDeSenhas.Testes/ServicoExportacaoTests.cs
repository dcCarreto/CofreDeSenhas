using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoExportacaoTests : IDisposable
{
    private readonly string _pasta;
    private readonly ServicoExportacao _servico = new();

    public ServicoExportacaoTests()
    {
        _pasta = PastaTemporariaTeste.Criar("GS_Export");
    }

    public void Dispose() => PastaTemporariaTeste.Apagar(_pasta);

    private string Caminho() => Path.Combine(_pasta, "cofre.gsenhas");

    private static List<SenhaExportada> Amostra() => new()
    {
        new() { NomeServico = "GitHub", Usuario = "dev@git.com", Senha = "GitHub@Secreta123", Categoria = Categoria.Work, Etiquetas = new() { "Dev", "Clientes" }, Url = "https://github.com", Favorito = true },
        new() { NomeServico = "Gmail", Usuario = "user@gmail.com", Senha = "Gmail@Forte456", Categoria = Categoria.Personal }
    };

    [Fact]
    public async Task ExportarEImportar_ComMesmaSenha_PreservaTodosOsDados()
    {
        var originais = Amostra();

        await _servico.ExportarAsync(Caminho(), originais, "SenhaExport@123");
        var importadas = await _servico.ImportarAsync(Caminho(), "SenhaExport@123");

        Assert.Equal(2, importadas.Count);
        var git = importadas.Single(s => s.NomeServico == "GitHub");
        Assert.Equal("dev@git.com", git.Usuario);
        Assert.Equal("GitHub@Secreta123", git.Senha);
        Assert.Equal(Categoria.Work, git.Categoria);
        Assert.Equal(new[] { "Dev", "Clientes" }, git.Etiquetas);
        Assert.Equal("https://github.com", git.Url);
        Assert.True(git.Favorito);
    }

    [Fact]
    public async Task Importar_ComSenhaErrada_LancaInvalidOperation()
    {
        await _servico.ExportarAsync(Caminho(), Amostra(), "SenhaCerta@123");

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.ImportarAsync(Caminho(), "SenhaErrada@999"));
        Assert.Equal("Export.Error.WrongPassword", ex.Chave);
    }

    [Fact]
    public async Task ArquivoExportado_NaoContemSenhaEmTextoClaro()
    {
        await _servico.ExportarAsync(Caminho(), Amostra(), "SenhaExport@123");

        var conteudo = await File.ReadAllTextAsync(Caminho());
        Assert.DoesNotContain("GitHub@Secreta123", conteudo);
        Assert.DoesNotContain("Gmail@Forte456", conteudo);
        Assert.DoesNotContain("dev@git.com", conteudo);
    }

    [Fact]
    public async Task Importar_ComArquivoAdulterado_LancaInvalidOperation()
    {
        await _servico.ExportarAsync(Caminho(), Amostra(), "SenhaExport@123");

        var texto = await File.ReadAllTextAsync(Caminho());
        int posDados = texto.IndexOf("\"Dados\"", StringComparison.Ordinal);
        int aspaInicio = texto.IndexOf('"', texto.IndexOf(':', posDados) + 1);
        int alvo = aspaInicio + 1;
        char novo = texto[alvo] == 'A' ? 'B' : 'A';
        texto = texto.Substring(0, alvo) + novo + texto.Substring(alvo + 1);
        await File.WriteAllTextAsync(Caminho(), texto);

        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.ImportarAsync(Caminho(), "SenhaExport@123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta")]
    public async Task Exportar_ComSenhaInvalida_LancaArgumentException(string senha)
    {
        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.ExportarAsync(Caminho(), Amostra(), senha));
    }

    [Fact]
    public async Task Exportar_UsaArgon2idComoKdf()
    {
        await _servico.ExportarAsync(Caminho(), Amostra(), "SenhaExport@123");

        var conteudo = await File.ReadAllTextAsync(Caminho());
        using var doc = JsonDocument.Parse(conteudo);

        Assert.Equal("Argon2id", doc.RootElement.GetProperty("Kdf").GetString());
    }

    [Fact]
    public async Task Importar_ArquivoComParametrosArgon2idZerados_CaiParaPadraoEmVezDeExcecaoCrua()
    {
        // "Iteracoes"/"MemoriaKb"/"Paralelismo" zerados (arquivo adulterado ou de uma
        // versão futura com outro default) antes caíam direto na biblioteca Argon2id e
        // explodiam com uma exceção não tratada. Agora caem pros valores padrão e o
        // fluxo segue normalmente até a checagem de senha — nunca uma exceção crua.
        var envelopeAdulterado = new
        {
            Versao = 1,
            Kdf = "Argon2id",
            Iteracoes = 0,
            MemoriaKb = 0,
            Paralelismo = 0,
            Salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            Iv = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)),
            Tag = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            Dados = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        };
        await File.WriteAllTextAsync(Caminho(), JsonSerializer.Serialize(envelopeAdulterado));

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.ImportarAsync(Caminho(), "SenhaExport@123"));

        Assert.Equal("Export.Error.WrongPassword", ex.Chave);
    }

    [Fact]
    public async Task Importar_ArquivoComParametrosArgon2idInvalidos_LancaErroLocalizavelEmVezDeExcecaoCrua()
    {
        // Combinação positiva mas inválida pra Argon2id (memória insuficiente para o
        // grau de paralelismo pedido) — precisa ser barrada antes de chegar na AES-GCM.
        var envelopeAdulterado = new
        {
            Versao = 1,
            Kdf = "Argon2id",
            Iteracoes = 1,
            MemoriaKb = 1,
            Paralelismo = 10_000,
            Salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            Iv = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)),
            Tag = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            Dados = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        };
        await File.WriteAllTextAsync(Caminho(), JsonSerializer.Serialize(envelopeAdulterado));

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.ImportarAsync(Caminho(), "SenhaExport@123"));

        Assert.Equal("Export.Error.InvalidFile", ex.Chave);
    }

    [Fact]
    public async Task Exportar_QuandoDestinoNaoPodeSerSubstituido_NaoDeixaArquivoTmpOrfao()
    {
        // Ocupa o caminho de destino com um diretório, forçando o passo final da
        // escrita atômica (File.Move) a falhar depois que o .tmp já foi criado.
        Directory.CreateDirectory(Caminho());

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _servico.ExportarAsync(Caminho(), Amostra(), "SenhaExport@123"));

        Assert.Empty(Directory.GetFiles(_pasta, "*.tmp"));
    }

    [Fact]
    public async Task Importar_ArquivoAntigoEmPbkdf2_AindaFunciona()
    {
        const string senha = "SenhaExport@123";
        const int iteracoesLegado = 200_000;

        var salt = RandomNumberGenerator.GetBytes(16);
        var iv = RandomNumberGenerator.GetBytes(12);
        var chave = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoesLegado, HashAlgorithmName.SHA256, 32);

        var json = JsonSerializer.Serialize(Amostra());
        var textoBytes = Encoding.UTF8.GetBytes(json);
        var cifrado = new byte[textoBytes.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(chave, 16))
            aes.Encrypt(iv, textoBytes, cifrado, tag);

        var envelopeLegado = new
        {
            Versao = 1,
            Kdf = "PBKDF2-SHA256",
            Iteracoes = iteracoesLegado,
            Salt = Convert.ToBase64String(salt),
            Iv = Convert.ToBase64String(iv),
            Tag = Convert.ToBase64String(tag),
            Dados = Convert.ToBase64String(cifrado)
        };
        await File.WriteAllTextAsync(Caminho(), JsonSerializer.Serialize(envelopeLegado));

        var importadas = await _servico.ImportarAsync(Caminho(), senha);

        Assert.Equal(2, importadas.Count);
        Assert.Contains(importadas, s => s.NomeServico == "GitHub" && s.Senha == "GitHub@Secreta123");
    }
}
