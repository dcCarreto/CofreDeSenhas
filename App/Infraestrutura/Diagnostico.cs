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
                var causa = ex.InnerException != null ? $" | Causa: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : "";
                var linha = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z{prefixoContexto} {ex.GetType().Name}: {ex.Message}{causa}{Environment.NewLine}";
                File.AppendAllText(caminho, linha);
            }
            catch
            {
            }
        }
    }
}
