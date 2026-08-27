namespace GerenciadorDeSenhas.Testes;

public class AmbienteCofreTests
{
    [Fact]
    public void PastaDados_DuranteOsTestes_NuncaEHOAppDataRealDoCofreInstalado()
    {
        var appDataReal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AmbienteCofre.NomePastaDados);

        Assert.NotEqual(appDataReal, AmbienteCofre.PastaDados);

        var baseIsolada = Environment.GetEnvironmentVariable("COFRE_BASE");
        Assert.False(string.IsNullOrWhiteSpace(baseIsolada));
        Assert.StartsWith(baseIsolada!, AmbienteCofre.PastaDados);
    }

    [Fact]
    public void Isolado_DuranteOsTestes_EstaAtivo()
    {
        // Se isto falhar, os no-ops de Windows Hello (ServicoDesbloqueioBiometrico)
        // não estão em vigor e um `dotnet test` pode apagar a credencial global do
        // Windows Hello do cofre instalado.
        Assert.True(AmbienteCofre.Isolado);
    }
}
