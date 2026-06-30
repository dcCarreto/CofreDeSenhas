using System.Text.Json;
using CofreDeSenhas.Nucleo;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ProcessadorTests : IDisposable
{
    private const string SenhaMestra = "SenhaMestra@123";
    private readonly string _pasta;

    public ProcessadorTests()
    {
        _pasta = Path.Combine(Path.GetTempPath(), "GS_Proc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true); } catch { }
    }

    private async Task<Guid> SemearCofreAsync()
    {
        var auth = new AutenticacaoMestra(_pasta);
        var chave = auth.CriarSenhaMestra(SenhaMestra);
        var cripto = new ServicoCriptografia(chave);
        var persist = new PersistenciaLocal(cripto, _pasta);
        var repo = new RepositorioSenha(persist, chave);
        var servico = new ServicoSenha(repo, cripto);
        var senha = await servico.CriarSenhaAsync(
            "GitHub", "dev@git.com", "GitHub@Secreta123", Categoria.Work, "https://github.com");
        await servico.PersistirAsync();
        return senha.Id;
    }

    private (Processador proc, SessaoCofre sessao) Montar(bool unlockAutomatico = true)
    {
        var sessao = new SessaoCofre(_pasta);
        var proc = new Processador(sessao,
            () => unlockAutomatico && sessao.DestrancarAsync(SenhaMestra).GetAwaiter().GetResult());
        return (proc, sessao);
    }

    private static JsonElement Json(string texto) => JsonDocument.Parse(texto).RootElement;

    [Fact]
    public async Task Status_Bloqueado_RetornaDestrancadoFalse()
    {
        await SemearCofreAsync();
        var (proc, sessao) = Montar();
        using (sessao)
        {
            var r = Json(proc.Processar("""{"tipo":"status"}"""));
            Assert.True(r.GetProperty("ok").GetBoolean());
            Assert.False(r.GetProperty("destrancado").GetBoolean());
            Assert.Equal(0, r.GetProperty("total").GetInt32());
        }
    }

    [Fact]
    public async Task Unlock_ComCallbackDeSucesso_DestrancaECarrega()
    {
        await SemearCofreAsync();
        var (proc, sessao) = Montar();
        using (sessao)
        {
            var r = Json(proc.Processar("""{"tipo":"unlock"}"""));
            Assert.Equal("unlocked", r.GetProperty("status").GetString());

            var st = Json(proc.Processar("""{"tipo":"status"}"""));
            Assert.True(st.GetProperty("destrancado").GetBoolean());
            Assert.Equal(1, st.GetProperty("total").GetInt32());
        }
    }

    [Fact]
    public async Task Unlock_ComCallbackCancelado_RetornaCancelled()
    {
        await SemearCofreAsync();
        var (proc, sessao) = Montar(unlockAutomatico: false);
        using (sessao)
        {
            var r = Json(proc.Processar("""{"tipo":"unlock"}"""));
            Assert.Equal("cancelled", r.GetProperty("status").GetString());
            Assert.False(sessao.Destrancado);
        }
    }

    [Fact]
    public async Task Query_DominioCasado_RetornaItemSemSenha()
    {
        await SemearCofreAsync();
        var (proc, sessao) = Montar();
        using (sessao)
        {
            proc.Processar("""{"tipo":"unlock"}""");
            var r = Json(proc.Processar("""{"tipo":"query","dominio":"github.com"}"""));

            var itens = r.GetProperty("itens");
            Assert.Equal(1, itens.GetArrayLength());
            Assert.Equal("GitHub", itens[0].GetProperty("servico").GetString());
            Assert.Equal("dev@git.com", itens[0].GetProperty("usuario").GetString());
            Assert.False(itens[0].TryGetProperty("senha", out _));
        }
    }

    [Fact]
    public async Task Query_DominioSemCorrespondencia_RetornaVazio()
    {
        await SemearCofreAsync();
        var (proc, sessao) = Montar();
        using (sessao)
        {
            proc.Processar("""{"tipo":"unlock"}""");
            var r = Json(proc.Processar("""{"tipo":"query","dominio":"gitlab.com"}"""));
            Assert.Equal(0, r.GetProperty("itens").GetArrayLength());
        }
    }

    [Fact]
    public async Task Query_Bloqueado_RetornaErro()
    {
        await SemearCofreAsync();
        var (proc, sessao) = Montar();
        using (sessao)
        {
            var r = Json(proc.Processar("""{"tipo":"query","dominio":"github.com"}"""));
            Assert.False(r.GetProperty("ok").GetBoolean());
            Assert.Equal("bloqueado", r.GetProperty("erro").GetString());
        }
    }

    [Fact]
    public async Task GetCredential_RetornaSenhaEmClaro()
    {
        var id = await SemearCofreAsync();
        var (proc, sessao) = Montar();
        using (sessao)
        {
            proc.Processar("""{"tipo":"unlock"}""");
            var r = Json(proc.Processar($$"""{"tipo":"getCredential","id":"{{id}}"}"""));
            Assert.Equal("dev@git.com", r.GetProperty("usuario").GetString());
            Assert.Equal("GitHub@Secreta123", r.GetProperty("senha").GetString());
        }
    }

    [Fact]
    public async Task GetCredential_IdInexistente_RetornaErro()
    {
        await SemearCofreAsync();
        var (proc, sessao) = Montar();
        using (sessao)
        {
            proc.Processar("""{"tipo":"unlock"}""");
            var r = Json(proc.Processar($$"""{"tipo":"getCredential","id":"{{Guid.NewGuid()}}"}"""));
            Assert.False(r.GetProperty("ok").GetBoolean());
            Assert.Equal("nao_encontrado", r.GetProperty("erro").GetString());
        }
    }

    [Fact]
    public void JsonInvalido_RetornaErro()
    {
        var (proc, sessao) = Montar();
        using (sessao)
        {
            var r = Json(proc.Processar("isto nao e json"));
            Assert.False(r.GetProperty("ok").GetBoolean());
            Assert.Equal("json_invalido", r.GetProperty("erro").GetString());
        }
    }

    [Fact]
    public void TipoDesconhecido_RetornaErro()
    {
        var (proc, sessao) = Montar();
        using (sessao)
        {
            var r = Json(proc.Processar("""{"tipo":"foo"}"""));
            Assert.Equal("tipo_desconhecido", r.GetProperty("erro").GetString());
        }
    }
}
