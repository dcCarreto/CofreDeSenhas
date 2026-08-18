namespace App.Testes
{
    // Preferencias.Sincronizacao/UltimoBanco/etc. são propriedades estáticas — o
    // xUnit roda classes de teste diferentes em paralelo por padrão, então sem isto
    // uma classe lendo o valor que outra acabou de trocar (mesmo que cada uma
    // restaure o original no finally) é uma corrida real, não hipotética. Toda
    // classe que muta Preferencias.* deve entrar nesta coleção.
    [CollectionDefinition("Preferencias", DisableParallelization = true)]
    public class ColecaoPreferencias { }
}
