using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

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

        public static byte[] LogoPng()
        {
            using var stream = AssetLoader.Open(_uriIcone);
            using var memoria = new MemoryStream();
            stream.CopyTo(memoria);
            return memoria.ToArray();
        }
    }
}
