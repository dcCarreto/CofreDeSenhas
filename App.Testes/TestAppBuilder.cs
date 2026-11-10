using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;

[assembly: AvaloniaTestApplication(typeof(App.Testes.TestAppBuilder))]

namespace App.Testes
{
    public static class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<CofreDeSenhas.App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .AfterSetup(_ => VincularFontManagerNovo());

        // No isolamento por teste (o padrão do Avalonia.Headless), o fim de cada teste
        // descarta o FontManager visível e o Dispose dele limpa as coleções de fontes.
        // De vez em quando um teste seguinte ainda enxergava essa instância descartada:
        // FontManager.Current a devolvia, e o construtor de Window quebrava com
        // KeyNotFoundException 'fonts:SystemFonts' — depois disso o dotnet test travava
        // até o limite de 6h do GitHub Actions (release.yml e ci.yml, 2026-11-10).
        // Vincular uma instância nova no escopo de cada teste, logo na inicialização,
        // faz FontManager.Current achá-la antes de qualquer instância antiga.
        private static void VincularFontManagerNovo()
        {
            var implementacao = AvaloniaLocator.Current.GetRequiredService<IFontManagerImpl>();
            AvaloniaLocator.CurrentMutable.Bind<FontManager>().ToConstant(new FontManager(implementacao));
        }
    }
}
