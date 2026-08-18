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
        _pasta = PastaTemporariaTeste.Criar("GS_MudSenha");
    }

    public void Dispose() => PastaTemporariaTeste.Apagar(_pasta);

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
    public async Task AlterarAsync_ComHistoricoCorrompido_NaoFalhaMasRegistraAviso()
    {
        await PrepararCofre("SenhaAntiga@123", ("GitHub", "dev@git.com", "GitHub@Secreta1"));

        // Simula dado corrompido em disco: um histórico que não decifra nem com a
        // chave certa (nem chega a ser Base64 válido).
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.Autenticar("SenhaAntiga@123")!;
        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var senhas = await persist.CarregarSenhasAsync(chave);
        var item = senhas.Single();
        item.Historico.Add(new HistoricoSenha { SenhaHash = "@@@nao-e-base64@@@", DataAlteracao = DateTime.UtcNow });
        await persist.SalvarSenhasAsync(senhas, chave);

        var servico = new ServicoMudancaSenhaMestra(_pasta);
        var chaveNova = await servico.AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        Assert.NotEmpty(servico.UltimosAvisos);

        var cryptoNovo = new ServicoCriptografia(chaveNova);
        var persistNovo = new PersistenciaLocal(cryptoNovo, _pasta);
        var recarregadas = await persistNovo.CarregarSenhasAsync(chaveNova);
        Assert.Single(recarregadas);
        Assert.Equal("GitHub@Secreta1", cryptoNovo.Descriptografar(recarregadas[0].SenhaHash));
    }

    [Fact]
    public async Task AlterarAsync_ComSenhaHashCorrompida_LancaErroLocalizavelIdentificandoACredencialENaoMutaNadaEmDisco()
    {
        await PrepararCofre("SenhaAntiga@123",
            ("GitHub", "dev@git.com", "GitHub@Secreta1"),
            ("Gmail", "user@gmail.com", "Gmail@Secreta2"));

        // Simula dado corrompido em disco: a própria senha da credencial não decifra.
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.Autenticar("SenhaAntiga@123")!;
        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var senhas = await persist.CarregarSenhasAsync(chave);
        var item = senhas.Single(s => s.NomeServico == "Gmail");
        item.SenhaHash = "@@@nao-e-base64@@@";
        await persist.SalvarSenhasAsync(senhas, chave);

        var servico = new ServicoMudancaSenhaMestra(_pasta);
        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() => servico.AlterarAsync("SenhaAntiga@123", "SenhaNova@456"));

        Assert.Equal("Master.Error.CorruptedEntry", ex.Chave);
        Assert.Equal("Gmail", ex.Argumentos.Single());

        // A senha mestra atual (e o cofre) continuam intactos após a falha.
        Assert.True(auth.ValidarChave(chave));
        var aindaLegiveis = await persist.CarregarSenhasAsync(chave);
        Assert.Equal(2, aindaLegiveis.Count);
        Assert.Equal("GitHub@Secreta1", crypto.Descriptografar(aindaLegiveis.Single(s => s.NomeServico == "GitHub").SenhaHash));
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

        // O marcador de conclusão (escrito antes do "return", pra distinguir uma
        // troca já bem-sucedida de uma interrompida no meio — ver
        // RestaurarBackupOrfaoSeNecessario) precisa sumir junto com os .bak numa
        // troca normal, sem sobrar lixo.
        Assert.False(File.Exists(Path.Combine(_pasta, "troca_senha.ok")));
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
    public async Task AlterarAsync_ComAnexoCorrompido_NaoFalhaMasDescartaERegistraAviso()
    {
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.CriarSenhaMestra("SenhaAntiga@123");
        var crypto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(crypto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        var servico = new ServicoSenha(repo, crypto);
        var servicoAnexos = new ServicoAnexos(crypto, _pasta);

        var senha = await servico.CriarSenhaAsync("Banco", "titular", "Senha@Forte1", Categoria.Personal);
        var anexoBom = await servicoAnexos.AdicionarAsync(senha, "bom.txt", "conteudo-ok"u8.ToArray());
        var anexoRuim = await servicoAnexos.AdicionarAsync(senha, "ruim.txt", "sera-corrompido"u8.ToArray());
        await servico.PersistirAsync();

        // Simula dado corrompido em disco: o arquivo cifrado do anexo é sobrescrito
        // com lixo que não decifra nem com a chave certa.
        var caminhoAnexoRuim = Path.Combine(_pasta, "anexos", anexoRuim.Id.ToString("N") + ".enc");
        File.WriteAllText(caminhoAnexoRuim, "nao-e-um-anexo-cifrado-valido");

        var servicoMudanca = new ServicoMudancaSenhaMestra(_pasta);
        var chaveNova = await servicoMudanca.AlterarAsync("SenhaAntiga@123", "SenhaNova@456");

        Assert.NotEmpty(servicoMudanca.UltimosAvisos);

        var cryptoNovo = new ServicoCriptografia(chaveNova);
        var persistNovo = new PersistenciaLocal(cryptoNovo, _pasta);
        var recarregada = Assert.Single(await persistNovo.CarregarSenhasAsync(chaveNova));

        var anexoRestante = Assert.Single(recarregada.Anexos);
        Assert.Equal(anexoBom.Id, anexoRestante.Id);

        var anexosNovo = new ServicoAnexos(cryptoNovo, _pasta);
        var conteudoAnexo = await anexosNovo.LerAsync(anexoRestante);
        Assert.Equal("conteudo-ok", System.Text.Encoding.UTF8.GetString(conteudoAnexo));
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
    public void RestaurarBackupOrfaoSeNecessario_ComOsDoisBakEMarcadorDeConclusao_NaoRestauraSoLimpa()
    {
        // Simula o processo morrendo bem entre a troca já ter terminado com sucesso
        // (marcador escrito, arquivos reais já na chave nova) e a limpeza do finally
        // terminar de apagar os .bak. Sem o marcador, essa janela seria
        // indistinguível de uma interrupção genuína no meio da troca (que também
        // deixa os dois .bak presentes) — o teste
        // ComBackupsOrfaos_RestauraEApagaOsBak cobre esse outro caso, sem marcador.
        var auth = new AutenticacaoMestra(_pasta);
        auth.CriarSenhaMestra("SenhaNova@123");

        var authPath = Path.Combine(_pasta, "auth.dat");
        var vaultPath = Path.Combine(_pasta, "senhas.json.enc");
        var authBak = authPath + ".bak";
        var vaultBak = vaultPath + ".bak";
        var marcador = Path.Combine(_pasta, "troca_senha.ok");

        File.WriteAllText(vaultPath, "conteudo-ja-na-chave-nova");
        File.WriteAllText(authBak, "conteudo-da-chave-antiga-que-nao-deve-voltar");
        File.WriteAllText(vaultBak, "conteudo-da-chave-antiga-que-nao-deve-voltar");
        File.WriteAllText(marcador, "");

        new ServicoMudancaSenhaMestra(_pasta).RestaurarBackupOrfaoSeNecessario();

        Assert.False(File.Exists(authBak));
        Assert.False(File.Exists(vaultBak));
        Assert.False(File.Exists(marcador));
        Assert.Equal("conteudo-ja-na-chave-nova", File.ReadAllText(vaultPath));
        Assert.NotNull(new AutenticacaoMestra(_pasta).Autenticar("SenhaNova@123"));
    }

    [Fact]
    public void RestaurarBackupOrfaoSeNecessario_ComApenasUmDosDoisBakOrfao_NaoRestauraSoApagaOOrfao()
    {
        // Os dois .bak (auth.dat e cofre) são criados juntos e só apagados depois que
        // a troca inteira já terminou — se só um sobreviveu até aqui, é porque a
        // exclusão dele falhou depois de uma troca já bem-sucedida (ex.: antivírus
        // segurando o arquivo por um instante), não porque a troca foi interrompida no
        // meio. Restaurar esse .bak sozinho reverteria só metade do cofre pra chave
        // antiga, deixando auth.dat e o cofre em chaves diferentes — o certo é só
        // descartar o órfão sem tocar nos arquivos reais.
        var auth = new AutenticacaoMestra(_pasta);
        auth.CriarSenhaMestra("SenhaNova@123");

        var authPath = Path.Combine(_pasta, "auth.dat");
        var vaultPath = Path.Combine(_pasta, "senhas.json.enc");
        var authBak = authPath + ".bak";

        File.WriteAllText(vaultPath, "conteudo-ja-na-chave-nova");
        File.WriteAllText(authBak, "conteudo-da-chave-antiga-que-nao-deve-voltar");

        new ServicoMudancaSenhaMestra(_pasta).RestaurarBackupOrfaoSeNecessario();

        Assert.False(File.Exists(authBak));
        Assert.Equal("conteudo-ja-na-chave-nova", File.ReadAllText(vaultPath));
        Assert.NotNull(new AutenticacaoMestra(_pasta).Autenticar("SenhaNova@123"));
    }

    [Fact]
    public void RestaurarBackupOrfaoSeNecessario_ComBakDeAnexoOrfao_RestauraEApagaOBak()
    {
        // Simula o processo morrendo no meio do laço de regravação de anexos em
        // AlterarAsync: auth.dat/vault já têm .bak (a troca começou), e um dos
        // anexos já foi regravado com a chave nova, mas o .bak físico dele (o
        // conteúdo cifrado com a chave antiga) ainda não foi limpo.
        var auth = new AutenticacaoMestra(_pasta);
        auth.CriarSenhaMestra("SenhaOriginal@123");

        var authPath = Path.Combine(_pasta, "auth.dat");
        var vaultPath = Path.Combine(_pasta, "senhas.json.enc");
        File.WriteAllText(vaultPath, "estado-anterior-valido");
        File.Copy(authPath, authPath + ".bak");
        File.Copy(vaultPath, vaultPath + ".bak");

        var pastaAnexos = Path.Combine(_pasta, "anexos");
        Directory.CreateDirectory(pastaAnexos);
        var id = Guid.NewGuid();
        var caminhoAnexo = Path.Combine(pastaAnexos, id.ToString("N") + ".enc");
        var caminhoBak = caminhoAnexo + ".bak";
        File.WriteAllText(caminhoBak, "cifrado-com-a-chave-antiga");
        File.WriteAllText(caminhoAnexo, "cifrado-com-a-chave-nova");

        new ServicoMudancaSenhaMestra(_pasta).RestaurarBackupOrfaoSeNecessario();

        Assert.False(File.Exists(caminhoBak));
        Assert.Equal("cifrado-com-a-chave-antiga", File.ReadAllText(caminhoAnexo));
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

    [Fact]
    public async Task MigrarKdfSeNecessarioAsync_ComAnexoCorrompido_NaoFicaBloqueadaEDescartaOAnexo()
    {
        var chaveLegada = PrepararCofreLegado(_pasta, "SenhaAtual@123", 100_000,
            ("GitHub", "dev@git.com", "GitHub@Secreta1"));

        var cryptoLegado = new ServicoCriptografia(chaveLegada);
        var persistLegado = new PersistenciaLocal(cryptoLegado, _pasta);
        var senhas = await persistLegado.CarregarSenhasAsync(chaveLegada);
        var git = senhas.Single();

        var servicoAnexos = new ServicoAnexos(cryptoLegado, _pasta);
        var anexo = await servicoAnexos.AdicionarAsync(git, "ruim.txt", "sera-corrompido"u8.ToArray());
        await persistLegado.SalvarSenhasAsync(senhas, chaveLegada);

        var caminhoAnexo = Path.Combine(_pasta, "anexos", anexo.Id.ToString("N") + ".enc");
        File.WriteAllText(caminhoAnexo, "nao-e-um-anexo-cifrado-valido");

        // Antes da correção, uma falha ao decifrar o anexo derrubava AlterarAsync
        // inteiro com uma exceção crua, e como MigrarKdfSeNecessarioAsync é chamado
        // a cada login enquanto o KDF estiver desatualizado, a migração de segurança
        // ficava permanentemente travada nesse mesmo ponto, sem nenhum aviso.
        var servico = new ServicoMudancaSenhaMestra(_pasta);
        var novaChave = await servico.MigrarKdfSeNecessarioAsync("SenhaAtual@123");

        Assert.NotNull(novaChave);
        Assert.False(new AutenticacaoMestra(_pasta).KdfDesatualizado());
        Assert.NotEmpty(servico.UltimosAvisos);

        var cryptoNovo = new ServicoCriptografia(novaChave!);
        var persistNovo = new PersistenciaLocal(cryptoNovo, _pasta);
        var recarregada = Assert.Single(await persistNovo.CarregarSenhasAsync(novaChave!));
        Assert.Empty(recarregada.Anexos);
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
