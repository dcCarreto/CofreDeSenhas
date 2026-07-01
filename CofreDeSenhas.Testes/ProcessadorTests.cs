using System.Text.Json.Nodes;
using CofreDeSenhas.Nucleo;

namespace CofreDeSenhas.Testes;

public class ProcessadorTests
{
    private static JsonNode Resp(string json) => JsonNode.Parse(json)!;

    private static async Task<(CofreTemporario cofre, SessaoCofre sessao, Processador proc)> DestrancadoAsync()
    {
        var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("GitHub", "dev@exemplo.com", "senha-gh", "https://github.com");
        var sessao = new SessaoCofre(cofre.Pasta, TimeSpan.FromMinutes(30));
        await sessao.DestrancarAsync(cofre.SenhaMestra);
        return (cofre, sessao, new Processador(sessao, () => true));
    }

    [Fact]
    public void JsonInvalido_RetornaErro()
    {
        using var cofre = new CofreTemporario();
        using var sessao = new SessaoCofre(cofre.Pasta);
        var proc = new Processador(sessao, () => false);

        var r = Resp(proc.Processar("{isto nao e json"));

        Assert.False(r["ok"]!.GetValue<bool>());
        Assert.Equal("json_invalido", r["erro"]!.GetValue<string>());
    }

    [Fact]
    public void TipoDesconhecido_RetornaErro()
    {
        using var cofre = new CofreTemporario();
        using var sessao = new SessaoCofre(cofre.Pasta);
        var proc = new Processador(sessao, () => false);

        Assert.Equal("tipo_desconhecido",
            Resp(proc.Processar("{\"tipo\":\"voar\"}"))["erro"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("{\"tipo\":\"removeCredential\",\"id\":\"x\"}")]
    [InlineData("{\"tipo\":\"delete\",\"id\":\"x\"}")]
    [InlineData("{\"tipo\":\"excluir\",\"id\":\"x\"}")]
    public async Task NaoExisteComandoDeRemocao(string requisicao)
    {
        var (cofre, sessao, proc) = await DestrancadoAsync();
        using (cofre)
        using (sessao)
        {
            Assert.Equal("tipo_desconhecido", Resp(proc.Processar(requisicao))["erro"]!.GetValue<string>());
        }
    }

    [Fact]
    public void Status_SemCofre_InformaNaoEncontrado()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "cofre-teste-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        try
        {
            using var sessao = new SessaoCofre(pasta);
            var r = Resp(new Processador(sessao, () => false).Processar("{\"tipo\":\"status\"}"));

            Assert.True(r["ok"]!.GetValue<bool>());
            Assert.False(r["destrancado"]!.GetValue<bool>());
            Assert.False(r["cofre"]!["pronto"]!.GetValue<bool>());
        }
        finally
        {
            Directory.Delete(pasta, true);
        }
    }

    [Fact]
    public async Task Unlock_ChamaSolicitarUnlock()
    {
        using var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("Servico", "u", "s", "https://x.com");
        using var sessao = new SessaoCofre(cofre.Pasta, TimeSpan.FromMinutes(30));

        var chamou = false;
        var proc = new Processador(sessao, () =>
        {
            chamou = true;
            return sessao.DestrancarAsync(cofre.SenhaMestra).GetAwaiter().GetResult();
        });

        var r = Resp(proc.Processar("{\"tipo\":\"unlock\"}"));

        Assert.True(chamou);
        Assert.Equal("unlocked", r["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task Query_Bloqueado_RetornaErro()
    {
        using var cofre = new CofreTemporario();
        await cofre.AdicionarPelaBaseAsync("Servico", "u", "s", "https://github.com");
        using var sessao = new SessaoCofre(cofre.Pasta, TimeSpan.FromMinutes(30));
        var proc = new Processador(sessao, () => false);

        Assert.Equal("bloqueado",
            Resp(proc.Processar("{\"tipo\":\"query\",\"dominio\":\"github.com\"}"))["erro"]!.GetValue<string>());
    }

    [Fact]
    public async Task Query_Destrancado_RetornaItens()
    {
        var (cofre, sessao, proc) = await DestrancadoAsync();
        using (cofre)
        using (sessao)
        {
            var r = Resp(proc.Processar("{\"tipo\":\"query\",\"dominio\":\"github.com\"}"));
            Assert.True(r["ok"]!.GetValue<bool>());
            Assert.Single(r["itens"]!.AsArray());
        }
    }

    [Fact]
    public async Task GetCredential_RetornaSenha()
    {
        var (cofre, sessao, proc) = await DestrancadoAsync();
        using (cofre)
        using (sessao)
        {
            var id = Resp(proc.Processar("{\"tipo\":\"query\",\"dominio\":\"github.com\"}"))
                ["itens"]!.AsArray()[0]!["id"]!.GetValue<string>();

            var r = Resp(proc.Processar($"{{\"tipo\":\"getCredential\",\"id\":\"{id}\"}}"));
            Assert.Equal("senha-gh", r["senha"]!.GetValue<string>());
        }
    }

    [Fact]
    public async Task AddCredential_GravaNoCofre()
    {
        var (cofre, sessao, proc) = await DestrancadoAsync();
        using (cofre)
        using (sessao)
        {
            var r = Resp(proc.Processar(
                "{\"tipo\":\"addCredential\",\"servico\":\"Nova\",\"usuario\":\"n@x.com\",\"senha\":\"p\",\"url\":\"https://nova.com\"}"));

            Assert.True(r["ok"]!.GetValue<bool>());
            var todas = await cofre.LerPelaBaseAsync();
            Assert.Contains(todas, s => s.NomeServico == "Nova");
        }
    }

    [Fact]
    public async Task UpdateCredential_AlteraNoCofre()
    {
        var (cofre, sessao, proc) = await DestrancadoAsync();
        using (cofre)
        using (sessao)
        {
            var id = Resp(proc.Processar("{\"tipo\":\"query\",\"dominio\":\"github.com\"}"))
                ["itens"]!.AsArray()[0]!["id"]!.GetValue<string>();

            var r = Resp(proc.Processar(
                $"{{\"tipo\":\"updateCredential\",\"id\":\"{id}\",\"servico\":\"Editado\",\"usuario\":\"dev@exemplo.com\",\"senha\":\"\",\"url\":\"https://github.com\",\"categoria\":\"Other\"}}"));

            Assert.True(r["ok"]!.GetValue<bool>());
            var salva = (await cofre.LerPelaBaseAsync()).Single(s => s.Id == Guid.Parse(id));
            Assert.Equal("Editado", salva.NomeServico);
        }
    }
}
