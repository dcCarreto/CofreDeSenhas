using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoAuditoriaSenhaTests
{
    private readonly ServicoAuditoriaSenha _servico = new();

    [Fact]
    public void Auditar_IdentificaSenhasFracasRepetidasEAntigas()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var fracaRepetidaAntiga = CriarSenha("Email", "a@example.com", "abc123", referencia.AddDays(-400));
        var fracaRepetida = CriarSenha("Forum", "b@example.com", "abc123", referencia.AddDays(-20));
        var forte = CriarSenha("Banco", "c@example.com", "Senha@123456", referencia.AddDays(-20));

        var resultado = _servico.Auditar(new[] { fracaRepetidaAntiga, fracaRepetida, forte },
            s => s.SenhaHash, referencia);

        Assert.Equal(3, resultado.TotalSenhas);
        Assert.Equal(2, resultado.TotalComAchados);
        Assert.Equal(2, resultado.TotalFracas);
        Assert.Equal(2, resultado.TotalRepetidas);
        Assert.Equal(1, resultado.TotalAntigas);

        var itemAntigo = Assert.Single(resultado.Itens, i => i.Senha.Id == fracaRepetidaAntiga.Id);
        Assert.Contains(TipoAchadoAuditoriaSenha.Fraca, itemAntigo.Achados);
        Assert.Contains(TipoAchadoAuditoriaSenha.Repetida, itemAntigo.Achados);
        Assert.Contains(TipoAchadoAuditoriaSenha.Antiga, itemAntigo.Achados);
        Assert.Equal(2, itemAntigo.OcorrenciasSenhaRepetida);
    }

    [Fact]
    public void Auditar_NaoMarcaPassphraseLongaComoFraca()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var passphrase = CriarSenha("Notas", "user", "correto-cavalo-bateria-grampeador",
            referencia.AddDays(-30));

        var resultado = _servico.Auditar(new[] { passphrase }, s => s.SenhaHash, referencia);

        Assert.Empty(resultado.Itens);
        Assert.Equal(0, resultado.TotalFracas);
    }

    [Fact]
    public void Auditar_ContaSenhaNaoAuditadaEMantemAchadoDeAntiguidade()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var antiga = CriarSenha("Legado", "user", "valor-cifrado", referencia.AddDays(-366));

        var resultado = _servico.Auditar(new[] { antiga }, _ => null, referencia);

        Assert.Equal(1, resultado.NaoAuditadas);
        var item = Assert.Single(resultado.Itens);
        Assert.Contains(TipoAchadoAuditoriaSenha.Antiga, item.Achados);
        Assert.DoesNotContain(TipoAchadoAuditoriaSenha.Fraca, item.Achados);
        Assert.DoesNotContain(TipoAchadoAuditoriaSenha.Repetida, item.Achados);
    }

    private static Senha CriarSenha(string servico, string usuario, string senha, DateTime atualizacaoUtc)
    {
        return new Senha
        {
            NomeServico = servico,
            Usuario = usuario,
            SenhaHash = senha,
            Categoria = Categoria.Personal,
            DataCriacao = atualizacaoUtc,
            DataAtualizacao = atualizacaoUtc
        };
    }
}
