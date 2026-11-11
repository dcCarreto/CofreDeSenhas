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
            Acessibilidade.Hidratar(TipoDaltonismo.Nenhum, ModoTema.Escuro, Densidade.Confortavel,
                LayoutDetalhe.Lateral, false, Acessibilidade.EscalaNormal, false, false);
            Acessibilidade.HidratarExpert(NivelContraste.Padrao, NivelMovimento.Completo, FonteLeitura.Padrao,
                EspacamentoTexto.Normal, VerbosidadeLeitor.Normal, false, false, false, false, false);
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
                Acessibilidade.DefinirLayoutDetalhe(LayoutDetalhe.Inferior);
                Aparencia.Salvar("embaixo");

                Assert.Contains(Aparencia.Salvos, p => p.Nome == "embaixo");
                Assert.Equal("embaixo", Aparencia.IdentificacaoAtual());

                Acessibilidade.DefinirDensidade(Densidade.Compacto);
                Assert.Null(Aparencia.IdentificacaoAtual());

                Aparencia.Aplicar(Aparencia.Salvos.Single(p => p.Nome == "embaixo"));
                Assert.Equal(Densidade.Confortavel, Acessibilidade.Densidade);
                Assert.Equal(LayoutDetalhe.Inferior, Acessibilidade.LayoutDetalhe);

                Aparencia.Excluir("embaixo");
                Assert.DoesNotContain(Aparencia.Salvos, p => p.Nome == "embaixo");
            }
            finally { Reset(); }
        }

        [Fact]
        public void PerfilSalvoAntigo_ComCorDeDestaque_AindaCarrega()
        {
            // Perfis salvos antes da remoção das cores de destaque têm o campo
            // "CorDestaque"; ele deve ser ignorado, não quebrar a leitura.
            var perfil = System.Text.Json.JsonSerializer.Deserialize<PerfilAparencia>(
                "{\"Nome\":\"antigo\",\"CorDestaque\":\"Terracota\",\"Densidade\":\"Compacto\"}");

            Assert.NotNull(perfil);
            Assert.Equal("antigo", perfil!.Nome);
            Assert.Equal("Compacto", perfil.Densidade);
        }

        [Fact]
        public void PerfisAparencia_SobrevivemAoRoundTrip()
        {
            var original = Preferencias.PerfisAparencia;
            try
            {
                Preferencias.PerfisAparencia = new List<PerfilAparencia>
                {
                    new() { Nome = "A", ModoTema = "Claro", Densidade = "Compacto" }
                };
                Preferencias.Salvar();
                Preferencias.PerfisAparencia = null;
                Preferencias.Carregar();

                Assert.Single(Preferencias.PerfisAparencia!);
                Assert.Equal("A", Preferencias.PerfisAparencia![0].Nome);
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
