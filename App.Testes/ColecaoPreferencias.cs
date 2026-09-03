namespace App.Testes
{
    // Serializa as classes que mutam estáticos globais do processo (Preferencias.*,
    // HistoricoPontuacaoSeguranca.CaminhoOverride, Diagnostico.CaminhoLogTestes); em
    // paralelo, uma classe zera o estático que a outra acabou de setar.
    [CollectionDefinition("Preferencias", DisableParallelization = true)]
    public class ColecaoPreferencias { }
}
