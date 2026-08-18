using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class RepositorioSenhaEspelhadoTests : IDisposable
{
    private readonly string _arquivo;
    private readonly ConexaoBanco _cfg;
    private readonly ServicoBancoDados _bd = new();
    private readonly byte[] _chave;
    private readonly IServicoCriptografia _cripto;

    public RepositorioSenhaEspelhadoTests()
    {
        _arquivo = Path.Combine(Path.GetTempPath(), "GS_Espelho_" + Guid.NewGuid().ToString("N") + ".db");
        _cfg = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = _arquivo };

        _chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(_chave);
        _cripto = new ServicoCriptografia(_chave);

        _bd.CriarTabelaAsync(_cfg).GetAwaiter().GetResult();
        _bd.GarantirColunasAsync(_cfg).GetAwaiter().GetResult();
    }

    private RepositorioSenha NovoLocal() => new(new PersistenciaEmMemoria(), _chave);
    private RepositorioSenhaBanco NovoBanco() => new(_cfg, _cripto);

    private Senha Nova(string dominio, string usuario, string plaintext) => new()
    {
        NomeServico = dominio,
        Usuario = usuario,
        SenhaHash = _cripto.Criptografar(plaintext),
        Categoria = Categoria.Other
    };

    private Senha NovaComId(Guid id, string dominio, string usuario, string plaintext, DateTime dataAtualizacao) => new()
    {
        Id = id,
        NomeServico = dominio,
        Usuario = usuario,
        SenhaHash = _cripto.Criptografar(plaintext),
        Categoria = Categoria.Other,
        DataAtualizacao = dataAtualizacao
    };

    // Delega tudo pra um RepositorioSenha de verdade, exceto ListarTudoAsync, que
    // lança na primeira chamada — simula uma falha transitória (rede/disco) bem no
    // meio da primeira mesclagem da sessão.
    private sealed class LocalComFalhaNaPrimeiraChamada : IRepositorioSenha
    {
        private readonly RepositorioSenha _interno;
        private int _chamadas;

        public LocalComFalhaNaPrimeiraChamada(RepositorioSenha interno) => _interno = interno;

        public Task AdicionarAsync(Senha senha) => _interno.AdicionarAsync(senha);
        public Task AtualizarAsync(Senha senha) => _interno.AtualizarAsync(senha);
        public Task RegistrarCopiaAsync(Guid id, TipoCampoCopiado campo) => _interno.RegistrarCopiaAsync(id, campo);
        public Task RemoverAsync(Guid id) => _interno.RemoverAsync(id);
        public Task MoverTudoParaLixeiraAsync() => _interno.MoverTudoParaLixeiraAsync();
        public Task<Senha?> ObterPorIdAsync(Guid id) => _interno.ObterPorIdAsync(id);
        public Task<List<Senha>> ListarTodosAsync() => _interno.ListarTodosAsync();
        public Task<List<Senha>> ListarLixeiraAsync() => _interno.ListarLixeiraAsync();

        public async Task<List<Senha>> ListarTudoAsync()
        {
            if (++_chamadas == 1)
                throw new InvalidOperationException("falha simulada de rede/disco");
            return await _interno.ListarTudoAsync();
        }

        public Task RestaurarAsync(Guid id) => _interno.RestaurarAsync(id);
        public Task RemoverDefinitivamenteAsync(Guid id) => _interno.RemoverDefinitivamenteAsync(id);
        public Task EsvaziarLixeiraAsync() => _interno.EsvaziarLixeiraAsync();
        public Task SalvarAsync() => _interno.SalvarAsync();
    }

    [Fact]
    public async Task Mesclar_ComFalhaTransitoriaNaPrimeiraTentativa_NaoTravaPermanentementeEASegundaChamadaFunciona()
    {
        // Antes da correção, SincronizarAsync() cacheava a Task falhada em
        // _sincronizacao pra sempre (só "??="), então TODO método público desta
        // classe (todos começam chamando SincronizarAsync()) relançava a mesma
        // exceção antiga indefinidamente — o cofre inteiro ficava inutilizável até
        // reiniciar o app, mesmo pra uma falha transitória de rede.
        var local = new LocalComFalhaNaPrimeiraChamada(NovoLocal());
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco(), reconciliacaoJaRealizada: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => espelho.ListarTodosAsync());

        var resultado = await espelho.ListarTodosAsync();
        Assert.NotNull(resultado);
    }

    [Fact]
    public async Task Mesclar_UneOsDoisLadosPorGuid_ItensSemConflitoTodosSobrevivem()
    {
        var local = NovoLocal();
        await local.AdicionarAsync(Nova("gmail", "u1", "local1"));

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("spotify", "u3", "s3"));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        Assert.Equal(2, todas.Count);
        Assert.Contains(todas, s => s.NomeServico == "spotify");

        var noBanco = await NovoBanco().ListarTodosAsync();
        Assert.Contains(noBanco, s => s.NomeServico == "gmail");
    }

    [Fact]
    public async Task Mesclar_ComAnexoLocalEBancoMaisRecente_PreservaOAnexoLocal()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = NovoLocal();
        var comAnexo = NovaComId(id, "gmail", "u1", "senha-local", agora);
        comAnexo.Anexos.Add(new AnexoSenha { NomeArquivo = "recibo.pdf", TamanhoBytes = 1024 });
        await local.AdicionarAsync(comAnexo);

        var banco = NovoBanco();
        // O banco "vence" por ter DataAtualizacao mais recente, mas nunca teve
        // coluna de anexos — sem a correção, isso apagaria o anexo local.
        await banco.AdicionarAsync(NovaComId(id, "gmail", "u1", "senha-do-banco", agora.AddMinutes(1)));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var mesclada = (await espelho.ListarTodosAsync()).Single();

        Assert.Single(mesclada.Anexos);
        Assert.Equal("recibo.pdf", mesclada.Anexos[0].NomeArquivo);
    }

    [Fact]
    public async Task Mesclar_MesmoGuidNosDoisLados_EdicaoMaisRecenteVence()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = NovoLocal();
        await local.AdicionarAsync(NovaComId(id, "github", "u2", "antiga", agora.AddMinutes(-10)));

        var banco = NovoBanco();
        await banco.AdicionarAsync(NovaComId(id, "github", "u2", "nova", agora));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        var item = Assert.Single(todas);
        Assert.Equal("nova", _cripto.Descriptografar(item.SenhaHash));
    }

    [Fact]
    public async Task Mesclar_GuidsDiferentesComMesmaChave_SobrevivemComoItensDistintos()
    {
        var local = NovoLocal();
        await local.AdicionarAsync(Nova("github", "u2", "doLocal"));

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("github", "u2", "doBanco"));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        Assert.Equal(2, todas.Count(s => s.NomeServico == "github"));
        Assert.Equal(2, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
    }

    [Fact]
    public async Task Mesclar_ComReconciliacaoLegadaPendente_AdotaGuidLocalENaoDuplica()
    {
        var local = NovoLocal();
        var itemLocal = Nova("github", "u2", "conteudo");
        await local.AdicionarAsync(itemLocal);

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("github", "u2", "conteudo"));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: false);
        var todas = await espelho.ListarTodosAsync();

        var item = Assert.Single(todas);
        Assert.Equal(itemLocal.Id, item.Id);
        Assert.True(espelho.ReconciliacaoRealizadaNestaSessao);

        var noBanco = await NovoBanco().ListarTodosAsync();
        var itemBanco = Assert.Single(noBanco);
        Assert.Equal(itemLocal.Id, itemBanco.Id);
    }

    [Fact]
    public async Task Mesclar_ComReconciliacaoLegadaEColisaoDeSenhasDiferentes_PreservaASenhaPerdedoraNoHistorico()
    {
        // Duas credenciais genuinamente diferentes (dispositivos ainda não
        // sincronizados) que coincidem em nome de serviço + usuário: a reconciliação
        // legada as unifica mesmo assim (aposta deliberada), mas a senha do lado que
        // perder a mesclagem por "mais recente vence" não pode desaparecer sem deixar
        // rastro nenhum.
        var local = NovoLocal();
        var itemLocal = Nova("github", "u2", "senha-local");
        itemLocal.DataAtualizacao = DateTime.UtcNow;
        await local.AdicionarAsync(itemLocal);

        var banco = NovoBanco();
        var itemBanco = Nova("github", "u2", "senha-banco");
        itemBanco.DataAtualizacao = DateTime.UtcNow.AddMinutes(-10);
        await banco.AdicionarAsync(itemBanco);

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: false);
        var todas = await espelho.ListarTodosAsync();

        var item = Assert.Single(todas);
        Assert.Equal("senha-local", _cripto.Descriptografar(item.SenhaHash));

        // Ambos os lados são guardados no histórico (não dá pra saber de antemão quem
        // vai vencer a mesclagem por data) — o que importa é que a senha do lado que
        // perdeu ("senha-banco") sobrevive em algum lugar recuperável.
        var senhasNoHistorico = item.Historico.Select(h => _cripto.Descriptografar(h.SenhaHash)).ToList();
        Assert.Contains("senha-banco", senhasNoHistorico);
    }

    [Fact]
    public async Task Mesclar_ComReconciliacaoJaFeita_NaoReconciliaMaisApesarDeChaveCoincidente()
    {
        var local = NovoLocal();
        await local.AdicionarAsync(Nova("github", "u2", "conteudo1"));

        var banco = NovoBanco();
        await banco.AdicionarAsync(Nova("github", "u2", "conteudo1"));

        var primeiro = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: false);
        await primeiro.ListarTodosAsync();

        await local.AdicionarAsync(Nova("gitlab", "u9", "conteudo2"));
        await NovoBanco().AdicionarAsync(Nova("gitlab", "u9", "conteudo2"));

        var segundo = new RepositorioSenhaEspelhado(local, NovoBanco(), reconciliacaoJaRealizada: true);
        var todas = await segundo.ListarTodosAsync();

        Assert.Equal(2, todas.Count(s => s.NomeServico == "gitlab"));
    }

    [Fact]
    public async Task Adicionar_GravaNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());

        await espelho.AdicionarAsync(Nova("netflix", "u4", "n4"));

        Assert.Single(await local.ListarTodosAsync());
        Assert.Contains(await NovoBanco().ListarTodosAsync(), s => s.NomeServico == "netflix");
    }

    [Fact]
    public async Task RegistrarCopia_PropagaParaOBancoTambem()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var senha = Nova("app", "u", "s");
        await espelho.AdicionarAsync(senha);

        await espelho.RegistrarCopiaAsync(senha.Id, TipoCampoCopiado.Senha);

        var noBanco = (await NovoBanco().ListarTodosAsync()).Single();
        Assert.NotNull(noBanco.DataUltimaCopiaSenha);
    }

    [Fact]
    public async Task Remover_ExcluiNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var senha = Nova("app", "u", "s");
        await espelho.AdicionarAsync(senha);

        await espelho.RemoverAsync(senha.Id);

        Assert.Empty(await local.ListarTodosAsync());
        Assert.Empty(await NovoBanco().ListarTodosAsync());
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE excluido = 1"));
    }

    [Fact]
    public async Task Remover_ComDoisEspelhosNoMesmoBanco_ExclusaoNaoEhRevertida()
    {
        var id = Guid.NewGuid();
        var criada = DateTime.UtcNow.AddMinutes(-10);

        var localA = NovoLocal();
        var espelhoA = new RepositorioSenhaEspelhado(localA, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoA.AdicionarAsync(NovaComId(id, "app", "u", "s", criada));

        var localB = NovoLocal();
        var espelhoB1 = new RepositorioSenhaEspelhado(localB, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoB1.ListarTodosAsync();
        Assert.Single(await localB.ListarTodosAsync());

        await espelhoA.RemoverAsync(id);

        var espelhoB2 = new RepositorioSenhaEspelhado(localB, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoB2.ListarTodosAsync();

        Assert.Empty(await localB.ListarTodosAsync());
        Assert.Empty(await NovoBanco().ListarTodosAsync());
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE excluido = 1"));
    }

    [Fact]
    public async Task RemoverDefinitivamente_ComDoisEspelhosNoMesmoBanco_NaoRessuscitaNoOutroDispositivo()
    {
        var id = Guid.NewGuid();
        var criada = DateTime.UtcNow.AddMinutes(-10);
        var item = NovaComId(id, "app", "u", "s", criada);
        // Etiquetas/histórico exercitam a mesclagem aditiva: sem o corte específico
        // pra tumba, esses dados do lado local (device B) seriam resgatados de volta
        // por cima da tumba, incluindo senhas antigas guardadas no histórico.
        item.Etiquetas.Add("trabalho");
        item.Historico.Add(new HistoricoSenha { SenhaHash = _cripto.Criptografar("senha-antiga"), DataAlteracao = criada });

        var localA = NovoLocal();
        var espelhoA = new RepositorioSenhaEspelhado(localA, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoA.AdicionarAsync(item);

        // Device B sincroniza antes da exclusão definitiva de A: fica com a própria
        // cópia local do item, exatamente o cenário que arriscava ressuscitá-lo.
        var localB = NovoLocal();
        var espelhoB1 = new RepositorioSenhaEspelhado(localB, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoB1.ListarTodosAsync();
        Assert.Single(await localB.ListarTodosAsync());

        await espelhoA.RemoverDefinitivamenteAsync(id);

        var espelhoB2 = new RepositorioSenhaEspelhado(localB, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelhoB2.ListarTodosAsync();

        Assert.Empty(await localB.ListarTodosAsync());
        Assert.Empty(await localB.ListarLixeiraAsync());
        Assert.Empty(await NovoBanco().ListarTodosAsync());

        // A linha crua ainda existe no banco (é a tumba, não um DELETE) mas totalmente
        // esvaziada — nenhum rastro de etiqueta, histórico ou senha sobra nela.
        var tumba = Assert.Single(await NovoBanco().ListarLixeiraAsync());
        Assert.Equal("", tumba.NomeServico);
        Assert.Empty(tumba.Etiquetas);
        Assert.Empty(tumba.Historico);
    }

    [Fact]
    public async Task Restaurar_TrazDeVoltaNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var senha = Nova("app", "u", "s");
        await espelho.AdicionarAsync(senha);
        await espelho.RemoverAsync(senha.Id);

        await espelho.RestaurarAsync(senha.Id);

        Assert.Single(await local.ListarTodosAsync());
        Assert.Contains(await NovoBanco().ListarTodosAsync(), s => s.NomeServico == "app");
        Assert.Equal(0, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE excluido = 1"));
    }

    [Fact]
    public async Task RemoverDefinitivamente_ApagaNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var senha = Nova("app", "u", "s");
        await espelho.AdicionarAsync(senha);
        await espelho.RemoverAsync(senha.Id);

        await espelho.RemoverDefinitivamenteAsync(senha.Id);

        Assert.Empty(await local.ListarLixeiraAsync());
        // A linha crua ainda existe (tumba, não DELETE), mas esvaziada.
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE dominio = '' AND excluido = 1"));
    }

    [Fact]
    public async Task EsvaziarLixeira_ApagaNosDoisLados()
    {
        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco());
        var ativa = Nova("ativa", "u", "s");
        var excluida = Nova("excluida", "u", "s");
        await espelho.AdicionarAsync(ativa);
        await espelho.AdicionarAsync(excluida);
        await espelho.RemoverAsync(excluida.Id);

        await espelho.EsvaziarLixeiraAsync();

        Assert.Empty(await local.ListarLixeiraAsync());
        Assert.Equal(2, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas"));
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE dominio = 'ativa'"));
        Assert.Equal(1, await ContarLinhas("SELECT COUNT(*) FROM CofreDeSenhas WHERE dominio = '' AND excluido = 1"));
    }

    [Fact]
    public async Task Mesclar_EtiquetasDivergentesNosDoisLados_UneEmVezDeDescartarAsDoLado()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var itemLocal = NovaComId(id, "servico", "u", "conteudo", agora.AddMinutes(-10));
        itemLocal.Etiquetas.Add("etiqueta-local");
        var local = NovoLocal();
        await local.AdicionarAsync(itemLocal);

        var itemBanco = NovaComId(id, "servico", "u", "conteudo-novo", agora);
        itemBanco.Etiquetas.Add("etiqueta-remota");
        var banco = NovoBanco();
        await banco.AdicionarAsync(itemBanco);

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        var item = Assert.Single(todas);
        Assert.Contains("etiqueta-local", item.Etiquetas);
        Assert.Contains("etiqueta-remota", item.Etiquetas);
    }

    [Fact]
    public async Task Mesclar_HistoricoDivergenteNosDoisLados_UneAsEntradas()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var itemLocal = NovaComId(id, "servico", "u", "conteudo", agora.AddMinutes(-10));
        itemLocal.Historico.Add(new HistoricoSenha { SenhaHash = "hash-local", DataAlteracao = agora.AddDays(-5) });
        var local = NovoLocal();
        await local.AdicionarAsync(itemLocal);

        var itemBanco = NovaComId(id, "servico", "u", "conteudo-novo", agora);
        itemBanco.Historico.Add(new HistoricoSenha { SenhaHash = "hash-remoto", DataAlteracao = agora.AddDays(-2) });
        var banco = NovoBanco();
        await banco.AdicionarAsync(itemBanco);

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        var item = Assert.Single(todas);
        Assert.Contains(item.Historico, h => h.SenhaHash == "hash-local");
        Assert.Contains(item.Historico, h => h.SenhaHash == "hash-remoto");
    }

    [Fact]
    public async Task Mesclar_CodigosRecuperacaoDivergentesNosDoisLados_UneEmVezDeDescartar()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var itemLocal = NovaComId(id, "servico", "u", "conteudo", agora.AddMinutes(-10));
        itemLocal.CodigosRecuperacao.Add(new CodigoRecuperacao { Codigo = "codigo-local" });
        var local = NovoLocal();
        await local.AdicionarAsync(itemLocal);

        var itemBanco = NovaComId(id, "servico", "u", "conteudo-novo", agora);
        itemBanco.CodigosRecuperacao.Add(new CodigoRecuperacao { Codigo = "codigo-remoto" });
        var banco = NovoBanco();
        await banco.AdicionarAsync(itemBanco);

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        var item = Assert.Single(todas);
        Assert.Contains(item.CodigosRecuperacao, c => c.Codigo == "codigo-local");
        Assert.Contains(item.CodigosRecuperacao, c => c.Codigo == "codigo-remoto");
    }

    [Fact]
    public async Task Mesclar_ComEdicaoConcorrente_RegistraConflitoEmUltimosConflitos()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = NovoLocal();
        await local.AdicionarAsync(NovaComId(id, "servico", "u", "antiga", agora.AddMinutes(-10)));

        var banco = NovoBanco();
        await banco.AdicionarAsync(NovaComId(id, "servico", "u", "nova", agora));

        var espelho = new RepositorioSenhaEspelhado(local, banco, reconciliacaoJaRealizada: true);
        await espelho.ListarTodosAsync();

        var conflito = Assert.Single(espelho.UltimosConflitos);
        Assert.Equal(id, conflito.SenhaId);
        Assert.Equal(TipoConflitoSincronizacao.EdicaoConcorrente, conflito.Tipo);
    }

    [Fact]
    public async Task Mesclar_ComLinhaAdulteradaNoBanco_RegistraViolacaoDeIntegridadeERejeitaODado()
    {
        var id = Guid.NewGuid();
        var senha = NovaComId(id, "confiavel.com", "u", "conteudo", DateTime.UtcNow);

        var banco = NovoBanco();
        await banco.AdicionarAsync(senha);

        await using (var con = _bd.CriarConexao(_cfg))
        {
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE CofreDeSenhas SET dominio = @valor";
            var p = cmd.CreateParameter();
            p.ParameterName = "@valor";
            p.Value = "adulterado-por-outro-cliente.com";
            cmd.Parameters.Add(p);
            await cmd.ExecuteNonQueryAsync();
        }

        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco(), reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        Assert.Empty(todas);
        var conflito = Assert.Single(espelho.UltimosConflitos);
        Assert.Equal(id, conflito.SenhaId);
        Assert.Equal(TipoConflitoSincronizacao.IntegridadeViolada, conflito.Tipo);
    }

    [Fact]
    public async Task Mesclar_ComLinhaAdulteradaNoBancoQueTambemExisteLocalmente_NaoSobrescreveALinhaAdulterada()
    {
        var id = Guid.NewGuid();
        var senhaOriginal = NovaComId(id, "confiavel.com", "u", "conteudo", DateTime.UtcNow);

        var banco = NovoBanco();
        await banco.AdicionarAsync(senhaOriginal);

        await using (var con = _bd.CriarConexao(_cfg))
        {
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE CofreDeSenhas SET dominio = @valor";
            var p = cmd.CreateParameter();
            p.ParameterName = "@valor";
            p.Value = "adulterado-por-outro-cliente.com";
            cmd.Parameters.Add(p);
            await cmd.ExecuteNonQueryAsync();
        }

        // Este dispositivo tem o MESMO guid localmente, com conteúdo legítimo — antes
        // da correção, a publicação automática no fim de MesclarAsync sobrescrevia a
        // linha adulterada do banco com este conteúdo local e um hmac novo e válido,
        // apagando o rastro da adulteração antes de qualquer tela mostrar o conflito.
        var local = NovoLocal();
        await local.AdicionarAsync(NovaComId(id, "confiavel.com", "u", "conteudo", DateTime.UtcNow));

        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco(), reconciliacaoJaRealizada: true);
        await espelho.ListarTodosAsync();

        var repoBancoDireto = NovoBanco();
        await repoBancoDireto.ListarTodosAsync();
        Assert.Contains(repoBancoDireto.ViolacoesIntegridade, v => v.Id == id);
    }

    [Fact]
    public async Task Mesclar_ComHmacApagadoNoBanco_RegistraConflitoDeIntegridadeAusenteSemDescartarODado()
    {
        var id = Guid.NewGuid();
        var senha = NovaComId(id, "sem-hmac.com", "u", "conteudo", DateTime.UtcNow);

        var banco = NovoBanco();
        await banco.AdicionarAsync(senha);

        await using (var con = _bd.CriarConexao(_cfg))
        {
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE CofreDeSenhas SET hmac = NULL";
            await cmd.ExecuteNonQueryAsync();
        }

        var local = NovoLocal();
        var espelho = new RepositorioSenhaEspelhado(local, NovoBanco(), reconciliacaoJaRealizada: true);
        var todas = await espelho.ListarTodosAsync();

        Assert.Single(todas);
        var conflito = Assert.Single(espelho.UltimosConflitos);
        Assert.Equal(id, conflito.SenhaId);
        Assert.Equal(TipoConflitoSincronizacao.IntegridadeAusente, conflito.Tipo);
    }

    private async Task<long> ContarLinhas(string sql)
    {
        await using var con = _bd.CriarConexao(_cfg);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    public void Dispose()
    {
        // ClearPool escopado à connection string deste arquivo — ClearAllPools
        // derrubaria conexões de outras classes de teste rodando em paralelo.
        using (var con = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _arquivo }.ConnectionString))
            SqliteConnection.ClearPool(con);
        try { if (File.Exists(_arquivo)) File.Delete(_arquivo); } catch { }
    }
}
