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

        _pastaTemp = PastaTemporariaTeste.Criar("GS_Anexos");

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
    public async Task Remover_QuandoArquivoCifradoNaoPodeSerApagado_RegistraAvisoMasRemoveReferenciaMesmoAssim()
    {
        var senha = NovaSenha();
        var anexo = await _servico.AdicionarAsync(senha, "recibo.pdf", new byte[] { 1, 2, 3 });

        // Substitui o arquivo cifrado por um diretório no mesmo caminho, forçando
        // File.Delete a falhar (simula arquivo bloqueado por antivírus/permissão).
        var caminho = Path.Combine(_pastaTemp, "anexos", anexo.Id.ToString("N") + ".enc");
        File.Delete(caminho);
        Directory.CreateDirectory(caminho);

        _servico.Remover(senha, anexo.Id);

        Assert.Empty(senha.Anexos);
        Assert.NotEmpty(_servico.UltimosAvisos);
    }

    [Fact]
    public async Task Remover_ChamadoNovamenteComSucesso_LimpaAvisoDaChamadaAnterior()
    {
        var senha = NovaSenha();
        var anexo1 = await _servico.AdicionarAsync(senha, "recibo1.pdf", new byte[] { 1, 2, 3 });
        var anexo2 = await _servico.AdicionarAsync(senha, "recibo2.pdf", new byte[] { 4, 5, 6 });

        var caminho1 = Path.Combine(_pastaTemp, "anexos", anexo1.Id.ToString("N") + ".enc");
        File.Delete(caminho1);
        Directory.CreateDirectory(caminho1);
        _servico.Remover(senha, anexo1.Id);
        Assert.NotEmpty(_servico.UltimosAvisos);

        _servico.Remover(senha, anexo2.Id);

        Assert.Empty(_servico.UltimosAvisos);
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
    public async Task RemoverTodos_QuandoUmDosArquivosCifradosNaoPodeSerApagado_RegistraAvisoMasRemoveTodasAsReferencias()
    {
        // RemoverTodos é chamado ao excluir definitivamente uma credencial e ao
        // esvaziar a lixeira (código de exclusão de dados) — se um dos vários anexos
        // falhar ao apagar (ex.: antivírus/permissão), o laço precisa continuar pros
        // demais em vez de abortar no meio, e a lista de anexos da credencial precisa
        // ficar vazia de qualquer jeito (senão a UI mostraria anexos fantasmas sem
        // arquivo correspondente).
        var senha = NovaSenha();
        var anexoBloqueado = await _servico.AdicionarAsync(senha, "bloqueado.pdf", new byte[] { 1, 2, 3 });
        var anexoNormal = await _servico.AdicionarAsync(senha, "normal.pdf", new byte[] { 4, 5, 6 });

        var caminhoBloqueado = Path.Combine(_pastaTemp, "anexos", anexoBloqueado.Id.ToString("N") + ".enc");
        File.Delete(caminhoBloqueado);
        Directory.CreateDirectory(caminhoBloqueado);
        var caminhoNormal = Path.Combine(_pastaTemp, "anexos", anexoNormal.Id.ToString("N") + ".enc");

        _servico.RemoverTodos(senha);

        Assert.Empty(senha.Anexos);
        Assert.NotEmpty(_servico.UltimosAvisos);
        Assert.False(File.Exists(caminhoNormal));
        Assert.True(Directory.Exists(caminhoBloqueado));
    }

    [Fact]
    public async Task RemoverTodos_EmLote_AcumulaAvisosDeTodosOsItensEmVezDeSoODoUltimo()
    {
        // A sobrecarga em lote existe justamente pra "esvaziar lixeira" (que chama
        // RemoverTodos várias vezes, uma por credencial, numa operação só) não
        // perder o aviso de um item porque o item seguinte limpou UltimosAvisos de
        // novo — RemoverTodos(Senha) sozinho, chamado num loop externo, só deixava
        // sobreviver o aviso do último item processado.
        var senha1 = NovaSenha();
        var anexo1 = await _servico.AdicionarAsync(senha1, "bloqueado1.pdf", new byte[] { 1 });
        var caminho1 = Path.Combine(_pastaTemp, "anexos", anexo1.Id.ToString("N") + ".enc");
        File.Delete(caminho1);
        Directory.CreateDirectory(caminho1);

        var senha2 = NovaSenha();
        var anexo2 = await _servico.AdicionarAsync(senha2, "bloqueado2.pdf", new byte[] { 2 });
        var caminho2 = Path.Combine(_pastaTemp, "anexos", anexo2.Id.ToString("N") + ".enc");
        File.Delete(caminho2);
        Directory.CreateDirectory(caminho2);

        _servico.RemoverTodos(new[] { senha1, senha2 });

        Assert.Empty(senha1.Anexos);
        Assert.Empty(senha2.Anexos);
        Assert.Equal(2, _servico.UltimosAvisos.Count);
    }

    [Fact]
    public async Task TamanhoTotalAtual_SomaOTamanhoDeTodosOsArquivosGravados()
    {
        var senha = NovaSenha();
        await _servico.AdicionarAsync(senha, "um.txt", new byte[100]);
        await _servico.AdicionarAsync(senha, "dois.txt", new byte[200]);

        var total = _servico.TamanhoTotalAtual();

        // Cada arquivo cifrado tem 12 bytes de nonce + 16 de tag do AES-GCM a mais
        // que o conteúdo original — 100+28 e 200+28.
        Assert.Equal(356, total);
    }

    [Fact]
    public async Task AdicionarAsync_AoExcederLimiteTotalDoCofre_LancaLimiteAnexoExcedido()
    {
        var senha = NovaSenha();
        var pastaAnexos = Path.Combine(_pastaTemp, "anexos");
        Directory.CreateDirectory(pastaAnexos);

        // Simula o cofre já quase no limite de 100 MB escrevendo um arquivo grande
        // direto em disco — bem mais rápido que gravar 100 MB de anexos de verdade
        // pela API (que precisaria cifrar tudo).
        var jaOcupado = ServicoAnexos.TamanhoMaximoTotalCofre - 1000;
        await File.WriteAllBytesAsync(Path.Combine(pastaAnexos, "preenchimento.enc"), new byte[jaOcupado]);

        var excecao = await Assert.ThrowsAsync<LimiteAnexoExcedidoException>(() =>
            _servico.AdicionarAsync(senha, "excede.bin", new byte[2000]));

        Assert.Equal("Attachment.Error.VaultLimit", excecao.Chave);
    }

    [Fact]
    public async Task AdicionarAsync_ComDuasChamadasConcorrentesPertoDoLimiteTotal_ApenasUmaConsegue()
    {
        // A trava (_travaEscrita) existe justamente pra impedir que duas chamadas
        // concorrentes passem ambas da checagem de TamanhoTotalAtual() antes de
        // qualquer uma escrever, ultrapassando os 100MB. Deixa só 1000 bytes de
        // margem e tenta adicionar dois anexos de 700 bytes ao mesmo tempo — cada um
        // cabe sozinho na margem, mas os dois juntos não. Sem a trava, as duas
        // chamadas veriam TamanhoTotalAtual() com o valor antigo e passariam da
        // checagem antes de qualquer uma escrever.
        var pastaAnexos = Path.Combine(_pastaTemp, "anexos");
        Directory.CreateDirectory(pastaAnexos);

        var jaOcupado = ServicoAnexos.TamanhoMaximoTotalCofre - 1000;
        await File.WriteAllBytesAsync(Path.Combine(pastaAnexos, "preenchimento.enc"), new byte[jaOcupado]);

        var senha1 = NovaSenha();
        var senha2 = NovaSenha();

        async Task<bool> TentarAsync(Senha senha, string nome)
        {
            try
            {
                await _servico.AdicionarAsync(senha, nome, new byte[700]);
                return true;
            }
            catch (LimiteAnexoExcedidoException)
            {
                return false;
            }
        }

        var resultados = await Task.WhenAll(TentarAsync(senha1, "um.bin"), TentarAsync(senha2, "dois.bin"));

        Assert.Equal(1, resultados.Count(sucesso => sucesso));
        Assert.Equal(1, resultados.Count(sucesso => !sucesso));
    }

    public void Dispose() => PastaTemporariaTeste.Apagar(_pastaTemp);
}
