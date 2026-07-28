using System.Security.Cryptography;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoMudancaSenhaMestraTests : IDisposable
{
    private readonly string _pasta;

    public ServicoMudancaSenhaMestraTests()
    {
        _pasta = Path.Combine(Path.GetTempPath(), "GS_MudSenha_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true); } catch { }
    }

    private async Task PrepararCofre(string senhaMestra, params (string nome, string usuario, string senha)[] itens)
    {
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.CriarSenhaMestra(senhaMestra);
        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        var servico = new ServicoSenha(repo, crypto);
        foreach (var (nome, usuario, senha) in itens)
            await servico.CriarSenhaAsync(nome, usuario, senha, Categoria.Personal);
        await servico.PersistirAsync();
    }

    [Fact]
    public async Task AlterarAsync_ComSenhaCorreta_TrocaChaveEPreservaSenhas()
    {
        await PrepararCofre("SenhaAntiga@123",
            ("GitHub", "dev@git.com", "GitHub@Secreta1"),
            ("Gmail", "user@gmail.com", "Gmail@Secreta2"));

        await new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        var auth = new AutenticacaoMestra(_pasta);
        Assert.Null(auth.Autenticar("SenhaAntiga@123"));
        var chaveNova = auth.Autenticar("SenhaNova@456");
        Assert.NotNull(chaveNova);

        var crypto = new ServicoCriptografia(chaveNova!);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var senhas = await persist.CarregarSenhasAsync(chaveNova!);
        Assert.Equal(2, senhas.Count);
        var git = senhas.Single(s => s.NomeServico == "GitHub");
        Assert.Equal("GitHub@Secreta1", crypto.Descriptografar(git.SenhaHash));
    }

    [Fact]
    public async Task AlterarAsync_ComSenhaAtualErrada_LancaInvalidOperationENaoAltera()
    {
        await PrepararCofre("SenhaAntiga@123", ("Svc", "u", "Senha@Forte1"));

        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaErrada@999", "SenhaNova@456"));

        Assert.NotNull(new AutenticacaoMestra(_pasta).Autenticar("SenhaAntiga@123"));
    }

    [Fact]
    public async Task AlterarAsync_ComNovaSenhaCurta_LancaArgumentException()
    {
        await PrepararCofre("SenhaAntiga@123", ("Svc", "u", "Senha@Forte1"));

        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaAntiga@123", "curta"));
    }

    [Fact]
    public async Task AlterarAsync_PreservaTotpEHistoricoSobNovaChave()
    {
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.CriarSenhaMestra("SenhaAntiga@123");
        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        var servico = new ServicoSenha(repo, crypto);

        var senha = await servico.CriarSenhaAsync("GitHub", "dev@git.com", "Primeira@111",
            Categoria.Personal, totpSegredo: "JBSWY3DPEHPK3PXP");
        await servico.AtualizarSenhaAsync(senha.Id, "GitHub", "dev@git.com", "Segunda@222", Categoria.Personal);
        await servico.PersistirAsync();

        await new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        var chaveNova = new AutenticacaoMestra(_pasta).Autenticar("SenhaNova@456");
        Assert.NotNull(chaveNova);

        var cryptoNovo = new ServicoCriptografia(chaveNova!);
        var persistNovo = new PersistenciaLocal(cryptoNovo, _pasta);
        var senhas = await persistNovo.CarregarSenhasAsync(chaveNova!);
        var git = senhas.Single(s => s.NomeServico == "GitHub");

        Assert.Equal("Segunda@222", cryptoNovo.Descriptografar(git.SenhaHash));
        Assert.NotNull(git.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", cryptoNovo.Descriptografar(git.TotpSegredo!));
        var anterior = Assert.Single(git.Historico);
        Assert.Equal("Primeira@111", cryptoNovo.Descriptografar(anterior.SenhaHash));
    }

    [Fact]
    public async Task AlterarAsync_CofreVazio_FuncionaSemErro()
    {
        new AutenticacaoMestra(_pasta).CriarSenhaMestra("SenhaAntiga@123");

        await new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        var auth = new AutenticacaoMestra(_pasta);
        Assert.Null(auth.Autenticar("SenhaAntiga@123"));
        Assert.NotNull(auth.Autenticar("SenhaNova@456"));
    }

    [Fact]
    public async Task AlterarAsync_RetornaAMesmaChaveQueAAutenticacaoPosterior()
    {
        await PrepararCofre("SenhaAntiga@123", ("Svc", "u", "Senha@Forte1"));

        var chaveRetornada = await new ServicoMudancaSenhaMestra(_pasta)
            .AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        var chaveAutenticada = new AutenticacaoMestra(_pasta).Autenticar("SenhaNova@456");
        Assert.Equal(chaveAutenticada, chaveRetornada);
    }

    [Fact]
    public async Task AlterarAsync_RecifraCamposExtrasCodigosRecuperacaoEAnexos()
    {
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.CriarSenhaMestra("SenhaAntiga@123");
        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        var servico = new ServicoSenha(repo, crypto);
        var servicoAnexos = new ServicoAnexos(crypto, _pasta);

        var camposExtras = new Dictionary<string, string> { ["cvv"] = "123" };
        var senha = await servico.CriarSenhaAsync("Banco", "titular", "Senha@Forte1", Categoria.Personal,
            tipo: TipoCredencial.Cartao, camposExtras: camposExtras);
        await servico.AdicionarCodigosRecuperacaoAsync(senha.Id, new[] { ("CODIGO-1", false) });
        var anexo = await servicoAnexos.AdicionarAsync(senha, "chave.txt", "conteudo-secreto"u8.ToArray());
        await servico.PersistirAsync();

        await new ServicoMudancaSenhaMestra(_pasta).AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        var chaveNova = new AutenticacaoMestra(_pasta).Autenticar("SenhaNova@456");
        Assert.NotNull(chaveNova);

        var cryptoNovo = new ServicoCriptografia(chaveNova!);
        var persistNovo = new PersistenciaLocal(cryptoNovo, _pasta);
        var senhas = await persistNovo.CarregarSenhasAsync(chaveNova!);
        var recarregada = Assert.Single(senhas);

        Assert.Equal("123", cryptoNovo.Descriptografar(recarregada.CamposExtras["cvv"]));
        var codigo = Assert.Single(recarregada.CodigosRecuperacao);
        Assert.Equal("CODIGO-1", cryptoNovo.Descriptografar(codigo.Codigo));

        var anexosNovo = new ServicoAnexos(cryptoNovo, _pasta);
        var conteudoAnexo = await anexosNovo.LerAsync(recarregada.Anexos.Single(a => a.Id == anexo.Id));
        Assert.Equal("conteudo-secreto", System.Text.Encoding.UTF8.GetString(conteudoAnexo));
    }

    [Fact]
    public void RestaurarBackupOrfaoSeNecessario_ComBackupsOrfaos_RestauraEApagaOsBak()
    {
        var auth = new AutenticacaoMestra(_pasta);
        auth.CriarSenhaMestra("SenhaOriginal@123");

        var authPath = Path.Combine(_pasta, "auth.dat");
        var vaultPath = Path.Combine(_pasta, "senhas.json.enc");
        var authBak = authPath + ".bak";
        var vaultBak = vaultPath + ".bak";

        File.Copy(authPath, authBak);
        File.WriteAllText(vaultPath, "estado-anterior-valido");
        File.Copy(vaultPath, vaultBak);

        File.WriteAllText(authPath, "lixo-de-escrita-interrompida");
        File.WriteAllText(vaultPath, "lixo-de-escrita-interrompida");

        new ServicoMudancaSenhaMestra(_pasta).RestaurarBackupOrfaoSeNecessario();

        Assert.False(File.Exists(authBak));
        Assert.False(File.Exists(vaultBak));
        Assert.Equal("estado-anterior-valido", File.ReadAllText(vaultPath));
        Assert.NotNull(new AutenticacaoMestra(_pasta).Autenticar("SenhaOriginal@123"));
    }

    [Fact]
    public async Task MigrarKdfSeNecessarioAsync_ComCofreJaAtualizado_NaoAlteraNadaERetornaNull()
    {
        await PrepararCofre("SenhaAtual@123", ("Svc", "u", "Senha@Forte1"));

        var resultado = await new ServicoMudancaSenhaMestra(_pasta)
            .MigrarKdfSeNecessarioAsync("SenhaAtual@123");

        Assert.Null(resultado);
        Assert.NotNull(new AutenticacaoMestra(_pasta).Autenticar("SenhaAtual@123"));
    }

    [Fact]
    public async Task MigrarKdfSeNecessarioAsync_ComCofreNoFormatoLegado_MigraParaArgon2idEPreservaSenhas()
    {
        var chaveLegada = PrepararCofreLegado(_pasta, "SenhaAtual@123", 100_000,
            ("GitHub", "dev@git.com", "GitHub@Secreta1"));

        Assert.True(new AutenticacaoMestra(_pasta).KdfDesatualizado());

        var novaChave = await new ServicoMudancaSenhaMestra(_pasta)
            .MigrarKdfSeNecessarioAsync("SenhaAtual@123");

        Assert.NotNull(novaChave);
        Assert.NotEqual(chaveLegada, novaChave);
        Assert.False(new AutenticacaoMestra(_pasta).KdfDesatualizado());

        var chaveReautenticada = new AutenticacaoMestra(_pasta).Autenticar("SenhaAtual@123");
        Assert.Equal(novaChave, chaveReautenticada);

        var crypto = new ServicoCriptografia(novaChave!);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var senhas = await persist.CarregarSenhasAsync(novaChave!);
        var git = Assert.Single(senhas);
        Assert.Equal("GitHub@Secreta1", crypto.Descriptografar(git.SenhaHash));
    }

    private static byte[] PrepararCofreLegado(string pasta, string senhaMestra, int iteracoesLegado,
        params (string nome, string usuario, string senha)[] itens)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var chave = Rfc2898DeriveBytes.Pbkdf2(senhaMestra, salt, iteracoesLegado, HashAlgorithmName.SHA256, 32);
        var verificador = SHA256.HashData(chave);

        var dados = new byte[16 + 32];
        Buffer.BlockCopy(salt, 0, dados, 0, 16);
        Buffer.BlockCopy(verificador, 0, dados, 16, 32);
        File.WriteAllText(Path.Combine(pasta, "auth.dat"), Convert.ToBase64String(dados));

        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, pasta);
        var repo = new RepositorioSenha(persist, chave);
        var servico = new ServicoSenha(repo, crypto);
        foreach (var (nome, usuario, senha) in itens)
            servico.CriarSenhaAsync(nome, usuario, senha, Categoria.Personal).GetAwaiter().GetResult();
        servico.PersistirAsync().GetAwaiter().GetResult();

        return chave;
    }
}
