namespace CofreDeSenhas
{
    internal static class DetectorPastaNuvem
    {
        private static readonly (string Rotulo, Func<string, bool> Corresponde)[] _provedores =
        {
            ("OneDrive", segmento => segmento.StartsWith("OneDrive", StringComparison.OrdinalIgnoreCase)),
            ("Dropbox", segmento => segmento.Equals("Dropbox", StringComparison.OrdinalIgnoreCase)),
            ("Google Drive", segmento => segmento.Replace(" ", "").Equals("GoogleDrive", StringComparison.OrdinalIgnoreCase)),
            ("iCloud Drive", segmento => segmento.Replace(" ", "").Equals("iCloudDrive", StringComparison.OrdinalIgnoreCase))
        };

        public static string? DetectarProvedor(string caminhoArquivo)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo))
                return null;

            var segmentos = caminhoArquivo.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segmento in segmentos)
            {
                foreach (var (rotulo, corresponde) in _provedores)
                {
                    if (corresponde(segmento))
                        return rotulo;
                }
            }

            return null;
        }
    }
}
