using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(App.Testes.TestAppBuilder))]

namespace App.Testes
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<CofreDeSenhas.App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
