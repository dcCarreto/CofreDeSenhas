using CofreDeSenhas;
using Xunit;

namespace App.Testes
{
    public class DetectorPastaNuvemTests
    {
        [Theory]
        [InlineData(@"C:\Users\denis\OneDrive\Documentos\qr.png", "OneDrive")]
        [InlineData(@"C:\Users\denis\OneDrive - Empresa Ltda\qr.png", "OneDrive")]
        [InlineData(@"C:\Users\denis\Dropbox\qr.png", "Dropbox")]
        [InlineData(@"C:\Users\denis\Google Drive\qr.png", "Google Drive")]
        [InlineData(@"C:\Users\denis\GoogleDrive\qr.png", "Google Drive")]
        [InlineData(@"C:\Users\denis\iCloudDrive\qr.png", "iCloud Drive")]
        [InlineData(@"C:\Users\denis\iCloud Drive\qr.png", "iCloud Drive")]
        [InlineData(@"/home/denis/Dropbox/qr.png", "Dropbox")]
        [InlineData(@"C:\Users\denis\dropbox\qr.png", "Dropbox")]
        public void DetectarProvedor_ComPastaConhecida_RetornaORotuloDoProvedor(string caminho, string provedorEsperado)
        {
            Assert.Equal(provedorEsperado, DetectorPastaNuvem.DetectarProvedor(caminho));
        }

        [Theory]
        [InlineData(@"C:\Users\denis\Documentos\qr.png")]
        [InlineData(@"C:\Users\denis\Desktop\backups\qr.png")]
        [InlineData(@"/home/denis/backups/qr.png")]
        [InlineData("")]
        public void DetectarProvedor_ComPastaComum_RetornaNulo(string caminho)
        {
            Assert.Null(DetectorPastaNuvem.DetectarProvedor(caminho));
        }
    }
}
