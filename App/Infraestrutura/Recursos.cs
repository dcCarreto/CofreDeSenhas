using System.Drawing;
using System.Reflection;

namespace App
{
    internal static class Recursos
    {
        public static Icon? IconeApp()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
            return stream != null ? new Icon(stream) : null;
        }

        private static Bitmap? _iconeBitmap;

        public static Bitmap? IconeAppBitmap()
        {
            if (_iconeBitmap != null) return _iconeBitmap;
            using var ico = IconeApp();
            if (ico == null) return null;

            using var grande = new Icon(ico, 128, 128);
            _iconeBitmap = grande.ToBitmap();
            return _iconeBitmap;
        }
    }
}
