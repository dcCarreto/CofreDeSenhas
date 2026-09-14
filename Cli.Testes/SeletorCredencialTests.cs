using CofreDeSenhas.Cli;
using GerenciadorDeSenhas.Modelos;

namespace Cli.Testes;

public class SeletorCredencialTests
{
    private static Senha Cred(string servico, string usuario) =>
        new() { NomeServico = servico, Usuario = usuario, SenhaHash = "x" };

    private static readonly List<Senha> Cofre = new()
    {
        Cred("GitHub", "denis"),
        Cred("GitHub", "trabalho@exemplo.com"),
        Cred("GitLab", "denis"),
        Cred("Banco do Brasil", "12345"),
    };

    [Fact]
    public void NomeExatoUnico_Encontra()
    {
        var r = SeletorCredencial.Selecionar(Cofre, "GitLab", null);
        Assert.Equal(TipoSelecao.Encontrada, r.Tipo);
        Assert.Equal("GitLab", r.Unica!.NomeServico);
    }

    [Fact]
    public void NomeExatoComVariosUsuarios_Ambiguo()
    {
        var r = SeletorCredencial.Selecionar(Cofre, "GitHub", null);
        Assert.Equal(TipoSelecao.Ambigua, r.Tipo);
        Assert.Equal(2, r.Candidatas.Count);
    }

    [Fact]
    public void NomeExatoAmbiguo_DesempataPorUsuario()
    {
        var r = SeletorCredencial.Selecionar(Cofre, "GitHub", "trabalho");
        Assert.Equal(TipoSelecao.Encontrada, r.Tipo);
        Assert.Equal("trabalho@exemplo.com", r.Unica!.Usuario);
    }

    [Fact]
    public void ContemNoNome_QuandoUnico_Encontra()
    {
        var r = SeletorCredencial.Selecionar(Cofre, "banco", null);
        Assert.Equal(TipoSelecao.Encontrada, r.Tipo);
        Assert.Equal("Banco do Brasil", r.Unica!.NomeServico);
    }

    [Fact]
    public void ContemNoNome_QuandoVarios_Ambiguo()
    {
        var r = SeletorCredencial.Selecionar(Cofre, "git", null);
        Assert.Equal(TipoSelecao.Ambigua, r.Tipo);
        Assert.Equal(3, r.Candidatas.Count);
    }

    [Fact]
    public void SemMatch_Nenhuma()
    {
        var r = SeletorCredencial.Selecionar(Cofre, "netflix", null);
        Assert.Equal(TipoSelecao.Nenhuma, r.Tipo);
    }

    [Fact]
    public void CasaPeloUsuarioQuandoNaoBateNoNome()
    {
        var r = SeletorCredencial.Selecionar(Cofre, "12345", null);
        Assert.Equal(TipoSelecao.Encontrada, r.Tipo);
        Assert.Equal("Banco do Brasil", r.Unica!.NomeServico);
    }
}
