using Avalonia;
using Avalonia.Platform;
#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.Win32;
#endif

namespace CofreDeSenhas
{
    internal static class AcessibilidadeSistema
    {
        public static bool AltoContraste { get; private set; }
        public static bool ReduzirMovimento { get; private set; }
        public static double EscalaTextoOs { get; private set; } = 1.0;

        public static event Action? Alterado;

        static AcessibilidadeSistema() => Ler();

        public static void Reavaliar()
        {
            var (contraste, movimento, escala) = (AltoContraste, ReduzirMovimento, EscalaTextoOs);
            Ler();
            if (contraste != AltoContraste || movimento != ReduzirMovimento ||
                Math.Abs(escala - EscalaTextoOs) > 0.001)
                Alterado?.Invoke();
        }

        private static void Ler()
        {
            AltoContraste = LerAltoContraste();
            ReduzirMovimento = LerReduzirMovimento();
            EscalaTextoOs = LerEscalaTexto();
        }

        private static bool LerAltoContraste()
        {
            try
            {
                return Application.Current?.PlatformSettings?.GetColorValues().ContrastPreference
                    == ColorContrastPreference.High;
            }
            catch
            {
                return false;
            }
        }

#if WINDOWS
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref ANIMATIONINFO pvParam, uint fWinIni);

        [StructLayout(LayoutKind.Sequential)]
        private struct ANIMATIONINFO
        {
            public uint cbSize;
            public int iMinAnimate;
        }

        private const uint SPI_GETCLIENTAREAANIMATION = 0x1042;

        private static bool LerReduzirMovimento()
        {
            try
            {
                var info = new ANIMATIONINFO { cbSize = (uint)Marshal.SizeOf<ANIMATIONINFO>() };
                if (SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref info, 0))
                    return info.iMinAnimate == 0;
            }
            catch
            {
            }
            return false;
        }

        private static double LerEscalaTexto()
        {
            try
            {
                using var chave = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Accessibility");
                if (chave?.GetValue("TextScaleFactor") is int fator && fator >= 100)
                    return fator / 100.0;
            }
            catch
            {
            }
            return 1.0;
        }
#else
        private static bool LerReduzirMovimento() => false;

        private static double LerEscalaTexto() => 1.0;
#endif
    }
}
