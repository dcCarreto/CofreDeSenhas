namespace App.Testes
{
    // Bucket de testes que mutam estado global do processo: Preferencias.* (Sincronizacao,
    // UltimoBanco, ...), HistoricoPontuacaoSeguranca.CaminhoOverride e
    // Diagnostico.CaminhoLogTestes. O xUnit roda classes de teste diferentes em paralelo
    // por padrão, então sem isto uma classe lendo/zerando o estático que outra acabou de
    // setar (mesmo com restauração no finally) é uma corrida real, não hipotética. Toda
    // classe que muta um desses estáticos deve entrar nesta coleção.
    [CollectionDefinition("Preferencias", DisableParallelization = true)]
    public class ColecaoPreferencias { }
}
