using CofreDeSenhas;
using Xunit;

namespace App.Testes
{
    public class ForcaSenhaTests
    {
        [Fact]
        public void Calcular_SenhaComprimentoMaiusculaMinusculaEDigitoMasSemSimbolo_NaoBateNoTeto()
        {
            var nivel = ForcaSenha.Calcular("Password123456");

            Assert.True(nivel < 4);
        }

        [Fact]
        public void Calcular_MesmaSenhaSemSimbolo_ConcordaComAuditoriaQueAMarcaComoFraca()
        {
            // ServicoAuditoriaSenha.SenhaForteParaAuditoria exige símbolo pra senhas
            // que não são passphrase — o indicador rápido da lista não pode mostrar
            // "Excelente" bem ao lado de uma senha que o Relatório de Segurança marca
            // como fraca por falta desse mesmo símbolo.
            var nivel = ForcaSenha.Calcular("Password123456");

            Assert.True(nivel <= 3);
        }

        [Fact]
        public void Calcular_SenhaComSimboloEComprimentoESeparacaoDeCaso_BateNoTeto()
        {
            var nivel = ForcaSenha.Calcular("Xk9#mQ2vL8!wR5z");

            Assert.Equal(4, nivel);
        }

        [Fact]
        public void Calcular_PassphraseComCincoPalavras_BateNoTetoMesmoSemSimbolo()
        {
            var nivel = ForcaSenha.Calcular("correct horse battery staple zebra");

            Assert.Equal(4, nivel);
        }

        [Fact]
        public void Calcular_SenhaVazia_RetornaZero()
        {
            Assert.Equal(0, ForcaSenha.Calcular(""));
        }
    }
}
