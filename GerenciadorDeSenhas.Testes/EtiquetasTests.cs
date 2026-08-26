using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class EtiquetasTests
{
    private static Senha NovaSenha(params string[] etiquetas)
    {
        var senha = new Senha { NomeServico = "s", Usuario = "u", SenhaHash = "c" };
        senha.Etiquetas.AddRange(etiquetas);
        return senha;
    }

    [Fact]
    public void Normalizar_RemoveDuplicatasIgnorandoCaixa()
    {
        var resultado = Etiquetas.Normalizar(new[] { "Trabalho", "trabalho", "TRABALHO" });

        Assert.Single(resultado);
    }

    [Fact]
    public void Normalizar_CortaNoLimitePadraoDeVinte()
    {
        var muitas = Enumerable.Range(0, 30).Select(i => $"tag-{i}");

        var resultado = Etiquetas.Normalizar(muitas);

        Assert.Equal(Etiquetas.QuantidadeMaxima, resultado.Count);
    }

    [Fact]
    public void Normalizar_ComLimiteExplicito_RespeitaOLimitePassado()
    {
        var muitas = Enumerable.Range(0, 50).Select(i => $"tag-{i}");

        var resultado = Etiquetas.Normalizar(muitas, limite: int.MaxValue);

        Assert.Equal(50, resultado.Count);
    }

    [Fact]
    public void Distintas_ComMaisDeVinteEtiquetasNoCofreInteiro_NaoCortaNoTetoPorCredencial()
    {
        // Nenhuma credencial individual passa do teto de 20 (cada uma só tem 1
        // etiqueta), mas o cofre inteiro soma 25 etiquetas distintas — o filtro
        // precisa listar todas, não só as 20 primeiras encontradas.
        var senhas = Enumerable.Range(0, 25).Select(i => NovaSenha($"tag-{i}"));

        var resultado = Etiquetas.Distintas(senhas);

        Assert.Equal(25, resultado.Count);
    }

    [Fact]
    public void Distintas_RemoveDuplicatasEntreCredenciaisDiferentes()
    {
        var senhas = new[] { NovaSenha("Trabalho"), NovaSenha("trabalho"), NovaSenha("Pessoal") };

        var resultado = Etiquetas.Distintas(senhas);

        Assert.Equal(2, resultado.Count);
    }
}
