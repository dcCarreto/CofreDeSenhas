namespace CofreDeSenhas
{
    internal static class CaminhosApp
    {
        // Caminho completo da pasta de dados. A regra de isolamento entre o app
        // instalado e as execuções de desenvolvimento/teste fica em
        // GerenciadorDeSenhas.AmbienteCofre.
        public static string PastaDados => GerenciadorDeSenhas.AmbienteCofre.PastaDados;
    }
}
