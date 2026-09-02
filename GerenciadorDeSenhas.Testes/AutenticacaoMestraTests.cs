using System.Security.Cryptography;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class AutenticacaoMestraTests : IDisposable
{
    private readonly string _pasta;
    private readonly AutenticacaoMestra _auth;

    public AutenticacaoMestraTests()
    {
        _pasta = PastaTemporariaTeste.Criar("GS_Auth");
        _auth = new AutenticacaoMestra(_pasta);
    }

    public void Dispose() => PastaTemporariaTeste.Apagar(_pasta);

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

    [Fact]
    public void ExcluirSenhaMestra_ComSenhaCriada_RemoveArquivoEDeixaDeExistir()
    {
        _auth.CriarSenhaMestra("SenhaMestra@123");

        _auth.ExcluirSenhaMestra();

        Assert.False(_auth.ExisteSenhaMestra());
    }

    [Fact]
    public void ExcluirSenhaMestra_SemSenhaCriada_NaoLancaExcecao()
    {
        _auth.ExcluirSenhaMestra();

        Assert.False(_auth.ExisteSenhaMestra());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarSenhaMestra_ComSenhaVazia_LancaExcecao(string senha)
    {
        Assert.Throws<ErroLocalizavel>(() => _auth.CriarSenhaMestra(senha));
    }

    [Fact]
    public void CriarSenhaMestra_ComMenosDe8Caracteres_LancaExcecao()
    {
        Assert.Throws<ErroLocalizavel>(() => _auth.CriarSenhaMestra("Abc@123"));
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
    public void ValidarChave_ComChaveCorreta_RetornaTrue()
    {
        var chave = _auth.CriarSenhaMestra("SenhaMestra@123");

        Assert.True(_auth.ValidarChave(chave));
    }

    [Fact]
    public void ValidarChave_ComChaveIncorreta_RetornaFalse()
    {
        _auth.CriarSenhaMestra("SenhaMestra@123");
        var chaveIncorreta = new byte[32];
        chaveIncorreta[0] = 1;

        Assert.False(_auth.ValidarChave(chaveIncorreta));
    }

    [Fact]
    public void ValidarChave_ComArquivoCorrompido_RetornaFalseSemLancar()
    {
        var chave = _auth.CriarSenhaMestra("SenhaMestra@123");
        File.WriteAllText(Path.Combine(_pasta, "auth.dat"), "isto-nao-e-base64-valido!!!");

        var excecao = Record.Exception(() => _auth.ValidarChave(chave));

        Assert.Null(excecao);
        Assert.False(_auth.ValidarChave(chave));
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
        var pasta2 = PastaTemporariaTeste.Criar("GS_Auth");
        try
        {
            var auth2 = new AutenticacaoMestra(pasta2);

            var chave1 = _auth.CriarSenhaMestra("SenhaMestra@123");
            var chave2 = auth2.CriarSenhaMestra("SenhaMestra@123");

            Assert.NotEqual(chave1, chave2);
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta2);
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

    [Fact]
    public void PastaApp_RetornaPastaConfigurada()
    {
        Assert.Equal(_pasta, _auth.PastaApp);
    }

    [Fact]
    public void CriarSenhaMestra_GravaComArgon2id_NaoPrecisaMigrar()
    {
        _auth.CriarSenhaMestra("SenhaMestra@123");

        Assert.False(_auth.KdfDesatualizado());
    }

    [Fact]
    public void KdfDesatualizado_SemArquivo_RetornaFalse()
    {
        Assert.False(_auth.KdfDesatualizado());
    }

    [Fact]
    public void KdfDesatualizado_ComArquivoNoFormatoAntigoSemContagem_RetornaTrue()
    {
        EscreverAuthFormatoAntigo(_pasta, "SenhaMestra@123", 100_000);

        Assert.True(_auth.KdfDesatualizado());
    }

    [Fact]
    public void Autenticar_ComArquivoNoFormatoAntigoSemContagem_UsaIteracoesLegadasEFunciona()
    {
        EscreverAuthFormatoAntigo(_pasta, "SenhaMestra@123", 100_000);

        var chave = _auth.Autenticar("SenhaMestra@123");

        Assert.NotNull(chave);
        Assert.Equal(32, chave!.Length);
    }

    [Fact]
    public void KdfDesatualizado_ComArquivoPbkdf2ComContagem_RetornaTrue()
    {
        EscreverAuthFormatoPbkdf2ComContagem(_pasta, "SenhaMestra@123", 600_000);

        Assert.True(_auth.KdfDesatualizado());
    }

    [Fact]
    public void Autenticar_ComArquivoNoFormatoArgon2id_Funciona()
    {
        EscreverAuthFormatoArgon2id(_pasta, "SenhaMestra@123", 3, 65536, 1);

        var chave = _auth.Autenticar("SenhaMestra@123");

        Assert.NotNull(chave);
        Assert.Equal(32, chave!.Length);
    }

    [Fact]
    public void KdfDesatualizado_ComArquivoNoFormatoArgon2id_RetornaFalse()
    {
        EscreverAuthFormatoArgon2id(_pasta, "SenhaMestra@123", 3, 65536, 1);

        Assert.False(_auth.KdfDesatualizado());
    }

    [Fact]
    public void Autenticar_ComArquivoPedindoMemoriaAbsurda_RetornaNullRapidoSemLancar()
    {
        var dados = new byte[16 + 32 + sizeof(int) + 1 + sizeof(int) + sizeof(int)];
        var offset = 16 + 32;
        BitConverter.GetBytes(3).CopyTo(dados, offset);
        offset += sizeof(int);
        dados[offset] = 1;
        offset += 1;
        BitConverter.GetBytes(int.MaxValue).CopyTo(dados, offset);
        offset += sizeof(int);
        BitConverter.GetBytes(1).CopyTo(dados, offset);
        File.WriteAllText(Path.Combine(_pasta, "auth.dat"), Convert.ToBase64String(dados));

        var relogio = System.Diagnostics.Stopwatch.StartNew();
        var excecao = Record.Exception(() => _auth.Autenticar("SenhaMestra@123"));
        relogio.Stop();

        Assert.Null(excecao);
        Assert.Null(_auth.Autenticar("SenhaMestra@123"));
        Assert.True(relogio.Elapsed < TimeSpan.FromSeconds(5), $"demorou {relogio.Elapsed}");
    }

    [Fact]
    public void TentarLerParametros_SemArquivo_RetornaFalse()
    {
        var achou = _auth.TentarLerParametros(out _, out _, out _, out _, out _, out _);

        Assert.False(achou);
    }

    [Fact]
    public void TentarLerParametros_ComSenhaCriada_DevolveVerificadorQueBateComAChave()
    {
        var chave = _auth.CriarSenhaMestra("SenhaMestra@123");

        var achou = _auth.TentarLerParametros(out _, out var verificador, out var kdf, out _, out _, out _);

        Assert.True(achou);
        Assert.Equal((byte)1, kdf);
        Assert.Equal(SHA256.HashData(chave), verificador);
    }

    [Fact]
    public void DerivarChaveDeParametros_ComParametrosPublicadosPorEsteDispositivo_ReproduzAMesmaChave()
    {
        var chave = _auth.CriarSenhaMestra("SenhaMestra@123");
        _auth.TentarLerParametros(out var salt, out _, out var kdf, out var custo, out var memoriaKb, out var paralelismo);

        var chaveReproduzida = AutenticacaoMestra.DerivarChaveDeParametros("SenhaMestra@123", salt, kdf, custo, memoriaKb, paralelismo);

        Assert.Equal(chave, chaveReproduzida);
    }

    [Fact]
    public void GravarAutenticacaoRestaurada_EmDispositivoQuePerdeuOCofre_PermiteAutenticarDeNovoComAMesmaSenha()
    {
        // Simula o cenário de restauração: um dispositivo "original" cria a senha mestra
        // e publicaria esses parâmetros no banco; um dispositivo "novo" (sem auth.dat
        // nenhum) recebe esses mesmos parâmetros do banco e os grava localmente.
        var original = new AutenticacaoMestra(_pasta);
        var chaveOriginal = original.CriarSenhaMestra("SenhaMestra@123");
        original.TentarLerParametros(out var salt, out var verificador, out var kdf, out var custo, out var memoriaKb, out var paralelismo);

        var pastaNova = PastaTemporariaTeste.Criar("GS_Auth");
        try
        {
            var dispositivoNovo = new AutenticacaoMestra(pastaNova);
            Assert.False(dispositivoNovo.ExisteSenhaMestra());

            dispositivoNovo.GravarAutenticacaoRestaurada(salt, verificador, kdf, custo, memoriaKb, paralelismo);

            var chaveRestaurada = dispositivoNovo.Autenticar("SenhaMestra@123");
            Assert.NotNull(chaveRestaurada);
            Assert.Equal(chaveOriginal, chaveRestaurada);
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pastaNova);
        }
    }

    [Fact]
    public void GravarAutenticacaoRestaurada_ComSenhaErrada_NaoAutentica()
    {
        _auth.CriarSenhaMestra("SenhaMestra@123");
        _auth.TentarLerParametros(out var salt, out var verificador, out var kdf, out var custo, out var memoriaKb, out var paralelismo);

        var pastaNova = PastaTemporariaTeste.Criar("GS_Auth");
        try
        {
            var dispositivoNovo = new AutenticacaoMestra(pastaNova);
            dispositivoNovo.GravarAutenticacaoRestaurada(salt, verificador, kdf, custo, memoriaKb, paralelismo);

            Assert.Null(dispositivoNovo.Autenticar("SenhaErrada@999"));
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pastaNova);
        }
    }

    private static void EscreverAuthFormatoAntigo(string pasta, string senha, int iteracoesLegado)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var chave = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoesLegado, HashAlgorithmName.SHA256, 32);
        var verificador = SHA256.HashData(chave);

        var dados = new byte[16 + 32];
        Buffer.BlockCopy(salt, 0, dados, 0, 16);
        Buffer.BlockCopy(verificador, 0, dados, 16, 32);

        File.WriteAllText(Path.Combine(pasta, "auth.dat"), Convert.ToBase64String(dados));
    }

    private static void EscreverAuthFormatoPbkdf2ComContagem(string pasta, string senha, int iteracoes)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var chave = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, 32);
        var verificador = SHA256.HashData(chave);

        var dados = new byte[16 + 32 + sizeof(int)];
        Buffer.BlockCopy(salt, 0, dados, 0, 16);
        Buffer.BlockCopy(verificador, 0, dados, 16, 32);
        BitConverter.GetBytes(iteracoes).CopyTo(dados, 48);

        File.WriteAllText(Path.Combine(pasta, "auth.dat"), Convert.ToBase64String(dados));
    }

    private static void EscreverAuthFormatoArgon2id(string pasta, string senha, int tempoCusto, int memoriaKb, int paralelismo)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var argon2 = new Konscious.Security.Cryptography.Argon2id(System.Text.Encoding.UTF8.GetBytes(senha))
        {
            Salt = salt,
            DegreeOfParallelism = paralelismo,
            Iterations = tempoCusto,
            MemorySize = memoriaKb
        };
        var chave = argon2.GetBytes(32);
        var verificador = SHA256.HashData(chave);

        var dados = new byte[16 + 32 + sizeof(int) + 1 + sizeof(int) + sizeof(int)];
        var offset = 0;
        Buffer.BlockCopy(salt, 0, dados, offset, 16);
        offset += 16;
        Buffer.BlockCopy(verificador, 0, dados, offset, 32);
        offset += 32;
        BitConverter.GetBytes(tempoCusto).CopyTo(dados, offset);
        offset += sizeof(int);
        dados[offset] = 1;
        offset += 1;
        BitConverter.GetBytes(memoriaKb).CopyTo(dados, offset);
        offset += sizeof(int);
        BitConverter.GetBytes(paralelismo).CopyTo(dados, offset);

        File.WriteAllText(Path.Combine(pasta, "auth.dat"), Convert.ToBase64String(dados));
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
