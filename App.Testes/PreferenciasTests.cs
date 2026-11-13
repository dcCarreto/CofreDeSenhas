using System.IO;
using CofreDeSenhas;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class PreferenciasTests
    {
        [Fact]
        public void AtualizacaoAutomatica_ConfigDeAntesDaMudanca_LigaEDeixaOAvisoPendente()
        {
            // Config gravado antes de a atualização passar a ser automática: não tem o
            // campo novo (só o antigo VerificarAtualizacoes, opt-in). Quem já usava
            // passa a atualizar sozinho e recebe o aviso único.
            ComConfig("{\"MinutosBloqueio\":5,\"VerificarAtualizacoes\":false}", () =>
            {
                Assert.True(Preferencias.AtualizacaoAutomatica);
                Assert.True(Preferencias.AvisoAtualizacaoAutomaticaPendente);
            });
        }

        [Fact]
        public void AtualizacaoAutomatica_DesligadaPeloUsuario_ContinuaDesligada()
        {
            ComConfig("{\"AtualizacaoAutomatica\":false,\"AvisoAtualizacaoAutomaticaPendente\":false}", () =>
            {
                Assert.False(Preferencias.AtualizacaoAutomatica);
                Assert.False(Preferencias.AvisoAtualizacaoAutomaticaPendente);
            });
        }

        [Fact]
        public void AtualizacaoAutomatica_AvisoJaVisto_NaoVoltaAFicarPendente()
        {
            ComConfig("{\"AtualizacaoAutomatica\":true,\"AvisoAtualizacaoAutomaticaPendente\":false}", () =>
            {
                Assert.True(Preferencias.AtualizacaoAutomatica);
                Assert.False(Preferencias.AvisoAtualizacaoAutomaticaPendente);
            });
        }

        private static void ComConfig(string json, Action verificar)
        {
            var caminho = Path.Combine(CaminhosApp.PastaDados, "config.json");
            var arquivoOriginal = File.Exists(caminho) ? File.ReadAllText(caminho) : null;
            try
            {
                Directory.CreateDirectory(CaminhosApp.PastaDados);
                File.WriteAllText(caminho, json);

                Preferencias.Carregar();

                verificar();
            }
            finally
            {
                // Recarrega a partir do estado original para não deixar nenhum estático
                // global de Preferencias sujo para o próximo teste da coleção. Sem
                // original, volta os dois campos ao padrão de uma instalação nova.
                if (arquivoOriginal != null)
                {
                    File.WriteAllText(caminho, arquivoOriginal);
                    Preferencias.Carregar();
                }
                else
                {
                    File.WriteAllText(caminho, "{\"AtualizacaoAutomatica\":true,\"AvisoAtualizacaoAutomaticaPendente\":false}");
                    Preferencias.Carregar();
                    File.Delete(caminho);
                }
            }
        }
    }
}
