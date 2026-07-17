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
            var (texto, cor) = ForcaSenha.Descrever(nivel);

            LblForca.Text = texto;
            LblForca.Foreground = Tema.Pincel(cor);
            AutomationProperties.SetName(LblForca, $"{Idioma.Texto("Generator.Strength")}: {texto}");

            var segmentos = new[] { Seg1, Seg2, Seg3, Seg4 };
            for (int i = 0; i < segmentos.Length; i++)
                segmentos[i].Background = Tema.Pincel(i < nivel ? cor : Tema.TrailInactive);
        }
    }
}
