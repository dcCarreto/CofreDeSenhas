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
                Assert.True(Preferencias.AvisoAtualizacaoAutomaticaPendente);
            });
        }

        [Fact]
        public void AtualizacaoAutomatica_DesligadaQuandoAindaHaviaAOpcao_DeixaOAvisoPendente()
        {
            // A opção de desligar foi removida: quem a tinha desligado passa a
            // atualizar sozinho e, como quem nunca teve, recebe o aviso único.
            ComConfig("{\"AtualizacaoAutomatica\":false,\"AvisoAtualizacaoAutomaticaPendente\":false}", () =>
            {
                Assert.True(Preferencias.AvisoAtualizacaoAutomaticaPendente);
            });
        }

        [Fact]
        public void AtualizacaoAutomatica_AvisoJaVisto_NaoVoltaAFicarPendente()
        {
            ComConfig("{\"AtualizacaoAutomatica\":true,\"AvisoAtualizacaoAutomaticaPendente\":false}", () =>
            {
                Assert.False(Preferencias.AvisoAtualizacaoAutomaticaPendente);

                // Salvar grava o marcador ligado, para o aviso não voltar no próximo Carregar.
                Preferencias.Salvar();
                Preferencias.Carregar();
                Assert.False(Preferencias.AvisoAtualizacaoAutomaticaPendente);
            });
        }

        [Fact]
        public void GeradorLoginRecolhidoEOpcoesDoGerador_SobrevivemASalvarECarregar()
        {
            ComConfig("{}", () =>
            {
                Assert.False(Preferencias.GeradorLoginRecolhido);
                Assert.Null(Preferencias.Gerador);

                Preferencias.GeradorLoginRecolhido = true;
                Preferencias.Gerador = new OpcoesGerador
                {
                    Modo = 1, Quantidade = 3, Tamanho = 32,
                    Maiusculas = false, Minusculas = true, Numeros = false, Especiais = true,
                    Palavras = 7, Separador = 2, CapitalizarFrase = true, NumeroFrase = false
                };
                Preferencias.Salvar();

                Preferencias.GeradorLoginRecolhido = false;
                Preferencias.Gerador = null;
                Preferencias.Carregar();

                Assert.True(Preferencias.GeradorLoginRecolhido);
                var o = Assert.IsType<OpcoesGerador>(Preferencias.Gerador);
                Assert.Equal((1, 3, 32, 7, 2), (o.Modo, o.Quantidade, o.Tamanho, o.Palavras, o.Separador));
                Assert.Equal((false, true, false, true), (o.Maiusculas, o.Minusculas, o.Numeros, o.Especiais));
                Assert.Equal((true, false), (o.CapitalizarFrase, o.NumeroFrase));
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
