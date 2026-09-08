using Avalonia.Headless.XUnit;
using CofreDeSenhas;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class AparenciaTests
    {
        public AparenciaTests() => Reset();

        private static void Reset()
        {
            Acessibilidade.Hidratar(TipoDaltonismo.Nenhum, ModoTema.Escuro, CorDestaque.Ambar, Densidade.Confortavel,
                LayoutDetalhe.Lateral, false, Acessibilidade.EscalaNormal, false, false);
            Acessibilidade.HidratarColunas((int)ColunasLista.Todas);
            Preferencias.PerfisAparencia = null;
            Preferencias.ColunasLista = (int)ColunasLista.Todas;
            Preferencias.Salvar();
        }

        [AvaloniaFact]
        public void AplicarPreset_ClaroCompacto_MudaOsEixosEIdentifica()
        {
            try
            {
                var preset = Aparencia.Presets.Single(p => p.Id == "ClaroCompacto").Perfil;
                Aparencia.Aplicar(preset);

                Assert.Equal(ModoTema.Claro, Acessibilidade.Modo);
                Assert.Equal(Densidade.Compacto, Acessibilidade.Densidade);
                Assert.Equal("ClaroCompacto", Aparencia.IdentificacaoAtual());
                Assert.Equal("Claro", Preferencias.ModoTema);
            }
            finally { Reset(); }
        }

        [AvaloniaFact]
        public void AplicarPreset_AltoContraste_LigaContrasteEEscala()
        {
            try
            {
                Aparencia.Aplicar(Aparencia.Presets.Single(p => p.Id == "AltoContraste").Perfil);

                Assert.True(Acessibilidade.AltoContraste);
                Assert.Equal(Acessibilidade.EscalaGrande, Acessibilidade.Escala);
                Assert.Equal("AltoContraste", Aparencia.IdentificacaoAtual());
            }
            finally { Reset(); }
        }

        [AvaloniaFact]
        public void SalvarEExcluir_PerfilDoUsuario()
        {
            try
            {
                Acessibilidade.DefinirCorDestaque(CorDestaque.Verde);
                Aparencia.Salvar("meu verde");

                Assert.Contains(Aparencia.Salvos, p => p.Nome == "meu verde");
                Assert.Equal("meu verde", Aparencia.IdentificacaoAtual());

                Acessibilidade.DefinirCorDestaque(CorDestaque.Terracota);
                Assert.Null(Aparencia.IdentificacaoAtual());

                Aparencia.Aplicar(Aparencia.Salvos.Single(p => p.Nome == "meu verde"));
                Assert.Equal(CorDestaque.Verde, Acessibilidade.Destaque);

                Aparencia.Excluir("meu verde");
                Assert.DoesNotContain(Aparencia.Salvos, p => p.Nome == "meu verde");
            }
            finally { Reset(); }
        }

        [Fact]
        public void PerfisAparencia_SobrevivemAoRoundTrip()
        {
            var original = Preferencias.PerfisAparencia;
            try
            {
                Preferencias.PerfisAparencia = new List<PerfilAparencia>
                {
                    new() { Nome = "A", ModoTema = "Claro", CorDestaque = "Azul", Densidade = "Compacto" }
                };
                Preferencias.Salvar();
                Preferencias.PerfisAparencia = null;
                Preferencias.Carregar();

                Assert.Single(Preferencias.PerfisAparencia!);
                Assert.Equal("A", Preferencias.PerfisAparencia![0].Nome);
                Assert.Equal("Azul", Preferencias.PerfisAparencia![0].CorDestaque);
                Assert.Equal("Compacto", Preferencias.PerfisAparencia![0].Densidade);
            }
            finally
            {
                Preferencias.PerfisAparencia = original;
                Preferencias.Salvar();
            }
        }
    }
}
