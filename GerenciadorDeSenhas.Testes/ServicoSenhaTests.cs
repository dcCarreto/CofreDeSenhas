using System.Security.Cryptography;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoSenhaTests : IDisposable
{
    private readonly byte[] _chave;
    private readonly IServicoCriptografia _criptografia;
    private readonly IPersistenciaLocal _persistencia;
    private readonly IRepositorioSenha _repositorio;
    private readonly IServicoSenha _servico;
    private readonly string _pastaTemp;

    public ServicoSenhaTests()
    {
        _chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(_chave);

        _pastaTemp = PastaTemporariaTeste.Criar("GS_Servico");

        _criptografia = new ServicoCriptografia(_chave);
        _persistencia = new PersistenciaLocal(_criptografia, _pastaTemp);
        _repositorio = new RepositorioSenha(_persistencia, _chave);
        _servico = new ServicoSenha(_repositorio, _criptografia);
    }

    public void Dispose() => PastaTemporariaTeste.Apagar(_pastaTemp);

    private async Task<Senha?> ObterAsync(Guid id) =>
        (await _servico.ListarTodosAsync()).FirstOrDefault(s => s.Id == id);

    [Fact]
    public async Task CriarSenhaAsync_ComDadosValidos_CriaSenhaEncriptada()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456",
            Categoria.Personal, "https://gmail.com", "Meu email pessoal");

        Assert.NotNull(senha);
        Assert.NotEqual(Guid.Empty, senha.Id);
        Assert.Equal("Gmail", senha.NomeServico);
        Assert.Equal("user@gmail.com", senha.Usuario);
        Assert.NotEqual("Senha@123456", senha.SenhaHash);
        Assert.Equal(Categoria.Personal, senha.Categoria);
        Assert.False(senha.Favorito);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComDadosValidos_AtualizaSenha()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789",
            Categoria.Personal);

        var atualizada = await ObterAsync(senha.Id);

        Assert.NotNull(atualizada);
        Assert.Equal("novo@gmail.com", atualizada.Usuario);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComSenhaAnteriorCorrompida_NaoFalhaMasRegistraAviso()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.PersistirAsync();

        var senhas = await _persistencia.CarregarSenhasAsync(_chave);
        var item = senhas.Single(s => s.Id == senha.Id);
        item.SenhaHash = "@@@nao-e-base64@@@";
        await _persistencia.SalvarSenhasAsync(senhas, _chave);

        var repoFresco = new RepositorioSenha(_persistencia, _chave);
        var servicoConcreto = new ServicoSenha(repoFresco, _criptografia);

        await servicoConcreto.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789", Categoria.Personal);

        Assert.NotEmpty(servicoConcreto.UltimosAvisos);
        var atualizada = (await repoFresco.ListarTodosAsync()).Single(s => s.Id == senha.Id);
        Assert.Equal("novo@gmail.com", atualizada.Usuario);

        var outraSenha = await servicoConcreto.CriarSenhaAsync(
            "GitHub", "dev@git.com", "SenhaOriginal@1", Categoria.Work);
        await servicoConcreto.AtualizarSenhaAsync(
            outraSenha.Id, "GitHub", "dev@git.com", "SenhaNova@2", Categoria.Work);

        Assert.Empty(servicoConcreto.UltimosAvisos);
    }

    [Fact]
    public async Task RemoverSenhaAsync_ComIdExistente_RemoveSenha()
    {
        var senha = await _servico.CriarSenhaAsync(
            "GitHub", "dev@github.com", "GitHubSenha@123", Categoria.Work);

        await _servico.RemoverSenhaAsync(senha.Id);
        var removida = await ObterAsync(senha.Id);

        Assert.Null(removida);
    }

    [Fact]
    public async Task ListarTodosAsync_ComMultiplasSenhas_RetornaTodasAsSenhas()
    {
        await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.CriarSenhaAsync("GitHub", "dev@github.com", "GitHubSenha@123", Categoria.Work);
        await _servico.CriarSenhaAsync("AWS", "admin@aws.com", "AwsSenha@123", Categoria.Finance);

        var todas = await _servico.ListarTodosAsync();

        Assert.Equal(3, todas.Count);
    }

    [Fact]
    public async Task CriarSenhaAsync_ComEtiquetas_Normaliza()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Portal Cliente", "cliente@example.com", "Senha@123456",
            Categoria.Work, etiquetas: new[] { " Clientes ", "Projetos", "clientes", "" });

        Assert.Equal(new[] { "Clientes", "Projetos" }, senha.Etiquetas);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_SemEtiquetas_PreservaEtiquetasExistentes()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456",
            Categoria.Personal, etiquetas: new[] { "Pessoal", "Email" });

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789", Categoria.Personal);

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(new[] { "Pessoal", "Email" }, atualizada!.Etiquetas);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComListaVazia_RemoveEtiquetas()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456",
            Categoria.Personal, etiquetas: new[] { "Pessoal" });

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789",
            Categoria.Personal, etiquetas: Array.Empty<string>());

        var atualizada = await ObterAsync(senha.Id);

        Assert.Empty(atualizada!.Etiquetas);
    }

    [Fact]
    public async Task CriarSenhaAsync_SemTipo_UsaLoginPorPadraoSemCamposExtras()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        Assert.Equal(TipoCredencial.Login, senha.Tipo);
        Assert.Empty(senha.CamposExtras);
    }

    [Fact]
    public async Task CriarSenhaAsync_ComTipoECamposExtras_ArmazenaCamposCifrados()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Cartão Nubank", "titular", "4111111111111111", Categoria.Finance,
            tipo: TipoCredencial.Cartao,
            camposExtras: new Dictionary<string, string>
            {
                ["validade"] = "12/29",
                ["cvv"] = "123"
            });

        Assert.Equal(TipoCredencial.Cartao, senha.Tipo);
        Assert.Equal(2, senha.CamposExtras.Count);
        Assert.NotEqual("12/29", senha.CamposExtras["validade"]);
        Assert.NotEqual("123", senha.CamposExtras["cvv"]);
        Assert.Equal("12/29", _criptografia.Descriptografar(senha.CamposExtras["validade"]));
        Assert.Equal("123", _criptografia.Descriptografar(senha.CamposExtras["cvv"]));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComCampoExtraVazio_IgnoraCampo()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Wi-Fi Casa", "ssid", "senhaRede123", Categoria.Personal,
            tipo: TipoCredencial.WiFi,
            camposExtras: new Dictionary<string, string> { ["seguranca"] = "", ["banda"] = "5GHz" });

        Assert.Single(senha.CamposExtras);
        Assert.True(senha.CamposExtras.ContainsKey("banda"));
        Assert.False(senha.CamposExtras.ContainsKey("seguranca"));
    }

    [Fact]
    public async Task AtualizarSenhaAsync_SemTipo_PreservaTipoECamposExtrasExistentes()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Servidor Prod", "root", "SenhaServidor@1", Categoria.Work,
            tipo: TipoCredencial.Servidor,
            camposExtras: new Dictionary<string, string> { ["host"] = "10.0.0.1" });

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Servidor Prod", "root", "NovaSenhaServidor@2", Categoria.Work);

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(TipoCredencial.Servidor, atualizada!.Tipo);
        Assert.Equal("10.0.0.1", _criptografia.Descriptografar(atualizada.CamposExtras["host"]));
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComNovoTipoECamposExtras_Substitui()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Servidor Prod", "root", "SenhaServidor@1", Categoria.Work,
            tipo: TipoCredencial.Servidor,
            camposExtras: new Dictionary<string, string> { ["host"] = "10.0.0.1" });

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Servidor Prod", "root", "NovaSenhaServidor@2", Categoria.Work,
            tipo: TipoCredencial.BancoDados,
            camposExtras: new Dictionary<string, string> { ["motor"] = "PostgreSQL" });

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(TipoCredencial.BancoDados, atualizada!.Tipo);
        Assert.False(atualizada.CamposExtras.ContainsKey("host"));
        Assert.Equal("PostgreSQL", _criptografia.Descriptografar(atualizada.CamposExtras["motor"]));
    }

    [Fact]
    public async Task AplicarSincronizadoAsync_ComIdInexistente_CriaSenhaPreservandoId()
    {
        var id = Guid.NewGuid();
        var item = new SenhaExportada
        {
            Id = id,
            NomeServico = "Servico Sincronizado",
            Usuario = "usuario@teste.com",
            Senha = "SenhaSincronizada@123",
            Categoria = Categoria.Work,
            Tipo = TipoCredencial.Servidor,
            CamposExtras = new Dictionary<string, string> { ["host"] = "10.0.0.1" },
            DataCriacao = DateTime.UtcNow.AddDays(-1),
            DataAtualizacao = DateTime.UtcNow
        };

        await _servico.AplicarSincronizadoAsync(item);
        var salva = await ObterAsync(id);

        Assert.NotNull(salva);
        Assert.Equal("Servico Sincronizado", salva!.NomeServico);
        Assert.Equal(TipoCredencial.Servidor, salva.Tipo);
        Assert.Equal("10.0.0.1", _criptografia.Descriptografar(salva.CamposExtras["host"]));
        Assert.NotEqual("SenhaSincronizada@123", salva.SenhaHash);
        Assert.Equal("SenhaSincronizada@123", _criptografia.Descriptografar(salva.SenhaHash));
    }

    [Fact]
    public async Task AplicarSincronizadoAsync_ComIdExistente_AtualizaTodosOsCampos()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        var item = new SenhaExportada
        {
            Id = senha.Id,
            NomeServico = "Gmail Atualizado",
            Usuario = "novo@gmail.com",
            Senha = "NovaSenha@789",
            Categoria = Categoria.Work,
            Tipo = TipoCredencial.Login,
            Favorito = true,
            Fixado = true,
            DataCriacao = senha.DataCriacao,
            DataAtualizacao = DateTime.UtcNow.AddMinutes(5)
        };

        await _servico.AplicarSincronizadoAsync(item);
        var atualizada = await ObterAsync(senha.Id);

        Assert.NotNull(atualizada);
        Assert.Equal("Gmail Atualizado", atualizada!.NomeServico);
        Assert.Equal("novo@gmail.com", atualizada.Usuario);
        Assert.True(atualizada.Favorito);
        Assert.True(atualizada.Fixado);
        Assert.Equal(item.DataAtualizacao, atualizada.DataAtualizacao);
    }

    [Fact]
    public async Task AplicarSincronizadoAsync_ComNaLixeira_MarcaComoExcluidaLocalmente()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        var dataExclusao = DateTime.UtcNow;
        var item = new SenhaExportada
        {
            Id = senha.Id,
            NomeServico = senha.NomeServico,
            Usuario = senha.Usuario,
            Senha = "Senha@123456",
            Categoria = senha.Categoria,
            NaLixeira = true,
            DataExclusao = dataExclusao,
            DataCriacao = senha.DataCriacao,
            DataAtualizacao = DateTime.UtcNow.AddMinutes(1)
        };

        await _servico.AplicarSincronizadoAsync(item);

        var lixeira = await _servico.ListarLixeiraAsync();
        var itemLixeira = Assert.Single(lixeira);
        Assert.Equal(senha.Id, itemLixeira.Id);
        Assert.Equal(dataExclusao, itemLixeira.DataExclusao);
    }

    [Fact]
    public async Task AplicarSincronizadoAsync_ComTumbaDeExclusaoDefinitiva_RemovePorCompletoEmVezDeDeixarNaLixeira()
    {
        // Tumba publicada por outro dispositivo depois de uma exclusão definitiva
        // (JanelaPrincipal.PublicarTumbasNaPastaDeSincronizacaoAsync) — precisa sumir
        // por completo, não virar uma cópia em branco sentada na lixeira local.
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        var tumba = new SenhaExportada
        {
            Id = senha.Id,
            NomeServico = "",
            Usuario = "",
            Senha = "",
            NaLixeira = true,
            DataAtualizacao = DateTime.UtcNow.AddMinutes(1)
        };

        await _servico.AplicarSincronizadoAsync(tumba);

        Assert.Null(await ObterAsync(senha.Id));
        Assert.Empty(await _servico.ListarLixeiraAsync());
    }

    [Fact]
    public async Task AplicarSincronizadoAsync_ComTumbaParaIdQueNuncaExistiuLocalmente_NaoFazNada()
    {
        var tumba = new SenhaExportada
        {
            Id = Guid.NewGuid(),
            NomeServico = "",
            Usuario = "",
            Senha = "",
            NaLixeira = true,
            DataAtualizacao = DateTime.UtcNow
        };

        await _servico.AplicarSincronizadoAsync(tumba);

        Assert.Empty(await _servico.ListarTodosAsync());
        Assert.Empty(await _servico.ListarLixeiraAsync());
    }

    [Fact]
    public async Task MarcarComoFavoritoAsync_MarcaSenhaComoFavorita()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.MarcarComoFavoritoAsync(senha.Id);
        var atualizada = await ObterAsync(senha.Id);

        Assert.True(atualizada?.Favorito);
    }

    [Fact]
    public async Task RemoverDeFavoritoAsync_RemoveMarcacaoFavorita()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.MarcarComoFavoritoAsync(senha.Id);

        await _servico.RemoverDeFavoritoAsync(senha.Id);
        var atualizada = await ObterAsync(senha.Id);

        Assert.False(atualizada?.Favorito);
    }

    [Fact]
    public async Task MarcarComoFixadoAsync_FixaSenhaNoTopo()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.MarcarComoFixadoAsync(senha.Id);
        var atualizada = await ObterAsync(senha.Id);

        Assert.True(atualizada?.Fixado);
    }

    [Fact]
    public async Task RemoverFixacaoAsync_RemoveFixacao()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.MarcarComoFixadoAsync(senha.Id);

        await _servico.RemoverFixacaoAsync(senha.Id);
        var atualizada = await ObterAsync(senha.Id);

        Assert.False(atualizada?.Fixado);
    }

    [Fact]
    public async Task RegistrarCopiaAsync_GravaDataDoCampoSemAlterarDataAtualizacao()
    {
        var senha = await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        var dataAtualizacaoOriginal = senha.DataAtualizacao;

        await _servico.RegistrarCopiaAsync(senha.Id, TipoCampoCopiado.Senha);
        var apos = await ObterAsync(senha.Id);

        Assert.NotNull(apos?.DataUltimaCopiaSenha);
        Assert.Equal(dataAtualizacaoOriginal, apos!.DataAtualizacao);
    }

    [Fact]
    public async Task PersistirAsync_SalvaSenhasEmArquivoEncriptado()
    {
        await _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.PersistirAsync();

        var caminhoSenhas = Path.Combine(_pastaTemp, "senhas.json.enc");
        Assert.True(File.Exists(caminhoSenhas));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComNomeServicoVazio_LancaExcecao()
    {
        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.CriarSenhaAsync("", "user@example.com", "Senha@123456", Categoria.Personal));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComUsuarioVazio_LancaExcecao()
    {
        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.CriarSenhaAsync("Gmail", "", "Senha@123456", Categoria.Personal));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComSenhaVazia_LancaExcecao()
    {
        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.CriarSenhaAsync("Gmail", "user@example.com", "", Categoria.Personal));
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComIdInexistente_LancaExcecao()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servico.AtualizarSenhaAsync(Guid.NewGuid(), "Gmail", "user@example.com", "Senha@123456", Categoria.Personal));
    }

    [Fact]
    public async Task RemoverSenhaAsync_ComIdInexistente_LancaExcecao()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servico.RemoverSenhaAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoverSenhaAsync_MoveParaLixeiraEmVezDeApagar()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.RemoverSenhaAsync(senha.Id);

        Assert.Null(await ObterAsync(senha.Id));
        var lixeira = await _servico.ListarLixeiraAsync();
        Assert.Single(lixeira);
        Assert.Equal(senha.Id, lixeira[0].Id);
        Assert.NotNull(lixeira[0].DataExclusao);
    }

    [Fact]
    public async Task RestaurarSenhaAsync_TrazDeVoltaParaOCofre()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.RemoverSenhaAsync(senha.Id);

        await _servico.RestaurarSenhaAsync(senha.Id);

        Assert.NotNull(await ObterAsync(senha.Id));
        Assert.Empty(await _servico.ListarLixeiraAsync());
    }

    [Fact]
    public async Task RemoverDefinitivamenteAsync_ApagaMesmoDaLixeira()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.RemoverSenhaAsync(senha.Id);

        await _servico.RemoverDefinitivamenteAsync(senha.Id);

        Assert.Empty(await _servico.ListarLixeiraAsync());
    }

    [Fact]
    public async Task EsvaziarLixeiraAsync_LimpaTodosOsItensExcluidos()
    {
        var senha1 = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        var senha2 = await _servico.CriarSenhaAsync(
            "GitHub", "dev@github.com", "Senha@654321", Categoria.Work);
        await _servico.RemoverSenhaAsync(senha1.Id);
        await _servico.RemoverSenhaAsync(senha2.Id);

        await _servico.EsvaziarLixeiraAsync();

        Assert.Empty(await _servico.ListarLixeiraAsync());
    }

    [Fact]
    public async Task LimparCofreAsync_MoveTodasAsSenhasAtivasParaALixeira()
    {
        var senha1 = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        var senha2 = await _servico.CriarSenhaAsync(
            "GitHub", "dev@github.com", "Senha@654321", Categoria.Work);

        await _servico.LimparCofreAsync();

        Assert.Empty(await _servico.ListarTodosAsync());
        var lixeira = await _servico.ListarLixeiraAsync();
        Assert.Equal(2, lixeira.Count);
        Assert.Contains(lixeira, s => s.Id == senha1.Id && s.DataExclusao != null);
        Assert.Contains(lixeira, s => s.Id == senha2.Id && s.DataExclusao != null);
    }

    [Fact]
    public async Task LimparCofreAsync_NaoDuplicaNemAlteraItensJaNaLixeira()
    {
        var senha1 = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        var senha2 = await _servico.CriarSenhaAsync(
            "GitHub", "dev@github.com", "Senha@654321", Categoria.Work);
        await _servico.RemoverSenhaAsync(senha1.Id);
        var dataExclusaoOriginal = (await _servico.ListarLixeiraAsync()).Single().DataExclusao;

        await _servico.LimparCofreAsync();

        var lixeira = await _servico.ListarLixeiraAsync();
        Assert.Equal(2, lixeira.Count);
        Assert.Equal(dataExclusaoOriginal, lixeira.Single(s => s.Id == senha1.Id).DataExclusao);
    }

    [Fact]
    public async Task CriarSenhaAsync_ComTotp_ArmazenaSegredoCifradoNormalizado()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal,
            totpSegredo: "jbsw y3dp ehpk 3pxp");

        Assert.NotNull(senha.TotpSegredo);
        Assert.NotEqual("jbsw y3dp ehpk 3pxp", senha.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(senha.TotpSegredo!));
    }

    [Fact]
    public async Task CriarSenhaAsync_SemTotp_DeixaSegredoNulo()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        Assert.Null(senha.TotpSegredo);
    }

    [Fact]
    public async Task CriarSenhaAsync_ComTotpInvalido_LancaExcecao()
    {
        await Assert.ThrowsAsync<FormatException>(() =>
            _servico.CriarSenhaAsync("Gmail", "user@gmail.com", "Senha@123456",
                Categoria.Personal, totpSegredo: "###"));
    }

    [Fact]
    public async Task DefinirTotpAsync_DefineEDepoisRemove()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.DefinirTotpAsync(senha.Id, "JBSWY3DPEHPK3PXP");
        var comTotp = await ObterAsync(senha.Id);
        Assert.NotNull(comTotp!.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(comTotp.TotpSegredo!));

        await _servico.DefinirTotpAsync(senha.Id, "");
        var semTotp = await ObterAsync(senha.Id);
        Assert.Null(semTotp!.TotpSegredo);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_PreservaTotpExistente()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal,
            totpSegredo: "JBSWY3DPEHPK3PXP");

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "NovaSenha@789", Categoria.Personal);

        var atualizada = await ObterAsync(senha.Id);

        Assert.NotNull(atualizada!.TotpSegredo);
        Assert.Equal("JBSWY3DPEHPK3PXP", _criptografia.Descriptografar(atualizada.TotpSegredo!));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComecaSemHistorico()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        Assert.Empty(senha.Historico);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComSenhaDiferente_GuardaSenhaAnteriorNoHistorico()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "user@gmail.com", "NovaSenha@789", Categoria.Personal);

        var atualizada = await ObterAsync(senha.Id);

        var anterior = Assert.Single(atualizada!.Historico);
        Assert.Equal("Senha@123456", _criptografia.Descriptografar(anterior.SenhaHash));
        Assert.Equal("NovaSenha@789", _criptografia.Descriptografar(atualizada.SenhaHash));
    }

    [Fact]
    public async Task AtualizarSenhaAsync_ComMesmaSenha_NaoRegistraHistorico()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "novo@gmail.com", "Senha@123456", Categoria.Work);

        var atualizada = await ObterAsync(senha.Id);

        Assert.Empty(atualizada!.Historico);
        Assert.Equal("novo@gmail.com", atualizada.Usuario);
    }

    [Fact]
    public async Task AtualizarSenhaAsync_MultiplasTrocas_MantemOrdemCronologica()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Primeira@111", Categoria.Personal);

        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "user@gmail.com", "Segunda@222", Categoria.Personal);
        await _servico.AtualizarSenhaAsync(
            senha.Id, "Gmail", "user@gmail.com", "Terceira@333", Categoria.Personal);

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(2, atualizada!.Historico.Count);
        Assert.Equal("Primeira@111", _criptografia.Descriptografar(atualizada.Historico[0].SenhaHash));
        Assert.Equal("Segunda@222", _criptografia.Descriptografar(atualizada.Historico[1].SenhaHash));
    }

    [Fact]
    public async Task AtualizarSenhaAsync_AcimaDoLimite_MantemApenasAsMaisRecentes()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@000000", Categoria.Personal);

        for (int i = 1; i <= 15; i++)
        {
            await _servico.AtualizarSenhaAsync(
                senha.Id, "Gmail", "user@gmail.com", $"Senha@{i:000000}", Categoria.Personal);
        }

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(10, atualizada!.Historico.Count);
        Assert.Equal("Senha@000005", _criptografia.Descriptografar(atualizada.Historico[0].SenhaHash));
        Assert.Equal("Senha@000014", _criptografia.Descriptografar(atualizada.Historico[9].SenhaHash));
    }

    [Fact]
    public async Task CriarSenhaAsync_ComecaSemCodigosRecuperacao()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        Assert.Empty(senha.CodigosRecuperacao);
    }

    [Fact]
    public async Task AdicionarCodigosRecuperacaoAsync_ArmazenaCodigosCifrados()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AdicionarCodigosRecuperacaoAsync(senha.Id,
            new[] { ("ABCD-1234", false), ("EFGH-5678", false) });

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(2, atualizada!.CodigosRecuperacao.Count);
        Assert.Equal("ABCD-1234", _criptografia.Descriptografar(atualizada.CodigosRecuperacao[0].Codigo));
        Assert.Equal("EFGH-5678", _criptografia.Descriptografar(atualizada.CodigosRecuperacao[1].Codigo));
        Assert.All(atualizada.CodigosRecuperacao, c => Assert.False(c.Usado));
    }

    [Fact]
    public async Task AdicionarCodigosRecuperacaoAsync_IgnoraLinhasVazias()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AdicionarCodigosRecuperacaoAsync(senha.Id,
            new[] { ("ABCD-1234", false), ("", false), ("   ", false) });

        var atualizada = await ObterAsync(senha.Id);

        Assert.Single(atualizada!.CodigosRecuperacao);
    }

    [Fact]
    public async Task AdicionarCodigosRecuperacaoAsync_ChamadasSucessivas_AcumulaSemPerderExistentes()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await _servico.AdicionarCodigosRecuperacaoAsync(senha.Id, new[] { ("ABCD-1234", false) });
        await _servico.AdicionarCodigosRecuperacaoAsync(senha.Id, new[] { ("EFGH-5678", false) });

        var atualizada = await ObterAsync(senha.Id);

        Assert.Equal(2, atualizada!.CodigosRecuperacao.Count);
    }

    [Fact]
    public async Task AdicionarCodigosRecuperacaoAsync_ComCodigoExcessivamenteLongo_LancaExcecao()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.AdicionarCodigosRecuperacaoAsync(senha.Id, new[] { (new string('A', 501), false) }));
    }

    [Fact]
    public async Task AdicionarCodigosRecuperacaoAsync_AcimaDoLimiteTotal_LancaExcecao()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        var muitos = Enumerable.Range(0, 101).Select(i => ($"codigo-{i}", false));

        await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            _servico.AdicionarCodigosRecuperacaoAsync(senha.Id, muitos));
    }

    [Fact]
    public async Task MarcarCodigoRecuperacaoAsync_AlternaEstadoUsado()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.AdicionarCodigosRecuperacaoAsync(senha.Id, new[] { ("ABCD-1234", false) });
        var codigoId = (await ObterAsync(senha.Id))!.CodigosRecuperacao[0].Id;

        await _servico.MarcarCodigoRecuperacaoAsync(senha.Id, codigoId, true);
        Assert.True((await ObterAsync(senha.Id))!.CodigosRecuperacao[0].Usado);

        await _servico.MarcarCodigoRecuperacaoAsync(senha.Id, codigoId, false);
        Assert.False((await ObterAsync(senha.Id))!.CodigosRecuperacao[0].Usado);
    }

    [Fact]
    public async Task MarcarCodigoRecuperacaoAsync_ComIdInexistente_LancaExcecao()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servico.MarcarCodigoRecuperacaoAsync(senha.Id, Guid.NewGuid(), true));
    }

    [Fact]
    public async Task RemoverCodigoRecuperacaoAsync_RemoveApenasOCodigoIndicado()
    {
        var senha = await _servico.CriarSenhaAsync(
            "Gmail", "user@gmail.com", "Senha@123456", Categoria.Personal);
        await _servico.AdicionarCodigosRecuperacaoAsync(senha.Id,
            new[] { ("ABCD-1234", false), ("EFGH-5678", false) });
        var codigos = (await ObterAsync(senha.Id))!.CodigosRecuperacao;
        var idParaRemover = codigos[0].Id;

        await _servico.RemoverCodigoRecuperacaoAsync(senha.Id, idParaRemover);

        var restante = Assert.Single((await ObterAsync(senha.Id))!.CodigosRecuperacao);
        Assert.Equal("EFGH-5678", _criptografia.Descriptografar(restante.Codigo));
    }
}
