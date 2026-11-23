namespace CofreDeSenhas
{
    // Instalado pelo pacote .deb (Linux Mint, Ubuntu e derivados): o app fica em
    // /opt/cofre-de-senhas, do root, e se atualiza pelo atualizar-pacote via pkexec,
    // sem senha (ver App/distribuicao/deb).
    internal static class PacoteDeb
    {
        internal const string Pasta = "/opt/cofre-de-senhas";
        internal const string Ajudante = Pasta + "/atualizar-pacote";

        // Este processo é o app do pacote.
        public static bool Instalado =>
            PresenteNoSistema &&
            Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory) == Pasta;

        // O pacote está instalado, rodando este processo ou não (o AppImage, por exemplo).
        public static bool PresenteNoSistema =>
            OperatingSystem.IsLinux() && File.Exists(Ajudante);
    }
}
