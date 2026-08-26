using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace CofreDeSenhas.Gerador
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            Preferencias.Carregar();
            Idioma.Definir(Preferencias.Idioma);
            Acessibilidade.Hidratar(
                ResolverDaltonismo(Preferencias.Daltonismo),
                Preferencias.AltoContraste,
                Preferencias.EscalaInterface,
                Preferencias.ReduzirAnimacoes,
                Preferencias.LeitorTela);
            Acessibilidade.Aplicar();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new JanelaGerador();

            base.OnFrameworkInitializationCompleted();
        }

        private static TipoDaltonismo ResolverDaltonismo(string? valor) =>
            Enum.TryParse<TipoDaltonismo>(valor, out var tipo) ? tipo : TipoDaltonismo.Nenhum;
    }
}
