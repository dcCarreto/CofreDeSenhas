using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class AutenticacaoMestraTests : IDisposable
{
    private readonly string _pasta;
    private readonly AutenticacaoMestra _auth;

    public AutenticacaoMestraTests()
    {
        _pasta = Path.Combine(Path.GetTempPath(), "GS_Auth_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);
        _auth = new AutenticacaoMestra(_pasta);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, recursive: true); } catch { }
    }

    [Fact]
    public void ExisteSenhaMestra_SemArquivo_RetornaFalse()
    {
        Assert.False(_auth.ExisteSenhaMestra());
    }

    [Fact]
    public void CriarSenhaMestra_ComSenhaValida_RetornaChave256BitsEMarcaExistente()
    {
        var chave = _auth.CriarSenhaMestra("SenhaMestra@123");

        Assert.NotNull(chave);
        Assert.Equal(32, chave.Length);
        Assert.True(_auth.ExisteSenhaMestra());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarSenhaMestra_ComSenhaVazia_LancaExcecao(string senha)
    {
        Assert.Throws<ArgumentException>(() => _auth.CriarSenhaMestra(senha));
    }

    [Fact]
    public void CriarSenhaMestra_ComMenosDe8Caracteres_LancaExcecao()
    {
        Assert.Throws<ArgumentException>(() => _auth.CriarSenhaMestra("Abc@123"));
    }

    [Fact]
    public void Autenticar_ComSenhaCorreta_RetornaMesmaChaveDaCriacao()
    {
        var chaveCriacao = _auth.CriarSenhaMestra("SenhaMestra@123");

        var chaveAutenticacao = _auth.Autenticar("SenhaMestra@123");

        Assert.NotNull(chaveAutenticacao);

        Assert.Equal(chaveCriacao, chaveAutenticacao);
    }

    [Fact]
    public void Autenticar_ComSenhaIncorreta_RetornaNull()
    {
        _auth.CriarSenhaMestra("SenhaMestra@123");

        Assert.Null(_auth.Autenticar("SenhaErrada@999"));
    }

    [Fact]
    public void Autenticar_SemSenhaMestraConfigurada_RetornaNull()
    {
        Assert.Null(_auth.Autenticar("QualquerSenha@123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Autenticar_ComSenhaVazia_RetornaNull(string senha)
    {
        _auth.CriarSenhaMestra("SenhaMestra@123");

        Assert.Null(_auth.Autenticar(senha));
    }

    [Fact]
    public void ArquivoAuth_NaoContemChaveDerivada()
    {
        var chave = _auth.CriarSenhaMestra("SenhaMestra@123");

        var conteudo = Convert.FromBase64String(File.ReadAllText(Path.Combine(_pasta, "auth.dat")));

        Assert.False(ContemSubsequencia(conteudo, chave),
            "auth.dat não deve conter a chave de criptografia.");
    }

    [Fact]
    public void CriarSenhaMestra_MesmaSenhaEmCofresDiferentes_GeraChavesDiferentes()
    {
        var pasta2 = Path.Combine(Path.GetTempPath(), "GS_Auth_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta2);
        try
        {
            var auth2 = new AutenticacaoMestra(pasta2);

            var chave1 = _auth.CriarSenhaMestra("SenhaMestra@123");
            var chave2 = auth2.CriarSenhaMestra("SenhaMestra@123");

            Assert.NotEqual(chave1, chave2);
        }
        finally
        {
            try { Directory.Delete(pasta2, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Autenticar_ComArquivoCorrompido_RetornaNullSemLancar()
    {
        _auth.CriarSenhaMestra("SenhaMestra@123");
        File.WriteAllText(Path.Combine(_pasta, "auth.dat"), "isto-nao-e-base64-valido!!!");

        var excecao = Record.Exception(() => _auth.Autenticar("SenhaMestra@123"));

        Assert.Null(excecao);
        Assert.Null(_auth.Autenticar("SenhaMestra@123"));
    }

    private static bool ContemSubsequencia(byte[] palheiro, byte[] agulha)
    {
        if (agulha.Length == 0 || palheiro.Length < agulha.Length) return false;
        for (int i = 0; i <= palheiro.Length - agulha.Length; i++)
        {
            bool igual = true;
            for (int j = 0; j < agulha.Length; j++)
            {
                if (palheiro[i + j] != agulha[j]) { igual = false; break; }
            }
            if (igual) return true;
        }
        return false;
    }
}
