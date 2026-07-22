using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class AgendaBackupTests
{
    private static readonly DateTime Agora = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Devido_FrequenciaManual_NuncaEDevido()
    {
        Assert.False(AgendaBackup.Devido(null, FrequenciaBackup.Manual, Agora));
        Assert.False(AgendaBackup.Devido(Agora.AddYears(-1), FrequenciaBackup.Manual, Agora));
    }

    [Theory]
    [InlineData(FrequenciaBackup.Diario)]
    [InlineData(FrequenciaBackup.Semanal)]
    public void Devido_SemBackupAnterior_EDevido(FrequenciaBackup frequencia)
    {
        Assert.True(AgendaBackup.Devido(null, frequencia, Agora));
    }

    [Fact]
    public void Devido_Diario_AntesDeUmDia_NaoEDevido()
    {
        var ultimo = Agora.AddHours(-23);
        Assert.False(AgendaBackup.Devido(ultimo, FrequenciaBackup.Diario, Agora));
    }

    [Fact]
    public void Devido_Diario_ApósUmDia_EDevido()
    {
        var ultimo = Agora.AddDays(-1);
        Assert.True(AgendaBackup.Devido(ultimo, FrequenciaBackup.Diario, Agora));
    }

    [Fact]
    public void Devido_Semanal_AntesDeSeteDias_NaoEDevido()
    {
        var ultimo = Agora.AddDays(-6);
        Assert.False(AgendaBackup.Devido(ultimo, FrequenciaBackup.Semanal, Agora));
    }

    [Fact]
    public void Devido_Semanal_AposSeteDias_EDevido()
    {
        var ultimo = Agora.AddDays(-7);
        Assert.True(AgendaBackup.Devido(ultimo, FrequenciaBackup.Semanal, Agora));
    }
}
