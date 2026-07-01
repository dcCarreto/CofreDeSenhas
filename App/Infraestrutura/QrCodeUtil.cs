using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using QRCoder;

namespace App
{
    internal static class QrCodeUtil
    {
        public static void OferecerSalvarBackup(IWin32Window owner, string senhaMestra)
        {
            var resp = MessageBox.Show(owner,
                "Deseja salvar um QR code de backup da sua senha mestra?\n\n" +
                "ATENÇÃO: o QR code contém a sua senha mestra de forma legível. " +
                "Qualquer pessoa que escaneie a imagem terá acesso ao seu cofre.\n\n" +
                "Guarde o arquivo em local seguro e offline (ou impresso) — " +
                "nunca em nuvem, e-mail ou pastas compartilhadas.",
                "Backup da senha mestra (QR code)",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (resp != DialogResult.Yes)
                return;

            using var sfd = new SaveFileDialog
            {
                Title = "Salvar QR code da senha mestra",
                Filter = "Imagem PNG (*.png)|*.png",
                FileName = $"senha-mestra-qrcode-{DateTime.Now:yyyy-MM-dd}.png"
            };
            if (sfd.ShowDialog(owner) != DialogResult.OK)
                return;

            try
            {
                File.WriteAllBytes(sfd.FileName, GerarFolhaBackupPng(senhaMestra));
                MessageBox.Show(owner,
                    "QR code salvo com sucesso. Guarde-o em local seguro.",
                    "Backup da senha mestra", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner,
                    $"Não foi possível salvar o QR code: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static byte[] GerarFolhaBackupPng(string senha)
        {
            using var gerador = new QRCodeGenerator();
            using var dados = gerador.CreateQrCode(senha, QRCodeGenerator.ECCLevel.Q);
            var qrPng = new PngByteQRCode(dados).GetGraphic(10);

            using var qrStream = new MemoryStream(qrPng);
            using var qr = new Bitmap(qrStream);

            const int margem = 40;
            const int topo = 110;
            const int rodape = 86;
            int qrW = qr.Width, qrH = qr.Height;
            int largura = Math.Max(qrW + margem * 2, 420);
            int altura = topo + qrH + rodape;

            var roxo = Color.FromArgb(124, 58, 237);
            var escuro = Color.FromArgb(32, 35, 43);
            var cinza = Color.FromArgb(110, 114, 128);

            var bmp = new Bitmap(largura, altura);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.White);

                var logo = Recursos.IconeAppBitmap();
                int textoX = margem;
                if (logo != null)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(logo, new Rectangle(margem, 30, 36, 36));
                    textoX = margem + 46;
                }
                using (var fTitulo = new Font("Segoe UI", 15, FontStyle.Bold))
                using (var b = new SolidBrush(escuro))
                    g.DrawString("Cofre de Senhas", fTitulo, b, textoX, 31);
                using (var fSub = new Font("Segoe UI", 10.5f))
                using (var b = new SolidBrush(roxo))
                    g.DrawString("Backup da senha mestra", fSub, b, textoX, 56);

                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                int qrX = (largura - qrW) / 2;
                g.DrawImage(qr, new Rectangle(qrX, topo, qrW, qrH));

                using (var fAviso = new Font("Segoe UI", 8.5f))
                using (var b = new SolidBrush(cinza))
                {
                    const string aviso = "Contém sua senha mestra ao ser escaneado.\nGuarde em local seguro e offline.";
                    var fmt = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString(aviso, fAviso, b,
                        new RectangleF(0, topo + qrH + 18, largura, rodape), fmt);
                }
            }

            using var saida = new MemoryStream();
            bmp.Save(saida, ImageFormat.Png);
            bmp.Dispose();
            return saida.ToArray();
        }
    }
}
