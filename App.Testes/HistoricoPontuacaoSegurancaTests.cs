using CofreDeSenhas;
using Xunit;

namespace App.Testes
{
    public class HistoricoPontuacaoSegurancaTests
    {
        [Fact]
        public void Limpar_ComHistoricoExistente_ApagaOArquivo()
        {
            var caminhoOriginal = HistoricoPontuacaoSeguranca.CaminhoOverride;
            try
            {
                HistoricoPontuacaoSeguranca.CaminhoOverride =
                    Path.Combine(TesteUtil.CriarPastaTemporaria(), "pontuacao-historico.json");

                HistoricoPontuacaoSeguranca.RegistrarPontuacao(70);
                Assert.True(File.Exists(HistoricoPontuacaoSeguranca.CaminhoOverride));

                // Excluir cofre promete apagar todo rastro do cofre — sem chamar Limpar
                // ali, este histórico (datas + pontuação ao longo do tempo) sobrevivia
                // sozinho em texto puro depois do usuário já ter decidido apagar tudo.
                HistoricoPontuacaoSeguranca.Limpar();

                Assert.False(File.Exists(HistoricoPontuacaoSeguranca.CaminhoOverride));
                Assert.Empty(HistoricoPontuacaoSeguranca.Carregar());
            }
            finally
            {
                HistoricoPontuacaoSeguranca.CaminhoOverride = caminhoOriginal;
            }
        }

        [Fact]
        public void Limpar_SemHistoricoExistente_NaoLancaExcecao()
        {
            var caminhoOriginal = HistoricoPontuacaoSeguranca.CaminhoOverride;
            try
            {
                HistoricoPontuacaoSeguranca.CaminhoOverride =
                    Path.Combine(TesteUtil.CriarPastaTemporaria(), "pontuacao-historico.json");

                HistoricoPontuacaoSeguranca.Limpar();
            }
            finally
            {
                HistoricoPontuacaoSeguranca.CaminhoOverride = caminhoOriginal;
            }
        }
    }
}
