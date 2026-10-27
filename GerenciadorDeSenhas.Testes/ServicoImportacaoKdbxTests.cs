using System.Text;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace GerenciadorDeSenhas.Testes;

public class ServicoImportacaoKdbxTests
{
    private const string SenhaDosArquivos = "Senha-Do-KeePass#1";

    private static string Fixture(string nome) => Path.Combine(AppContext.BaseDirectory, "Fixtures", "kdbx", nome);

    [Fact]
    public void ChaCha20_BateComOVetorDoRfc8439()
    {
        var chave = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var nonce = Convert.FromHexString("000000000000004A00000000");
        var dados = Encoding.ASCII.GetBytes("Ladies and Gentlemen of the class of '99: If I could offer you only one tip for the future, sunscreen would be it.");

        new ChaCha20(chave, nonce, contador: 1).Aplicar(dados);

        Assert.Equal(
            "6e2e359a2568f98041ba0728dd0d6981e97e7aec1d4360c20a27afccfd9fae0bf91b65c5524733ab8f593dabcd62b3571639d624e65152ab8f530c359f0861d807ca0dbf500d6a6156a38e088a22b65e52bc514d16ccf806818ce91ab77937365af90bbf74a35be6b40b8eedf2785e42874d",
            Convert.ToHexString(dados).ToLowerInvariant());
    }

    [Theory]
    [InlineData("kdbx4-padrao.kdbx")]
    [InlineData("kdbx4-chacha20-argon2id.kdbx")]
    [InlineData("kdbx4-aes256-aeskdf.kdbx")]
    public void Importar_ArquivoKdbx4_LeTudoMenosALixeira(string arquivo)
    {
        var itens = new ServicoImportacaoKdbx().ImportarArquivo(Fixture(arquivo), SenhaDosArquivos);

        Assert.Equal(2, itens.Count);
        Assert.DoesNotContain(itens, i => i.NomeServico == "Apagado");

        var banco = itens.Single(i => i.NomeServico == "Banco, Exemplo");
        Assert.Equal("pessoa@exemplo.com", banco.Usuario);
        Assert.Equal("SenhaNova@2", banco.Senha);
        Assert.Equal("https://banco.exemplo.com", banco.Url);
        Assert.StartsWith("linha 1\nlinha 2", banco.Notas);
        Assert.Contains("PIN: 4321", banco.Notas);
        Assert.Contains("Agencia: 0001", banco.Notas);
        Assert.Equal(Categoria.Finance, banco.Categoria);
        Assert.Contains("pessoal", banco.Etiquetas);
        Assert.Contains("viagem", banco.Etiquetas);
        Assert.Contains("Bancos", banco.Etiquetas);
        Assert.Contains("secret=JBSWY3DPEHPK3PXP", banco.TotpSegredo);
        Assert.Equal("Senha\"Com,Aspas<&>1", Assert.Single(banco.Historico).Senha);
        var anexo = Assert.Single(banco.Anexos);
        Assert.Equal("recibo.txt", anexo.NomeArquivo);
        Assert.Equal("conteudo do anexo de teste", Encoding.UTF8.GetString(Convert.FromBase64String(anexo.ConteudoBase64)));

        var email = itens.Single(i => i.NomeServico == "Email");
        Assert.Equal("SenhaDoEmail@3", email.Senha);
        Assert.Empty(email.Etiquetas);
    }

    [Fact]
    public void Importar_SenhaErrada_AvisaSenhaIncorreta()
    {
        var erro = Assert.Throws<ErroLocalizavel>(() =>
            new ServicoImportacaoKdbx().ImportarArquivo(Fixture("kdbx4-padrao.kdbx"), "outra senha"));

        Assert.Equal("Kdbx.Error.WrongPassword", erro.Chave);
    }

    [Fact]
    public void Importar_ArquivoQueNaoEKdbx_AvisaFormatoInvalido()
    {
        var erro = Assert.Throws<ErroLocalizavel>(() =>
            new ServicoImportacaoKdbx().Importar(Encoding.UTF8.GetBytes("name,url,username,password"), SenhaDosArquivos));

        Assert.Equal("Kdbx.Error.NotKdbx", erro.Chave);
    }

    [Fact]
    public void Importar_Kdbx3_PedeParaConverterParaKdbx4()
    {
        var cabecalho = Convert.FromHexString("03D9A29A67FB4BB501000300");

        var erro = Assert.Throws<ErroLocalizavel>(() => new ServicoImportacaoKdbx().Importar(cabecalho, SenhaDosArquivos));

        Assert.Equal("Kdbx.Error.Version3", erro.Chave);
    }

    [Fact]
    public void Importar_ArquivoTruncado_AvisaCorrompido()
    {
        var bytes = File.ReadAllBytes(Fixture("kdbx4-padrao.kdbx"));

        var erro = Assert.Throws<ErroLocalizavel>(() =>
            new ServicoImportacaoKdbx().Importar(bytes.AsSpan(0, bytes.Length - 40).ToArray(), SenhaDosArquivos));

        Assert.Equal("Kdbx.Error.Corrupted", erro.Chave);
    }
}
