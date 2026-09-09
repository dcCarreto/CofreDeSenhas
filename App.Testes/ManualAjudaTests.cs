using System.Text.RegularExpressions;
using CofreDeSenhas;

namespace App.Testes
{
    public class ManualAjudaTests
    {
        [Fact]
        public void TodoTopico_TemConteudoCarregado()
        {
            foreach (var (id, _) in ManualAjuda.Topicos)
                Assert.False(string.IsNullOrWhiteSpace(ManualAjuda.Conteudo(id)),
                    $"tópico '{id}' sem conteúdo .md embutido");
        }

        [Fact]
        public void TodoTopico_TemTituloTraduzido()
        {
            foreach (var (_, chave) in ManualAjuda.Topicos)
                Assert.NotEqual(chave, Idioma.Texto(chave));
        }

        [Fact]
        public void LinksInternos_ApontamParaTopicosExistentes()
        {
            var rx = new Regex(@"\[[^\]]+\]\((?<chave>[^)]+)\)");

            foreach (var (id, _) in ManualAjuda.Topicos)
                foreach (Match m in rx.Matches(ManualAjuda.Conteudo(id)))
                {
                    var alvo = m.Groups["chave"].Value;
                    Assert.True(ManualAjuda.TopicoExiste(alvo),
                        $"'{id}.md' tem link para tópico inexistente: '{alvo}'");
                }
        }

        [Fact]
        public void NormalizarId_CaiNoPadraoQuandoInvalido()
        {
            Assert.Equal(ManualAjuda.TopicoPadrao, ManualAjuda.NormalizarId(null));
            Assert.Equal(ManualAjuda.TopicoPadrao, ManualAjuda.NormalizarId("nao-existe"));
            Assert.Equal("faq", ManualAjuda.NormalizarId("faq"));
        }

        [Theory]
        [InlineData("en")]
        [InlineData("es")]
        [InlineData("fr")]
        [InlineData("de")]
        [InlineData("it")]
        public void CadaIdioma_TemConteudoProprioParaTodosOsTopicos(string idioma)
        {
            var pt = new Regex(@"\[[^\]]+\]\((?<chave>[^)]+)\)");

            foreach (var (id, _) in ManualAjuda.Topicos)
            {
                var traduzido = ManualAjuda.Conteudo(id, idioma);
                var portugues = ManualAjuda.Conteudo(id, "pt-BR");

                Assert.False(string.IsNullOrWhiteSpace(traduzido), $"'{id}' sem conteúdo em '{idioma}'");
                Assert.NotEqual(portugues, traduzido);

                var alvosPt = pt.Matches(portugues).Select(m => m.Groups["chave"].Value).OrderBy(x => x);
                var alvosTr = pt.Matches(traduzido).Select(m => m.Groups["chave"].Value).OrderBy(x => x);
                Assert.Equal(alvosPt, alvosTr);
            }
        }

        [Fact]
        public void IdiomaSemManual_CaiNoPortugues()
        {
            Assert.Equal(ManualAjuda.Conteudo("introducao", "pt-BR"), ManualAjuda.Conteudo("introducao", "ja"));
        }
    }
}
