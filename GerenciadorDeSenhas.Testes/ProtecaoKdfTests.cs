using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ProtecaoKdfTests
{
    [Theory]
    [InlineData(3, 65536, 1)]
    [InlineData(1, 8, 1)]
    [InlineData(ProtecaoKdf.IteracoesArgonMaximas, ProtecaoKdf.MemoriaKbMaxima, ProtecaoKdf.ParalelismoMaximo)]
    public void Argon2idDentroDoLimite_AceitaValoresRazoaveis(int iteracoes, int memoriaKb, int paralelismo)
    {
        Assert.True(ProtecaoKdf.Argon2idDentroDoLimite(iteracoes, memoriaKb, paralelismo));
    }

    [Theory]
    [InlineData(0, 65536, 1)]
    [InlineData(3, 65536, 0)]
    [InlineData(3, 7, 1)]
    [InlineData(3, int.MaxValue, 1)]
    [InlineData(int.MaxValue, 65536, 1)]
    [InlineData(3, 65536, 10_000)]
    [InlineData(-1, 65536, 1)]
    public void Argon2idDentroDoLimite_RejeitaValoresForaDaFaixa(int iteracoes, int memoriaKb, int paralelismo)
    {
        Assert.False(ProtecaoKdf.Argon2idDentroDoLimite(iteracoes, memoriaKb, paralelismo));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProtecaoKdf.GarantirArgon2id(iteracoes, memoriaKb, paralelismo));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(600_000)]
    [InlineData(ProtecaoKdf.IteracoesPbkdf2Maximas)]
    public void Pbkdf2DentroDoLimite_AceitaValoresRazoaveis(int iteracoes)
    {
        Assert.True(ProtecaoKdf.Pbkdf2DentroDoLimite(iteracoes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Pbkdf2DentroDoLimite_RejeitaValoresForaDaFaixa(int iteracoes)
    {
        Assert.False(ProtecaoKdf.Pbkdf2DentroDoLimite(iteracoes));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProtecaoKdf.GarantirPbkdf2(iteracoes));
    }
}
