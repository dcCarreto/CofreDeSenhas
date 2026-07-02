using System.Text.RegularExpressions;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ServicoGeracaoSenhaTests
{
    private readonly ServicoGeracaoSenha _servico = new();

    [Fact]
    public void GerarSenha_ComApenasMinusculas_RespeitaTamanhoETipos()
    {
        var senha = _servico.GerarSenha(
            tamanho: 32,
            incluirMaiusculas: false,
            incluirMinusculas: true,
            incluirNumeros: false,
            incluirEspeciais: false);

        Assert.Equal(32, senha.Length);
        Assert.Matches("^[a-z]+$", senha);
    }

    [Fact]
    public void GerarSenhas_ComQuantidadeValida_RetornaListaNoTamanhoSolicitado()
    {
        var senhas = _servico.GerarSenhas(
            quantidade: 3,
            tamanho: 16,
            incluirMaiusculas: true,
            incluirMinusculas: true,
            incluirNumeros: true,
            incluirEspeciais: false);

        Assert.Equal(3, senhas.Count);
        Assert.All(senhas, senha => Assert.Equal(16, senha.Length));
    }

    [Fact]
    public void GerarSenha_SemTiposSelecionados_LancaExcecao()
    {
        Assert.Throws<ArgumentException>(() => _servico.GerarSenha(
            tamanho: 12,
            incluirMaiusculas: false,
            incluirMinusculas: false,
            incluirNumeros: false,
            incluirEspeciais: false));
    }

    [Fact]
    public void GerarFraseSenha_ComListaPersonalizada_UsaPalavrasDaListaESeparador()
    {
        var palavras = new[] { "casa", "rio", "sol" };

        var frase = _servico.GerarFraseSenha(
            palavras,
            quantidadePalavras: 4,
            separador: "-",
            capitalizar: false,
            incluirNumero: false);

        var partes = frase.Split('-');
        Assert.Equal(4, partes.Length);
        Assert.All(partes, parte => Assert.Contains(parte, palavras));
    }

    [Fact]
    public void GerarFraseSenha_ComCapitalizacaoENumero_FormataPartes()
    {
        var palavras = new[] { "casa", "rio", "sol" };

        var frase = _servico.GerarFraseSenha(
            palavras,
            quantidadePalavras: 3,
            separador: "_",
            capitalizar: true,
            incluirNumero: true);

        var partes = frase.Split('_');
        Assert.Equal(4, partes.Length);
        Assert.All(partes.Take(3), parte => Assert.True(char.IsUpper(parte[0])));
        Assert.Matches(new Regex("^\\d{2}$"), partes[^1]);
    }

    [Fact]
    public void GerarFrasesSenha_ComQuantidadeValida_RetornaListaNoTamanhoSolicitado()
    {
        var frases = _servico.GerarFrasesSenha(
            quantidade: 4,
            quantidadePalavras: 5,
            separador: "-",
            capitalizar: false,
            incluirNumero: true);

        Assert.Equal(4, frases.Count);
        Assert.All(frases, frase => Assert.Contains("-", frase));
    }

    [Fact]
    public void GerarFraseSenha_ComQuantidadePalavrasInvalida_LancaExcecao()
    {
        Assert.Throws<ArgumentException>(() => _servico.GerarFraseSenha(quantidadePalavras: 2));
        Assert.Throws<ArgumentException>(() => _servico.GerarFraseSenha(quantidadePalavras: 13));
    }

    [Fact]
    public void GerarFraseSenha_ComListaInsuficiente_LancaExcecao()
    {
        Assert.Throws<ArgumentException>(() => _servico.GerarFraseSenha(
            new[] { "casa", "" },
            quantidadePalavras: 3));
    }
}
