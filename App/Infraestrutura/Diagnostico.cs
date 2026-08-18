namespace CofreDeSenhas
{
    internal static class Diagnostico
    {
        private const long TamanhoMaximoBytes = 1_000_000;

        private static string CaminhoLog => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            CaminhosApp.PastaDados, "logs", "erros.log");

        public static void Registrar(Exception ex, string? contexto = null)
        {
            try
            {
                var caminho = CaminhoLog;
                var pasta = Path.GetDirectoryName(caminho)!;
                if (!Directory.Exists(pasta))
                    Directory.CreateDirectory(pasta);

                if (File.Exists(caminho) && new FileInfo(caminho).Length > TamanhoMaximoBytes)
                    File.Delete(caminho);

                var prefixoContexto = contexto != null ? $" [{contexto}]" : "";
                var causa = ex.InnerException != null ? $" | Causa: {ex.InnerException.GetType().Name}: {Redigir(ex.InnerException.Message)}" : "";
                var linha = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z{prefixoContexto} {ex.GetType().Name}: {Redigir(ex.Message)}{causa}{Environment.NewLine}";
                File.AppendAllText(caminho, linha);
            }
            catch
            {
            }
        }

        // Evita gravar o caminho do perfil do Windows (que carrega o nome de usuário
        // do SO) quando ele aparece em mensagens de exceção de I/O, ex.: "Could not
        // find file 'C:\Users\alguem\AppData\...\senhas.json.enc'".
        internal static string Redigir(string mensagem)
        {
            var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.IsNullOrEmpty(perfil)
                ? mensagem
                : mensagem.Replace(perfil, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }
    }
}
