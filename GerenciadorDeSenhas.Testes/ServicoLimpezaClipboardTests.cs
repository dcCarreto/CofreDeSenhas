using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoLimpezaClipboardTests
{
    private static Func<int, Task> AguardarInstantaneo(List<int> segundosAguardados) =>
        s =>
        {
            segundosAguardados.Add(s);
            return Task.CompletedTask;
        };

    [Fact]
    public async Task ProgramarLimpezaAsync_ClipboardInalterado_LimpaAposEspera()
    {
        var clipboard = new AreaTransferenciaEmMemoria();
        await clipboard.DefinirTextoAsync("senha-secreta");
        var esperas = new List<int>();

        await ServicoLimpezaClipboard.ProgramarLimpezaAsync(clipboard, "senha-secreta", 30, AguardarInstantaneo(esperas));

        Assert.Equal("", clipboard.Texto);
        Assert.Equal(new[] { 30 }, esperas);
    }

    [Fact]
    public async Task ProgramarLimpezaAsync_ClipboardAlteradoPeloUsuario_NaoSobrescreve()
    {
        var clipboard = new AreaTransferenciaEmMemoria();
        await clipboard.DefinirTextoAsync("senha-secreta");

        await ServicoLimpezaClipboard.ProgramarLimpezaAsync(clipboard, "senha-secreta", 30, async s =>
        {
            await clipboard.DefinirTextoAsync("outro-conteudo-copiado-depois");
        });

        Assert.Equal("outro-conteudo-copiado-depois", clipboard.Texto);
    }

    [Fact]
    public async Task ProgramarLimpezaAsync_TempoDesativado_NaoEsperaNemLimpa()
    {
        var clipboard = new AreaTransferenciaEmMemoria();
        await clipboard.DefinirTextoAsync("senha-secreta");
        var esperas = new List<int>();

        await ServicoLimpezaClipboard.ProgramarLimpezaAsync(clipboard, "senha-secreta", 0, AguardarInstantaneo(esperas));

        Assert.Equal("senha-secreta", clipboard.Texto);
        Assert.Empty(esperas);
    }

    [Fact]
    public async Task ProgramarLimpezaAsync_SemTextoCopiado_NaoFazNada()
    {
        var clipboard = new AreaTransferenciaEmMemoria();
        var esperas = new List<int>();

        await ServicoLimpezaClipboard.ProgramarLimpezaAsync(clipboard, "", 30, AguardarInstantaneo(esperas));

        Assert.Null(clipboard.Texto);
        Assert.Empty(esperas);
    }
}
