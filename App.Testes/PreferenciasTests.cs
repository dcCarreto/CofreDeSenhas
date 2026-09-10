using System.IO;
using CofreDeSenhas;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class PreferenciasTests
    {
        [Fact]
        public void VerificarAtualizacoes_QuandoOConfigNaoTemAChave_FicaDesligado()
        {
            var caminho = Path.Combine(CaminhosApp.PastaDados, "config.json");
            var arquivoOriginal = File.Exists(caminho) ? File.ReadAllText(caminho) : null;
            try
            {
                Directory.CreateDirectory(CaminhosApp.PastaDados);
                File.WriteAllText(caminho, "{\"MinutosBloqueio\":5}");
                Preferencias.VerificarAtualizacoes = true;

                Preferencias.Carregar();

                Assert.False(Preferencias.VerificarAtualizacoes);
            }
            finally
            {
                // Recarrega a partir do estado original (ou de um "{}" = tudo no
                // padrão, o mesmo efeito de não haver config) para não deixar
                // nenhum estático global de Preferencias sujo para o próximo teste
                // da coleção.
                File.WriteAllText(caminho, arquivoOriginal ?? "{}");
                Preferencias.Carregar();
                if (arquivoOriginal == null)
                    File.Delete(caminho);
            }
        }
    }
}
