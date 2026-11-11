using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CofreDeSenhas.Controles;

namespace CofreDeSenhas
{
    internal static class Recursos
    {
        private static readonly Uri _uriIcone = new("avares://CofreDeSenhas/Ativos/app.png");

        private static Bitmap? _logo;

        public static Bitmap Logo
        {
            get
            {
                if (_logo == null)
                {
                    using var stream = AssetLoader.Open(_uriIcone);
                    _logo = new Bitmap(stream);
                }
                return _logo;
            }
        }

        public static WindowIcon IconeApp() => new(Logo);

        // No Windows 11 os ícones da bandeja são glifos monocromáticos que seguem o
        // tema da barra de tarefas (que pode diferir do tema dos apps). Fora do
        // Windows a bandeja usa o ícone colorido do app.
        public static WindowIcon IconeBandeja()
        {
            if (!OperatingSystem.IsWindows())
                return IconeApp();

            var nome = BarraDeTarefasClara() ? "bandeja-escura.ico" : "bandeja-clara.ico";
            using var stream = AssetLoader.Open(new Uri("avares://CofreDeSenhas/Ativos/" + nome));
            return new WindowIcon(stream);
        }

        private static bool BarraDeTarefasClara()
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                using var chave = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return chave?.GetValue("SystemUsesLightTheme") is int valor && valor != 0;
            }
            catch
            {
                return false;
            }
        }

        public static byte[] LogoPng()
        {
            using var stream = AssetLoader.Open(_uriIcone);
            using var memoria = new MemoryStream();
            stream.CopyTo(memoria);
            return memoria.ToArray();
        }

        public static Icone ImagemIcone(string chave, double tamanho, IBrush? cor = null, bool preenchido = false)
        {
            var icone = new Icone { Width = tamanho, Height = tamanho, Chave = chave, Preenchido = preenchido };
            if (cor != null)
            {
                if (preenchido)
                    icone.Fill = cor;
                else
                    icone.Stroke = cor;
            }
            return icone;
        }
    }
}
