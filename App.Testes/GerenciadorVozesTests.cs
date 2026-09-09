using CofreDeSenhas;

namespace App.Testes
{
    public class GerenciadorVozesTests
    {
        [Theory]
        [InlineData("pt-BR", "pt")]
        [InlineData("en", "en")]
        [InlineData("es", "es")]
        [InlineData("fr", "fr")]
        [InlineData("de", "de")]
        [InlineData("it", "it")]
        public void Catalogo_TemVozParaCadaIdiomaDaInterface(string codigo, string curto)
        {
            Assert.Equal(curto, GerenciadorVozes.Curto(codigo));
            Assert.True(GerenciadorVozes.Suportado(codigo));

            var voz = GerenciadorVozes.Voz(codigo);
            Assert.NotNull(voz);
            Assert.StartsWith("vits-piper-", voz!.Arquivo);
            Assert.True(GerenciadorVozes.TamanhoMb(codigo) > 0);
        }

        [Fact]
        public void Instalada_FalsoQuandoOsArquivosNaoExistem()
        {
            Assert.False(GerenciadorVozes.Instalada("pt-BR"));
            Assert.False(GerenciadorVozes.Suportado("ja"));
            Assert.False(GerenciadorVozes.Instalada("ja"));
        }

        [Fact]
        public void Caminhos_FicamSobOPerfilDeDados()
        {
            var modelo = GerenciadorVozes.CaminhoModelo("pt-BR");
            Assert.EndsWith(Path.Combine("vozes", "pt", "model.onnx"), modelo);
            Assert.EndsWith(Path.Combine("vozes", "pt", "tokens.txt"), GerenciadorVozes.CaminhoTokens("pt-BR"));
            Assert.EndsWith(Path.Combine("vozes", "espeak-ng-data"), GerenciadorVozes.PastaEspeak);
        }
    }
}
