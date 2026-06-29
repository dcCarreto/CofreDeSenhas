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
        _pasta = Path.Combine(Path.GetTempPath(), "GS_Export_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true); } catch { }
    }

    private string Caminho() => Path.Combine(_pasta, "cofre.gsenhas");

    private static List<SenhaExportada> Amostra() => new()
    {
        new() { NomeServico = "GitHub", Usuario = "dev@git.com", Senha = "GitHub@Secreta123", Categoria = Categoria.Work, Url = "https://github.com", Favorito = true },
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
        Assert.Equal("https://github.com", git.Url);
        Assert.True(git.Favorito);
    }

    [Fact]
    public async Task Importar_ComSenhaErrada_LancaInvalidOperation()
    {
        await _servico.ExportarAsync(Caminho(), Amostra(), "SenhaCerta@123");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servico.ImportarAsync(Caminho(), "SenhaErrada@999"));
        Assert.Contains("incorreta", ex.Message);
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

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servico.ImportarAsync(Caminho(), "SenhaExport@123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta")]
    public async Task Exportar_ComSenhaInvalida_LancaArgumentException(string senha)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _servico.ExportarAsync(Caminho(), Amostra(), senha));
    }
}
