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
            var original = File.Exists(caminho) ? File.ReadAllText(caminho) : null;
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
                if (original != null)
                    File.WriteAllText(caminho, original);
                else
                    File.Delete(caminho);
                Preferencias.Carregar();
            }
        }
    }
}
