using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;

namespace CofreDeSenhas
{
    // Material Mica do Windows 11. A janela pede o backdrop do sistema e as
    // superfícies dela usam os pincéis "Material*" (fundo transparente e cartões
    // translúcidos, ver Acessibilidade.Aplicar) para o Mica aparecer por trás.
    //
    // O backdrop preenche o retângulo inteiro da janela, então a moldura perde a
    // margem/sombra desenhadas pelo app e os cantos passam a ser arredondados pelo
    // próprio DWM, no raio padrão do Windows 11.
    public static class Material
    {
        public const double RaioCanto = 8;

        private const int DwmaCantoPreferido = 33; // DWMWA_WINDOW_CORNER_PREFERENCE
        private const int CantoArredondado = 2;     // DWMWCP_ROUND

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int atributo, ref int valor, int tamanho);

        public static bool Suportado => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

        public static void Aplicar(Window janela)
        {
            if (!Suportado)
                return;

            janela.TransparencyLevelHint = new[]
            {
                WindowTransparencyLevel.Mica,
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Transparent
            };

            if ((janela.Content as Border ?? janela.FindControl<Border>("Moldura")) is { } moldura)
                AjustarMoldura(janela, moldura);

            janela.Opened += (_, _) => ArredondarCantos(janela);
            // Se o sistema não entregar o Mica (falha de composição, por exemplo), a
            // janela ganha o fundo opaco da paleta para não ficar vazada.
            AjustarFundo(janela);
            janela.PropertyChanged += (_, e) =>
            {
                if (e.Property == TopLevel.ActualTransparencyLevelProperty)
                    AjustarFundo(janela);
            };
        }

        private static void AjustarFundo(Window janela)
        {
            var nivel = janela.ActualTransparencyLevel;
            if (nivel == WindowTransparencyLevel.Mica || nivel == WindowTransparencyLevel.AcrylicBlur)
                janela.Background = Brushes.Transparent;
            else
                janela[!TemplatedControl.BackgroundProperty] = janela.GetResourceObservable("WorkspaceBackground").ToBinding();
        }

        private static void AjustarMoldura(Window janela, Border moldura)
        {
            var margem = moldura.Margin;
            if (margem != default)
            {
                double dx = margem.Left + margem.Right;
                double dy = margem.Top + margem.Bottom;
                if (!double.IsNaN(janela.Width)) janela.Width -= dx;
                if (!double.IsNaN(janela.Height)) janela.Height -= dy;
                if (janela.MinWidth > 0) janela.MinWidth = Math.Max(0, janela.MinWidth - dx);
                if (janela.MinHeight > 0) janela.MinHeight = Math.Max(0, janela.MinHeight - dy);
                moldura.Margin = default;
            }

            moldura.BoxShadow = default;
            moldura.CornerRadius = new CornerRadius(RaioCanto);
        }

        private static void ArredondarCantos(Window janela)
        {
            if (janela.TryGetPlatformHandle()?.Handle is { } hwnd && hwnd != IntPtr.Zero)
            {
                int canto = CantoArredondado;
                DwmSetWindowAttribute(hwnd, DwmaCantoPreferido, ref canto, sizeof(int));
            }
        }
    }
}
