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
                ResolverModoTema(Preferencias.ModoTema),
                ResolverCorDestaque(Preferencias.CorDestaque),
                ResolverDensidade(Preferencias.Densidade),
                ResolverLayoutDetalhe(Preferencias.LayoutDetalhe),
                Preferencias.AltoContraste,
                Preferencias.EscalaInterface,
                Preferencias.ReduzirAnimacoes,
                Preferencias.LeitorTela);
            Acessibilidade.HidratarExpert(
                ResolverContraste(Preferencias.NivelContraste, Preferencias.AltoContraste),
                ResolverMovimento(Preferencias.NivelMovimento, Preferencias.ReduzirAnimacoes),
                ResolverEnum(Preferencias.FonteLeitura, FonteLeitura.Padrao),
                ResolverEnum(Preferencias.EspacamentoTexto, EspacamentoTexto.Normal),
                ResolverEnum(Preferencias.VerbosidadeLeitor, VerbosidadeLeitor.Normal),
                Preferencias.EscalaAutomatica,
                Preferencias.SublinharLinks,
                Preferencias.FocoReforcado,
                Preferencias.AnunciarAcoes,
                Preferencias.AvisarAntesBloqueio);
            Acessibilidade.Aplicar();
            AcessibilidadeSistema.Alterado += () =>
                Avalonia.Threading.Dispatcher.UIThread.Post(Acessibilidade.ReavaliarSistema);
            if (PlatformSettings != null)
                PlatformSettings.ColorValuesChanged += (s, e) =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        Acessibilidade.ReavaliarTemaDoSistema();
                        AcessibilidadeSistema.Reavaliar();
                    });

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new JanelaGerador();

            base.OnFrameworkInitializationCompleted();
        }

        private static TipoDaltonismo ResolverDaltonismo(string? valor) =>
            Enum.TryParse<TipoDaltonismo>(valor, out var tipo) ? tipo : TipoDaltonismo.Nenhum;

        private static ModoTema ResolverModoTema(string? valor) =>
            Enum.TryParse<ModoTema>(valor, out var modo) ? modo : ModoTema.Escuro;

        private static CorDestaque ResolverCorDestaque(string? valor) =>
            Enum.TryParse<CorDestaque>(valor, out var cor) ? cor : CorDestaque.Ambar;

        private static Densidade ResolverDensidade(string? valor) =>
            Enum.TryParse<Densidade>(valor, out var d) ? d : Densidade.Confortavel;

        private static LayoutDetalhe ResolverLayoutDetalhe(string? valor) =>
            Enum.TryParse<LayoutDetalhe>(valor, out var l) ? l : LayoutDetalhe.Lateral;

        private static T ResolverEnum<T>(string? valor, T padrao) where T : struct, Enum =>
            Enum.TryParse<T>(valor, ignoreCase: true, out var v) ? v : padrao;

        private static NivelContraste ResolverContraste(string? valor, bool legado) =>
            Enum.TryParse<NivelContraste>(valor, ignoreCase: true, out var n)
                ? n
                : (legado ? NivelContraste.Alto : NivelContraste.Padrao);

        private static NivelMovimento ResolverMovimento(string? valor, bool legado) =>
            Enum.TryParse<NivelMovimento>(valor, ignoreCase: true, out var n)
                ? n
                : (legado ? NivelMovimento.Reduzido : NivelMovimento.Completo);
    }
}
