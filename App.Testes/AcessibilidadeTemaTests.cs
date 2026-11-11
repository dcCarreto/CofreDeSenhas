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
            Acessibilidade.Hidratar(TipoDaltonismo.Nenhum, ModoTema.Escuro, Densidade.Confortavel,
                LayoutDetalhe.Lateral, false, Acessibilidade.EscalaNormal, false, false);
            Acessibilidade.HidratarExpert(NivelContraste.Padrao, NivelMovimento.Completo, FonteLeitura.Padrao,
                EspacamentoTexto.Normal, VerbosidadeLeitor.Normal, false, false, false, false, false);
            Acessibilidade.HidratarColunas((int)ColunasLista.Todas);
            Acessibilidade.DestaqueSistemaFixo = Acessibilidade.DestaquePadraoWindows;
        }

        [AvaloniaFact]
        public void ModoEscuro_UsaAPaletaEscura()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Escuro);
                Assert.False(Acessibilidade.TemaClaroEfetivo);
                Assert.Equal(Color.FromUInt32(0xFF202020), Acessibilidade.Cor(CorVisual.WorkspaceBackground));
                Assert.Equal(Color.FromUInt32(0xFFFFFFFF), Acessibilidade.Cor(CorVisual.TextPrimary));
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
                Assert.Equal(Color.FromUInt32(0xFFF3F3F3), Acessibilidade.Cor(CorVisual.WorkspaceBackground));
                Assert.Equal(Color.FromUInt32(0xFF1B1B1B), Acessibilidade.Cor(CorVisual.TextPrimary));
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

                Assert.Equal(Color.FromUInt32(0xFFF3F3F3), Acessibilidade.Cor(CorVisual.WorkspaceBackground));
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
            var (t, d, l, oc, od, cl) = (Preferencias.ModoTema, Preferencias.Densidade,
                Preferencias.LayoutDetalhe, Preferencias.OrdenacaoColuna, Preferencias.OrdenacaoDescendente,
                Preferencias.ColunasLista);
            try
            {
                Preferencias.ModoTema = "Claro";
                Preferencias.Densidade = "Compacto";
                Preferencias.LayoutDetalhe = "Inferior";
                Preferencias.OrdenacaoColuna = "Forca";
                Preferencias.OrdenacaoDescendente = true;
                Preferencias.ColunasLista = (int)ColunasLista.Usuario;
                Preferencias.Salvar();

                Preferencias.ModoTema = Preferencias.Densidade =
                    Preferencias.LayoutDetalhe = Preferencias.OrdenacaoColuna = null;
                Preferencias.OrdenacaoDescendente = false;
                Preferencias.ColunasLista = (int)ColunasLista.Todas;
                Preferencias.Carregar();

                Assert.Equal("Claro", Preferencias.ModoTema);
                Assert.Equal("Compacto", Preferencias.Densidade);
                Assert.Equal("Inferior", Preferencias.LayoutDetalhe);
                Assert.Equal("Forca", Preferencias.OrdenacaoColuna);
                Assert.True(Preferencias.OrdenacaoDescendente);
                Assert.Equal((int)ColunasLista.Usuario, Preferencias.ColunasLista);
            }
            finally
            {
                (Preferencias.ModoTema, Preferencias.Densidade,
                    Preferencias.LayoutDetalhe, Preferencias.OrdenacaoColuna, Preferencias.OrdenacaoDescendente,
                    Preferencias.ColunasLista) = (t, d, l, oc, od, cl);
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

        private static double Luminancia(Color c)
        {
            static double Canal(byte v)
            {
                double x = v / 255.0;
                return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Canal(c.R) + 0.7152 * Canal(c.G) + 0.0722 * Canal(c.B);
        }

        private static double Contraste(Color a, Color b)
        {
            double la = Luminancia(a), lb = Luminancia(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        [AvaloniaFact]
        public void DestaqueDoSistema_NoEscuro_ViraTomClaroComTextoPreto()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Escuro);

                var destaque = Acessibilidade.Cor(CorVisual.AccentPrimary);
                Assert.True(Luminancia(destaque) > Luminancia(Acessibilidade.DestaquePadraoWindows));
                Assert.True(destaque.B > destaque.R, "o tom deve continuar azul");
                Assert.Equal(Colors.Black, Acessibilidade.Cor(CorVisual.TextOnAccent));
                Assert.True(Contraste(destaque, Colors.Black) >= 4.5);
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void DestaqueDoSistema_NoClaro_ViraTomEscuroComTextoBranco()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Claro);

                var destaque = Acessibilidade.Cor(CorVisual.AccentPrimary);
                Assert.Equal(Colors.White, Acessibilidade.Cor(CorVisual.TextOnAccent));
                Assert.True(Contraste(destaque, Colors.White) >= 4.5);
                Assert.True(Contraste(Acessibilidade.Cor(CorVisual.AccentText), Acessibilidade.Cor(CorVisual.CardBackground)) >= 4.5);
            }
            finally { Restaurar(); }
        }

        [AvaloniaTheory]
        [InlineData(0xFFFFB900u)] // amarelo
        [InlineData(0xFFE81123u)] // vermelho
        [InlineData(0xFF767676u)] // cinza
        [InlineData(0xFF00CC6Au)] // verde claro
        [InlineData(0xFF2D0A5Au)] // roxo bem escuro
        public void DestaqueDoSistema_QualquerCor_FechaContrasteNosDoisTemas(uint argb)
        {
            try
            {
                Acessibilidade.DestaqueSistemaFixo = Color.FromUInt32(argb);
                foreach (var modo in new[] { ModoTema.Escuro, ModoTema.Claro })
                {
                    Acessibilidade.DefinirModoTema(modo);
                    var destaque = Acessibilidade.Cor(CorVisual.AccentPrimary);
                    var cartao = Acessibilidade.Cor(CorVisual.CardBackground);

                    Assert.True(Contraste(destaque, Acessibilidade.Cor(CorVisual.TextOnAccent)) >= 4.5, $"{modo}: texto sobre destaque");
                    Assert.True(Contraste(destaque, cartao) >= 3, $"{modo}: destaque sobre cartão");
                    Assert.True(Contraste(Acessibilidade.Cor(CorVisual.AccentText), cartao) >= 4.5, $"{modo}: texto de destaque");
                }
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void DestaqueDoSistema_NaoMudaAsSuperficiesNeutras()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Escuro);
                var fundoAzul = Acessibilidade.Cor(CorVisual.WorkspaceBackground);
                var cartaoAzul = Acessibilidade.Cor(CorVisual.CardBackground);
                var destaqueAzul = Acessibilidade.Cor(CorVisual.AccentPrimary);

                Acessibilidade.DestaqueSistemaFixo = Color.FromUInt32(0xFFE81123);

                Assert.NotEqual(destaqueAzul, Acessibilidade.Cor(CorVisual.AccentPrimary));
                Assert.Equal(fundoAzul, Acessibilidade.Cor(CorVisual.WorkspaceBackground));
                Assert.Equal(cartaoAzul, Acessibilidade.Cor(CorVisual.CardBackground));
            }
            finally { Restaurar(); }
        }

        [AvaloniaFact]
        public void DestaqueDoSistema_IgnoradoQuandoODaltonismoEstaAtivo()
        {
            try
            {
                Acessibilidade.DefinirModoTema(ModoTema.Escuro);
                Acessibilidade.DefinirDaltonismo(TipoDaltonismo.Protanopia);
                Acessibilidade.DestaqueSistemaFixo = Color.FromUInt32(0xFFE81123);

                Assert.Equal(Color.FromUInt32(0xFF56B4E9), Acessibilidade.Cor(CorVisual.AccentPrimary));
                // As superfícies das paletas de daltonismo também ficaram neutras.
                Assert.Equal(Color.FromUInt32(0xFF202020), Acessibilidade.Cor(CorVisual.WorkspaceBackground));
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
