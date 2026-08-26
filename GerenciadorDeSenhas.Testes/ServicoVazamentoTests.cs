using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoVazamentoTests
{
    [Fact]
    public void ContarOcorrencias_ComSufixoPresente_RetornaAContagem()
    {
        var resposta = "0018A45C4D1DEF81644B54AB7F969B88D65:1\r\n003D68EB55068C33ACE09247EE4C639306B:42\r\n";

        var contagem = ServicoVazamento.ContarOcorrencias(resposta, "003D68EB55068C33ACE09247EE4C639306B");

        Assert.Equal(42, contagem);
    }

    [Fact]
    public void ContarOcorrencias_ComSufixoAusente_RetornaZero()
    {
        var resposta = "0018A45C4D1DEF81644B54AB7F969B88D65:1\r\n";

        var contagem = ServicoVazamento.ContarOcorrencias(resposta, "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");

        Assert.Equal(0, contagem);
    }

    [Fact]
    public void ContarOcorrencias_ComSufixoEmCaixaDiferente_IgnoraCaixa()
    {
        var resposta = "003d68eb55068c33ace09247ee4c639306b:7\r\n";

        var contagem = ServicoVazamento.ContarOcorrencias(resposta, "003D68EB55068C33ACE09247EE4C639306B");

        Assert.Equal(7, contagem);
    }

    [Fact]
    public void ContarOcorrencias_ComSufixoBatendoMasContagemInvalida_LancaExcecaoEmVezDeReportarSeguro()
    {
        // Sem a correção, uma resposta em formato inesperado pro sufixo que bateu
        // fazia esta checagem de segurança relatar "não vazada" (fail-open) em vez
        // de avisar que não conseguiu confirmar nada.
        var resposta = "003D68EB55068C33ACE09247EE4C639306B:não-é-um-numero\r\n";

        Assert.Throws<FormatException>(() =>
            ServicoVazamento.ContarOcorrencias(resposta, "003D68EB55068C33ACE09247EE4C639306B"));
    }

    [Fact]
    public void ContarOcorrencias_ComRespostaVazia_RetornaZero()
    {
        var contagem = ServicoVazamento.ContarOcorrencias("", "003D68EB55068C33ACE09247EE4C639306B");

        Assert.Equal(0, contagem);
    }
}
