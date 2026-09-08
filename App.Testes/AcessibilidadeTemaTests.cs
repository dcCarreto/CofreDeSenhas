using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using CofreDeSenhas;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class AcessibilidadeTemaTests
    {
        public AcessibilidadeTemaTests() => Restaurar();

        private static void Restaurar()
        {
            Acessibilidade.Hidratar(TipoDaltonismo.Nenhum, ModoTema.Escuro, CorDestaque.Ambar, Densidade.Confortavel,
                LayoutDetalhe.Lateral, false, Acessibilidade.EscalaNormal, false, false);
            Acessibilidade.HidratarColunas((int)ColunasLista.Todas);
        }

        [AvaloniaFact]
        public void ModoEscuro_UsaAPaletaEscura()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Escuro);
                Assert.False(Acessibilidade.TemaClaroEfetivo);
                Assert.Equal(Color.FromUInt32(0xFF17130F), Acessibilidade.Cor(CorVisual.WorkspaceBackground));
                Assert.Equal(Color.FromUInt32(0xFFF3EADC), Acessibilidade.Cor(CorVisual.TextPrimary));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void ModoClaro_UsaAPaletaClara()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Claro);
                Assert.True(Acessibilidade.TemaClaroEfetivo);
                Assert.Equal(Color.FromUInt32(0xFFF7F4EF), Acessibilidade.Cor(CorVisual.WorkspaceBackground));
                Assert.Equal(Color.FromUInt32(0xFF2A2317), Acessibilidade.Cor(CorVisual.TextPrimary));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void ModoClaroComDaltonismo_TrocaAccentMasMantemFundoClaro()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Claro);
                Acessibilidade.DefinirDaltonismo(TipoDaltonismo.Protanopia);

                Assert.Equal(Color.FromUInt32(0xFFF7F4EF), Acessibilidade.Cor(CorVisual.WorkspaceBackground));
                Assert.Equal(Color.FromUInt32(0xFF006398), Acessibilidade.Cor(CorVisual.AccentPrimary));
                Assert.NotEqual(Acessibilidade.Cor(CorVisual.StrengthStrong), Acessibilidade.Cor(CorVisual.StrengthWeak));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void Aplicar_DefineOThemeVariantEOsBrushes()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Claro);
                Acessibilidade.Aplicar();
                Assert.Equal(ThemeVariant.Light, Application.Current!.RequestedThemeVariant);
                Assert.IsType<SolidColorBrush>(Application.Current!.Resources["CardBackground"]);

                Acessibilidade.DefinirModoTema(ModoTema.Escuro);
                Acessibilidade.Aplicar();
                Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void SelecionarModoTema_SalvaNaPreferencia()
        {
            var original = Preferencias.ModoTema;
            try
            {
                Acessibilidade.SelecionarModoTema("Claro");
                Assert.Equal(ModoTema.Claro, Acessibilidade.Modo);
                Assert.Equal("Claro", Preferencias.ModoTema);
            }
            finally
            {
                Restaurar();
                Preferencias.ModoTema = original;
                Preferencias.Salvar();
            }
        }

        [Fact]
        public void Preferencias_AparenciaCompleta_SobreviveAoRoundTrip()
        {
            var (t, a, d, l, oc, od, cl) = (Preferencias.ModoTema, Preferencias.CorDestaque, Preferencias.Densidade,
                Preferencias.LayoutDetalhe, Preferencias.OrdenacaoColuna, Preferencias.OrdenacaoDescendente,
                Preferencias.ColunasLista);
            try
            {
                Preferencias.ModoTema = "Claro";
                Preferencias.CorDestaque = "Azul";
                Preferencias.Densidade = "Compacto";
                Preferencias.LayoutDetalhe = "Inferior";
                Preferencias.OrdenacaoColuna = "Forca";
                Preferencias.OrdenacaoDescendente = true;
                Preferencias.ColunasLista = (int)ColunasLista.Usuario;
                Preferencias.Salvar();

                Preferencias.ModoTema = Preferencias.CorDestaque = Preferencias.Densidade =
                    Preferencias.LayoutDetalhe = Preferencias.OrdenacaoColuna = null;
                Preferencias.OrdenacaoDescendente = false;
                Preferencias.ColunasLista = (int)ColunasLista.Todas;
                Preferencias.Carregar();

                Assert.Equal("Claro", Preferencias.ModoTema);
                Assert.Equal("Azul", Preferencias.CorDestaque);
                Assert.Equal("Compacto", Preferencias.Densidade);
                Assert.Equal("Inferior", Preferencias.LayoutDetalhe);
                Assert.Equal("Forca", Preferencias.OrdenacaoColuna);
                Assert.True(Preferencias.OrdenacaoDescendente);
                Assert.Equal((int)ColunasLista.Usuario, Preferencias.ColunasLista);
            }
            finally
            {
                (Preferencias.ModoTema, Preferencias.CorDestaque, Preferencias.Densidade,
                    Preferencias.LayoutDetalhe, Preferencias.OrdenacaoColuna, Preferencias.OrdenacaoDescendente,
                    Preferencias.ColunasLista) = (t, a, d, l, oc, od, cl);
                Preferencias.Salvar();
            }
        }

        [AvaloniaFact]
        public void SelecionarColunaLista_LigaEDesligaAsFlags()
        {
            try
            {
                Assert.True(Acessibilidade.ColunasLista.HasFlag(ColunasLista.Categoria));

                Acessibilidade.SelecionarColunaLista(ColunasLista.Categoria, false);
                Assert.False(Acessibilidade.ColunasLista.HasFlag(ColunasLista.Categoria));
                Assert.True(Acessibilidade.ColunasLista.HasFlag(ColunasLista.Usuario));
                Assert.Equal((int)Acessibilidade.ColunasLista, Preferencias.ColunasLista);

                Acessibilidade.SelecionarColunaLista(ColunasLista.Categoria, true);
                Assert.True(Acessibilidade.ColunasLista.HasFlag(ColunasLista.Categoria));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void CorDestaque_TrocaOAccentNosDoisModos_SemMexerNoFundo()
        {
            try
            {
                Acessibilidade.DefinirCorDestaque(CorDestaque.Azul);

                Acessibilidade.DefinirModoTema(ModoTema.Escuro);
                Assert.Equal(Color.FromUInt32(0xFF5B9BD5), Acessibilidade.Cor(CorVisual.AccentPrimary));
                Assert.Equal(Color.FromUInt32(0xFF17130F), Acessibilidade.Cor(CorVisual.WorkspaceBackground));

                Acessibilidade.DefinirModoTema(ModoTema.Claro);
                Assert.Equal(Color.FromUInt32(0xFF2F6FB0), Acessibilidade.Cor(CorVisual.AccentPrimary));
                Assert.Equal(Color.FromUInt32(0xFFF7F4EF), Acessibilidade.Cor(CorVisual.WorkspaceBackground));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void CorDestaque_IgnoradaQuandoODaltonismoEstaAtivo()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Escuro);
                Acessibilidade.DefinirDaltonismo(TipoDaltonismo.Protanopia);
                Acessibilidade.DefinirCorDestaque(CorDestaque.Azul);

                Assert.False(Acessibilidade.DestaqueDisponivel);
                Assert.Equal(Color.FromUInt32(0xFF56B4E9), Acessibilidade.Cor(CorVisual.AccentPrimary));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void Densidade_AjustaAlturaDaLinhaEOsRecursos()
        {
            try
            {
                Acessibilidade.DefinirDensidade(Densidade.Confortavel);
                Assert.Equal(52, Acessibilidade.AlturaLinhaLista);

                Acessibilidade.DefinirDensidade(Densidade.Compacto);
                Acessibilidade.Aplicar();
                Assert.Equal(46, Acessibilidade.AlturaLinhaLista);
                Assert.Equal(38.0, Application.Current!.Resources["AlturaCampo"]);
            }
            finally { Restaurar(); }
        }
    }
}
