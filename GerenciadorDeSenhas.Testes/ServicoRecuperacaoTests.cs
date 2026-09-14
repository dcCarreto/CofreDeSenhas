using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoRecuperacaoTests : IDisposable
{
    private readonly string _pasta;
    public ServicoRecuperacaoTests() => _pasta = PastaTemporariaTeste.Criar("GS_Recup");
    public void Dispose() => PastaTemporariaTeste.Apagar(_pasta);

    private static byte[] ChaveAleatoria()
    {
        var c = new byte[32];
        RandomNumberGenerator.Fill(c);
        return c;
    }

    [Fact]
    public void HabilitarERecuperar_DevolveAMesmaChaveMestra()
    {
        var chave = ChaveAleatoria();
        var svc = new ServicoRecuperacao(_pasta);

        Assert.False(svc.EstaHabilitada());
        var segredo = svc.Habilitar(chave);
        Assert.True(svc.EstaHabilitada());

        var recuperada = svc.Recuperar(segredo);
        Assert.NotNull(recuperada);
        Assert.Equal(chave, recuperada);
    }

    [Fact]
    public void Recuperar_SegredoErrado_RetornaNulo()
    {
        var svc = new ServicoRecuperacao(_pasta);
        svc.Habilitar(ChaveAleatoria());

        Assert.Null(svc.Recuperar("AAAA-AAAA-AAAA-AAAA-AAAA-AAAA-AAAA-AAAA"));
        Assert.Null(svc.Recuperar("nao e uma chave"));
        Assert.Null(svc.Recuperar(""));
    }

    [Fact]
    public void Recuperar_ArquivoAdulterado_RetornaNulo()
    {
        var svc = new ServicoRecuperacao(_pasta);
        var segredo = svc.Habilitar(ChaveAleatoria());

        var caminho = Path.Combine(_pasta, "recuperacao.dat");
        var bytes = Convert.FromBase64String(File.ReadAllText(caminho));
        bytes[^1] ^= 0xFF;
        File.WriteAllText(caminho, Convert.ToBase64String(bytes));

        Assert.Null(svc.Recuperar(segredo));
    }

    [Fact]
    public void Recuperar_SemHabilitar_RetornaNulo()
    {
        Assert.Null(new ServicoRecuperacao(_pasta).Recuperar("AAAA-AAAA-AAAA-AAAA-AAAA-AAAA-AAAA-AAAA"));
    }

    [Fact]
    public void Recuperar_ToleraFormatoDigitadoAMao()
    {
        var chave = ChaveAleatoria();
        var svc = new ServicoRecuperacao(_pasta);
        var segredo = svc.Habilitar(chave);

        var semSeparador = segredo.Replace("-", "");
        var minusculoComEspacos = segredo.ToLowerInvariant().Replace("-", " ");

        Assert.Equal(chave, svc.Recuperar(semSeparador));
        Assert.Equal(chave, svc.Recuperar(minusculoComEspacos));
    }

    [Fact]
    public void SegredoExibido_TemGruposDe4()
    {
        var segredo = new ServicoRecuperacao(_pasta).Habilitar(ChaveAleatoria());
        var grupos = segredo.Split('-');
        Assert.Equal(8, grupos.Length); // 160 bits -> 32 base32 -> 8 grupos de 4
        Assert.All(grupos, g => Assert.Equal(4, g.Length));
    }

    [Fact]
    public void Desabilitar_ApagaOArquivo()
    {
        var svc = new ServicoRecuperacao(_pasta);
        svc.Habilitar(ChaveAleatoria());
        Assert.True(svc.EstaHabilitada());
        svc.Desabilitar();
        Assert.False(svc.EstaHabilitada());
    }

    [Fact]
    public async Task FluxoCompleto_RecuperaChaveEDefineSenhaNova_CofreAbreComElaEOsSegredosSobrevivem()
    {
        // cria o cofre
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.CriarSenhaMestra("SenhaEsquecida@1");
        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        var servico = new ServicoSenha(repo, crypto);
        await servico.CriarSenhaAsync("GitHub", "dev@git.com", "S3nhaDoGitHub!", Categoria.Personal);
        await servico.PersistirAsync();

        // habilita recuperação com a chave mestra atual
        var recup = new ServicoRecuperacao(_pasta);
        var segredo = recup.Habilitar(auth.Autenticar("SenhaEsquecida@1")!);

        // "esqueci a senha" -> recupera a chave e troca por uma nova
        var chaveRecuperada = recup.Recuperar(segredo);
        Assert.NotNull(chaveRecuperada);

        var chaveNova = await new ServicoMudancaSenhaMestra(_pasta).AlterarComChaveAsync(chaveRecuperada!, "SenhaNovaEmUso@2");

        // a senha antiga não abre mais; a nova abre e a credencial está intacta
        Assert.Null(auth.Autenticar("SenhaEsquecida@1"));
        var chavePelaNova = auth.Autenticar("SenhaNovaEmUso@2");
        Assert.NotNull(chavePelaNova);

        var cryptoNovo = new ServicoCriptografia(chaveNova);
        var persistNovo = new PersistenciaLocal(cryptoNovo, _pasta);
        var senhas = await persistNovo.CarregarSenhasAsync(chaveNova);
        Assert.Single(senhas);
        Assert.Equal("S3nhaDoGitHub!", cryptoNovo.Descriptografar(senhas[0].SenhaHash));

        // recuperação re-habilitada com a chave nova volta a funcionar
        var segredo2 = recup.Habilitar(chaveNova);
        Assert.Equal(chaveNova, recup.Recuperar(segredo2));
    }
}
