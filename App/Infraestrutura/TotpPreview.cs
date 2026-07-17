using System.Globalization;
using Avalonia.Media;
using Avalonia.Threading;

namespace CofreDeSenhas
{
    internal static class TotpPreview
    {
        public static string FormatarCodigo(string codigo) =>
            codigo.Length == 6 ? codigo.Insert(3, " ") : codigo;

        public static Geometry? ConstruirAnelProgresso(int restantes, int periodo, double raio, double centro)
        {
            double fracao = periodo <= 0 ? 0 : Math.Clamp(restantes / (double)periodo, 0, 1);
            double angulo = fracao * 360;
            if (angulo <= 0.1)
                return null;
            if (angulo >= 359.9)
                angulo = 359.9;

            double rad = angulo * Math.PI / 180.0;
            double fx = centro + raio * Math.Sin(rad);
            double fy = centro - raio * Math.Cos(rad);
            int grande = angulo > 180 ? 1 : 0;
            return StreamGeometry.Parse(string.Format(CultureInfo.InvariantCulture,
                "M {0} {1} A {2} {2} 0 {3} 1 {4:0.##} {5:0.##}", centro, centro - raio, raio, grande, fx, fy));
        }

        public sealed class Temporizador
        {
            private DispatcherTimer? _timer;

            public void Garantir(Action tick)
            {
                if (_timer != null)
                    return;

                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += (s, e) => tick();
                _timer.Start();
            }

            public void Parar()
            {
                _timer?.Stop();
                _timer = null;
            }
        }
    }
}
