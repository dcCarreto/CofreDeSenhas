using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoRelatorioSegurancaTests
{
    private readonly ServicoAuditoriaSenha _auditoria = new();

    [Fact]
    public void Gerar_CofreVazio_PontuacaoMaximaETodasContagensZeradas()
    {
        var auditoria = _auditoria.Auditar(Array.Empty<Senha>(), s => s.SenhaHash);

        var relatorio = ServicoRelatorioSeguranca.Gerar(Array.Empty<Senha>(), auditoria);

        Assert.Equal(0, relatorio.TotalSenhas);
        Assert.Equal(100, relatorio.Pontuacao);
        Assert.True(relatorio.SemProblemas);
    }

    [Fact]
    public void Gerar_CofreSemProblemas_PontuacaoMaximaENenhumAchado()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var senha = CriarSenha("Banco", "user", "Senha@123456", referencia.AddDays(-10),
            categoria: Categoria.Finance, url: "https://banco.com", totp: "JBSWY3DPEHPK3PXP");

        var lista = new[] { senha };
        var resultadoAuditoria = _auditoria.Auditar(lista, s => s.SenhaHash, referencia);

        var relatorio = ServicoRelatorioSeguranca.Gerar(lista, resultadoAuditoria);

        Assert.Equal(1, relatorio.TotalSenhas);
        Assert.Equal(100, relatorio.Pontuacao);
        Assert.True(relatorio.SemProblemas);
    }

    [Fact]
    public void Gerar_IdentificaSemTotpSemUrlESemCategoria()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var incompleta = CriarSenha("Servico", "user", "Senha@123456", referencia.AddDays(-10),
            categoria: Categoria.Other, url: null, totp: null);
        var completa = CriarSenha("Outro", "user2", "Outra@Senha1", referencia.AddDays(-10),
            categoria: Categoria.Work, url: "https://exemplo.com", totp: "JBSWY3DPEHPK3PXP");

        var lista = new[] { incompleta, completa };
        var resultadoAuditoria = _auditoria.Auditar(lista, s => s.SenhaHash, referencia);

        var relatorio = ServicoRelatorioSeguranca.Gerar(lista, resultadoAuditoria);

        Assert.Equal(1, relatorio.SemTotp);
        Assert.Equal(1, relatorio.SemUrl);
        Assert.Equal(1, relatorio.SemCategoria);
        Assert.True(relatorio.Pontuacao < 100);
    }

    [Fact]
    public void Gerar_CategoriaOutroComEtiquetaPersonalizada_NaoContaComoSemCategoria()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var comCategoriaPersonalizada = CriarSenha("Servico", "user", "Senha@123456", referencia.AddDays(-10),
            categoria: Categoria.Other, url: "https://exemplo.com", totp: "JBSWY3DPEHPK3PXP");
        comCategoriaPersonalizada.Etiquetas.Add("Streaming");

        var lista = new[] { comCategoriaPersonalizada };
        var resultadoAuditoria = _auditoria.Auditar(lista, s => s.SenhaHash, referencia);

        var relatorio = ServicoRelatorioSeguranca.Gerar(lista, resultadoAuditoria);

        Assert.Equal(0, relatorio.SemCategoria);
    }

    [Fact]
    public void Gerar_SemVazamentosInformados_ComprometidasFicaZero()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var senha = CriarSenha("Servico", "user", "Senha@123456", referencia.AddDays(-10));

        var lista = new[] { senha };
        var resultadoAuditoria = _auditoria.Auditar(lista, s => s.SenhaHash, referencia);

        var relatorio = ServicoRelatorioSeguranca.Gerar(lista, resultadoAuditoria);

        Assert.Equal(0, relatorio.Comprometidas);
    }

    [Fact]
    public void Gerar_ComVazamentosInformados_ContaSenhasComprometidas()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var comprometida = CriarSenha("Servico", "user", "Senha@123456", referencia.AddDays(-10));
        var segura = CriarSenha("Outro", "user2", "Outra@Senha1", referencia.AddDays(-10));

        var lista = new[] { comprometida, segura };
        var resultadoAuditoria = _auditoria.Auditar(lista, s => s.SenhaHash, referencia);
        var vazamentos = new Dictionary<Guid, int> { [comprometida.Id] = 3, [segura.Id] = 0 };

        var relatorio = ServicoRelatorioSeguranca.Gerar(lista, resultadoAuditoria, vazamentos);

        Assert.Equal(1, relatorio.Comprometidas);
    }

    [Fact]
    public void Gerar_TodasAsSenhasComTodosOsProblemas_PontuacaoChegaAZero()
    {
        var referencia = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var pessima1 = CriarSenha("Servico1", "user1", "abc123", referencia.AddDays(-400),
            categoria: Categoria.Other, url: null, totp: null);
        var pessima2 = CriarSenha("Servico2", "user2", "abc123", referencia.AddDays(-400),
            categoria: Categoria.Other, url: null, totp: null);

        var lista = new[] { pessima1, pessima2 };
        var resultadoAuditoria = _auditoria.Auditar(lista, s => s.SenhaHash, referencia);
        var vazamentos = new Dictionary<Guid, int> { [pessima1.Id] = 5, [pessima2.Id] = 2 };

        var relatorio = ServicoRelatorioSeguranca.Gerar(lista, resultadoAuditoria, vazamentos);

        Assert.Equal(2, relatorio.Fracas);
        Assert.Equal(2, relatorio.Repetidas);
        Assert.Equal(2, relatorio.Antigas);
        Assert.Equal(2, relatorio.Comprometidas);
        Assert.Equal(0, relatorio.Pontuacao);
        Assert.False(relatorio.SemProblemas);
    }

    [Fact]
    public void Contagem_RetornaValorDaCategoriaCorrespondente()
    {
        var relatorio = new RelatorioSegurancaCofre
        {
            TotalSenhas = 10,
            Fracas = 1,
            Repetidas = 2,
            Antigas = 3,
            Comprometidas = 4,
            SemTotp = 5,
            SemUrl = 6,
            SemCategoria = 7,
            Pontuacao = 50
        };

        Assert.Equal(1, relatorio.Contagem(CategoriaRelatorioSeguranca.Fraca));
        Assert.Equal(2, relatorio.Contagem(CategoriaRelatorioSeguranca.Repetida));
        Assert.Equal(3, relatorio.Contagem(CategoriaRelatorioSeguranca.Antiga));
        Assert.Equal(4, relatorio.Contagem(CategoriaRelatorioSeguranca.Comprometida));
        Assert.Equal(5, relatorio.Contagem(CategoriaRelatorioSeguranca.SemTotp));
        Assert.Equal(6, relatorio.Contagem(CategoriaRelatorioSeguranca.SemUrl));
        Assert.Equal(7, relatorio.Contagem(CategoriaRelatorioSeguranca.SemCategoria));
    }

    private static Senha CriarSenha(string servico, string usuario, string senha, DateTime atualizacaoUtc,
        Categoria categoria = Categoria.Personal, string? url = "https://exemplo.com", string? totp = "JBSWY3DPEHPK3PXP")
    {
        return new Senha
        {
            NomeServico = servico,
            Usuario = usuario,
            SenhaHash = senha,
            Categoria = categoria,
            Url = url,
            TotpSegredo = totp,
            DataCriacao = atualizacaoUtc,
            DataAtualizacao = atualizacaoUtc
        };
    }
}
