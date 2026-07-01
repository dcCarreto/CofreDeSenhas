using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AppLinux.Janelas;
using QRCoder;
using SkiaSharp;

namespace AppLinux
{
    internal static class QrBackup
    {
        public static async Task OferecerSalvarAsync(Window dono, string senhaMestra)
        {
            var aceitou = await CaixaMensagem.ConfirmarAsync(dono,
                "Deseja salvar um QR code de backup da sua senha mestra?\n\n" +
                "ATENÇÃO: o QR code contém a sua senha mestra de forma legível. " +
                "Qualquer pessoa que escaneie a imagem terá acesso ao seu cofre.\n\n" +
                "Guarde o arquivo em local seguro e offline (ou impresso) — " +
                "nunca em nuvem, e-mail ou pastas compartilhadas.",
                "Backup da senha mestra (QR code)");

            if (!aceitou)
                return;

            var arquivo = await dono.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Salvar QR code da senha mestra",
                SuggestedFileName = $"senha-mestra-qrcode-{DateTime.Now:yyyy-MM-dd}.png",
                DefaultExtension = "png",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Imagem PNG") { Patterns = new[] { "*.png" } }
                }
            });
            if (arquivo == null)
                return;

            try
            {
                await File.WriteAllBytesAsync(arquivo.Path.LocalPath, GerarFolhaBackupPng(senhaMestra));
                await CaixaMensagem.MostrarAsync(dono,
                    "QR code salvo com sucesso. Guarde-o em local seguro.",
                    "Backup da senha mestra");
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(dono,
                    $"Não foi possível salvar o QR code: {ex.Message}",
                    "Erro", TipoMensagem.Erro);
            }
        }

        public static byte[] GerarFolhaBackupPng(string senha)
        {
            using var gerador = new QRCodeGenerator();
            using var dados = gerador.CreateQrCode(senha, QRCodeGenerator.ECCLevel.Q);
            var qrPng = new PngByteQRCode(dados).GetGraphic(10);

            using var qr = SKBitmap.Decode(qrPng);

            const int margem = 40;
            const int topo = 110;
            const int rodape = 86;
            int qrW = qr.Width, qrH = qr.Height;
            int largura = Math.Max(qrW + margem * 2, 420);
            int altura = topo + qrH + rodape;

            var roxo = new SKColor(124, 58, 237);
            var escuro = new SKColor(32, 35, 43);
            var cinza = new SKColor(110, 114, 128);

            using var surface = SKSurface.Create(new SKImageInfo(largura, altura));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            int textoX = margem;
            using (var logo = SKBitmap.Decode(Recursos.LogoPng()))
            {
                if (logo != null)
                {
                    using var paintLogo = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    canvas.DrawBitmap(logo, new SKRect(margem, 30, margem + 36, 30 + 36), paintLogo);
                    textoX = margem + 46;
                }
            }

            using var negrito = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
            using var normal = SKTypeface.FromFamilyName(null, SKFontStyle.Normal);

            using (var paint = new SKPaint { Color = escuro, TextSize = 20, IsAntialias = true, Typeface = negrito })
                canvas.DrawText("Cofre de Senhas", textoX, 31 + 20, paint);
            using (var paint = new SKPaint { Color = roxo, TextSize = 14, IsAntialias = true, Typeface = normal })
                canvas.DrawText("Backup da senha mestra", textoX, 56 + 14, paint);

            int qrX = (largura - qrW) / 2;
            canvas.DrawBitmap(qr, new SKRect(qrX, topo, qrX + qrW, topo + qrH));

            using (var paint = new SKPaint { Color = cinza, TextSize = 11.5f, IsAntialias = true, Typeface = normal })
            {
                string[] aviso =
                {
                    "Contém sua senha mestra ao ser escaneado.",
                    "Guarde em local seguro e offline."
                };
                float y = topo + qrH + 18 + paint.TextSize;
                foreach (var linha in aviso)
                {
                    float w = paint.MeasureText(linha);
                    canvas.DrawText(linha, (largura - w) / 2, y, paint);
                    y += paint.TextSize + 5;
                }
            }

            using var imagem = surface.Snapshot();
            using var png = imagem.Encode(SKEncodedImageFormat.Png, 100);
            return png.ToArray();
        }
    }
}
