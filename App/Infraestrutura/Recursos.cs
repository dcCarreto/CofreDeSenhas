using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace CofreDeSenhas
{
    internal static class Recursos
    {
        private static readonly Uri _uriIcone = new("avares://CofreDeSenhas/Ativos/app.png");

        private static Bitmap? _logo;
        private static readonly Dictionary<string, Bitmap> _iconesPng = new();

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

        public static Bitmap IconePng(string chave)
        {
            if (!_iconesPng.TryGetValue(chave, out var bitmap))
            {
                using var stream = AssetLoader.Open(new Uri($"avares://CofreDeSenhas/Ativos/Icones/{chave}.png"));
                bitmap = new Bitmap(stream);
                _iconesPng[chave] = bitmap;
            }
            return bitmap;
        }

        public static Image ImagemIcone(string chave, double tamanho) => new()
        {
            Width = tamanho,
            Height = tamanho,
            Stretch = Stretch.Uniform,
            Source = IconePng(chave)
        };
    }
}
