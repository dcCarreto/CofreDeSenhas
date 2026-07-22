using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoAnexosTests : IDisposable
{
    private readonly ServicoCriptografia _criptografia;
    private readonly ServicoAnexos _servico;
    private readonly string _pastaTemp;

    public ServicoAnexosTests()
    {
        var chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(chave);

        _pastaTemp = Path.Combine(Path.GetTempPath(), "GS_Anexos_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pastaTemp);

        _criptografia = new ServicoCriptografia(chave);
        _servico = new ServicoAnexos(_criptografia, _pastaTemp);
    }

    private static Senha NovaSenha() => new()
    {
        NomeServico = "Servico",
        Usuario = "usuario",
        SenhaHash = "irrelevante-para-o-teste"
    };

    [Fact]
    public async Task AdicionarAsync_ComArquivoValido_AdicionaAosAnexosERetornaMetadados()
    {
        var senha = NovaSenha();
        var conteudo = new byte[] { 1, 2, 3, 4, 5 };

        var anexo = await _servico.AdicionarAsync(senha, "recibo.pdf", conteudo);

        Assert.Single(senha.Anexos);
        Assert.Equal("recibo.pdf", anexo.NomeArquivo);
        Assert.Equal(conteudo.Length, anexo.TamanhoBytes);
    }

    [Fact]
    public async Task ApagarTudo_ComAnexosExistentes_RemovePastaDeAnexos()
    {
        var senha = NovaSenha();
        await _servico.AdicionarAsync(senha, "recibo.pdf", new byte[] { 1, 2, 3 });
        var pastaAnexos = Path.Combine(_pastaTemp, "anexos");
        Assert.True(Directory.Exists(pastaAnexos));

        _servico.ApagarTudo();

        Assert.False(Directory.Exists(pastaAnexos));
    }

    [Fact]
    public void ApagarTudo_SemAnexos_NaoLancaExcecao()
    {
        _servico.ApagarTudo();
    }

    [Fact]
    public async Task AdicionarAsync_DepoisLerAsync_RetornaConteudoOriginalIntacto()
    {
        var senha = NovaSenha();
        var conteudo = new byte[4096];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(conteudo);

        var anexo = await _servico.AdicionarAsync(senha, "imagem.png", conteudo);
        var lido = await _servico.LerAsync(anexo);

        Assert.Equal(conteudo, lido);
    }

    [Fact]
    public async Task AdicionarAsync_GravaArquivoCifradoEmDisco_DiferenteDoConteudoOriginal()
    {
        var senha = NovaSenha();
        var conteudo = System.Text.Encoding.UTF8.GetBytes("texto secreto identificavel");

        await _servico.AdicionarAsync(senha, "notas.txt", conteudo);

        var arquivoGravado = Directory.GetFiles(Path.Combine(_pastaTemp, "anexos")).Single();
        var bytesGravados = await File.ReadAllBytesAsync(arquivoGravado);

        Assert.NotEqual(conteudo, bytesGravados);
        Assert.True(bytesGravados.Length > conteudo.Length);
    }

    [Fact]
    public async Task AdicionarAsync_ComArquivoMaiorQueOLimite_LancaLimiteAnexoExcedido()
    {
        var senha = NovaSenha();
        var conteudo = new byte[ServicoAnexos.TamanhoMaximoPorAnexo + 1];

        await Assert.ThrowsAsync<LimiteAnexoExcedidoException>(() =>
            _servico.AdicionarAsync(senha, "grande.bin", conteudo));
    }

    [Fact]
    public async Task AdicionarAsync_AoAtingirQuantidadeMaximaPorCredencial_LancaLimiteAnexoExcedido()
    {
        var senha = NovaSenha();
        for (var i = 0; i < ServicoAnexos.QuantidadeMaximaPorCredencial; i++)
            await _servico.AdicionarAsync(senha, $"arquivo{i}.txt", new byte[] { 1 });

        await Assert.ThrowsAsync<LimiteAnexoExcedidoException>(() =>
            _servico.AdicionarAsync(senha, "excedente.txt", new byte[] { 1 }));
    }

    [Fact]
    public async Task Remover_ApagaDosAnexosEDoDisco()
    {
        var senha = NovaSenha();
        var anexo = await _servico.AdicionarAsync(senha, "a-remover.txt", new byte[] { 1, 2, 3 });
        var caminhoArquivo = Directory.GetFiles(Path.Combine(_pastaTemp, "anexos")).Single();

        _servico.Remover(senha, anexo.Id);

        Assert.Empty(senha.Anexos);
        Assert.False(File.Exists(caminhoArquivo));
    }

    [Fact]
    public async Task RemoverTodos_ApagaTodosOsAnexosDaCredencial()
    {
        var senha = NovaSenha();
        await _servico.AdicionarAsync(senha, "um.txt", new byte[] { 1 });
        await _servico.AdicionarAsync(senha, "dois.txt", new byte[] { 2 });

        _servico.RemoverTodos(senha);

        Assert.Empty(senha.Anexos);
        Assert.Empty(Directory.GetFiles(Path.Combine(_pastaTemp, "anexos")));
    }

    [Fact]
    public async Task TamanhoTotalAtual_SomaOTamanhoDeTodosOsArquivosGravados()
    {
        var senha = NovaSenha();
        await _servico.AdicionarAsync(senha, "um.txt", new byte[100]);
        await _servico.AdicionarAsync(senha, "dois.txt", new byte[200]);

        var total = _servico.TamanhoTotalAtual();

        Assert.True(total > 0);
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
}
