using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using CofreDeSenhas;

namespace App.Testes
{
    public class AreaTransferenciaSeguraTests
    {
        [AvaloniaFact]
        public async Task CopiarAsync_ColocaOTextoNaAreaDeTransferencia()
        {
            var janela = new Window();
            janela.Show();

            await AreaTransferenciaSegura.CopiarAsync(janela.Clipboard!, "segredo-42");

            Assert.Equal("segredo-42", await janela.Clipboard!.TryGetTextAsync());
        }
    }
}
