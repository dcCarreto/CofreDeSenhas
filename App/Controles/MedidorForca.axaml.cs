using Avalonia.Automation;
using Avalonia.Controls;

namespace CofreDeSenhas.Controles
{
    public partial class MedidorForca : UserControl
    {
        public MedidorForca()
        {
            InitializeComponent();
            Avaliar("");
        }

        public void Avaliar(string? senha)
        {
            int nivel = ForcaSenha.Calcular(senha ?? "");
            var (texto, cor) = nivel switch
            {
                1 => (Idioma.Texto("Generator.StrengthWeak"), Tema.StrengthWeak),
                2 => (Idioma.Texto("Generator.StrengthMedium"), Tema.StrengthMedium),
                3 => (Idioma.Texto("Generator.StrengthStrong"), Tema.StrengthStrong),
                4 => (Idioma.Texto("Generator.StrengthExcellent"), Tema.StrengthExcellent),
                _ => ("—", Tema.TextSecondary)
            };

            LblForca.Text = texto;
            LblForca.Foreground = Tema.Pincel(cor);
            AutomationProperties.SetName(LblForca, $"{Idioma.Texto("Generator.Strength")}: {texto}");

            var segmentos = new[] { Seg1, Seg2, Seg3, Seg4 };
            for (int i = 0; i < segmentos.Length; i++)
                segmentos[i].Background = Tema.Pincel(i < nivel ? cor : Tema.TrailInactive);
        }
    }
}
