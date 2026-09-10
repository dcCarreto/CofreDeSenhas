namespace GerenciadorDeSenhas.Servicos
{
    public enum FrequenciaBackup
    {
        Manual = 0,
        Diario = 1,
        Semanal = 2
    }

    public static class AgendaBackup
    {
        public static bool Devido(DateTime? ultimoBackupUtc, FrequenciaBackup frequencia, DateTime agoraUtc)
        {
            if (frequencia == FrequenciaBackup.Manual)
                return false;

            if (ultimoBackupUtc == null)
                return true;

            var intervalo = frequencia == FrequenciaBackup.Diario ? TimeSpan.FromDays(1) : TimeSpan.FromDays(7);
            return agoraUtc - ultimoBackupUtc.Value >= intervalo;
        }
    }
}
