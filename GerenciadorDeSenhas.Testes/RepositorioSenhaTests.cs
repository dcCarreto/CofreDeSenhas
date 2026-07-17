using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class RepositorioSenhaTests : IDisposable
{
    private readonly byte[] _chave;
    private readonly IServicoCriptografia _criptografia;
    private readonly IPersistenciaLocal _persistencia;
    private readonly IRepositorioSenha _repositorio;
    private readonly string _pastaTemp;

    public RepositorioSenhaTests()
    {
        _chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(_chave);

        _pastaTemp = Path.Combine(Path.GetTempPath(), "GS_Repo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pastaTemp);

        _criptografia = new ServicoCriptografia(_chave);
        _persistencia = new PersistenciaLocal(_criptografia, _pastaTemp);
        _repositorio = new RepositorioSenha(_persistencia, _chave);
    }

    [Fact]
    public async Task AdicionarAsync_ComSenhaValida_AdicionaAoRepositorio()
    {
        var senha = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Gmail",
            Usuario = "user@gmail.com",
            SenhaHash = _criptografia.Criptografar("senha123"),
            Categoria = Categoria.Personal
        };

        await _repositorio.AdicionarAsync(senha);
        var total = (await _repositorio.ListarTodosAsync()).Count;

        Assert.Equal(1, total);
    }

    [Fact]
    public async Task AdicionarAsync_ComDuplicata_LancaExcecao()
    {
        var id = Guid.NewGuid();
        var senha1 = new Senha
        {
            Id = id,
            NomeServico = "Gmail",
            Usuario = "user@gmail.com",
            SenhaHash = _criptografia.Criptografar("senha123"),
            Categoria = Categoria.Personal
        };

        var senha2 = new Senha
        {
            Id = id,
            NomeServico = "Gmail2",
            Usuario = "user2@gmail.com",
            SenhaHash = _criptografia.Criptografar("senha456"),
            Categoria = Categoria.Work
        };

        await _repositorio.AdicionarAsync(senha1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repositorio.AdicionarAsync(senha2));
    }

    [Fact]
    public async Task AtualizarAsync_ComSenhaValida_AtualizaPropriedades()
    {
        var id = Guid.NewGuid();
        var senha = new Senha
        {
            Id = id,
            NomeServico = "Gmail",
            Usuario = "user@gmail.com",
            SenhaHash = _criptografia.Criptografar("senha123"),
            Categoria = Categoria.Personal,
            Favorito = false
        };

        await _repositorio.AdicionarAsync(senha);

        senha.Favorito = true;
        senha.NomeServico = "Gmail Pessoal";
        await _repositorio.AtualizarAsync(senha);

        var atualizada = await _repositorio.ObterPorIdAsync(id);

        Assert.NotNull(atualizada);
        Assert.True(atualizada.Favorito);
        Assert.Equal("Gmail Pessoal", atualizada.NomeServico);
    }

    [Fact]
    public async Task RemoverAsync_ComSenhaExistente_Remove()
    {
        var senha = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "GitHub",
            Usuario = "dev@github.com",
            SenhaHash = _criptografia.Criptografar("github123"),
            Categoria = Categoria.Work
        };

        await _repositorio.AdicionarAsync(senha);

        await _repositorio.RemoverAsync(senha.Id);
        var total = (await _repositorio.ListarTodosAsync()).Count;

        Assert.Equal(0, total);
    }

    [Fact]
    public async Task ListarTodosAsync_RetornaTodasAsSenhas()
    {
        var senha1 = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Gmail",
            Usuario = "user@gmail.com",
            SenhaHash = _criptografia.Criptografar("senha123"),
            Categoria = Categoria.Personal
        };

        var senha2 = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "GitHub",
            Usuario = "dev@github.com",
            SenhaHash = _criptografia.Criptografar("github123"),
            Categoria = Categoria.Work
        };

        await _repositorio.AdicionarAsync(senha1);
        await _repositorio.AdicionarAsync(senha2);

        var todas = await _repositorio.ListarTodosAsync();

        Assert.Equal(2, todas.Count);
    }

    [Fact]
    public async Task SalvarAsync_ECarregarDeNovo_RetornaSeunhasPersistidas()
    {
        var senhaOriginal = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "AWS",
            Usuario = "admin@company.com",
            SenhaHash = _criptografia.Criptografar("aws123"),
            Categoria = Categoria.Finance
        };

        await _repositorio.AdicionarAsync(senhaOriginal);
        await _repositorio.SalvarAsync();

        var novoRepositorio = new RepositorioSenha(_persistencia, _chave);
        var senhasCarregadas = await novoRepositorio.ListarTodosAsync();

        Assert.Single(senhasCarregadas);
        Assert.Equal("AWS", senhasCarregadas[0].NomeServico);
        Assert.Equal("admin@company.com", senhasCarregadas[0].Usuario);
    }

    [Fact]
    public async Task ObterPorIdAsync_ComIdInexistente_RetornaNull()
    {
        var resultado = await _repositorio.ObterPorIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task RemoverAsync_ComIdInexistente_LancaExcecao()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repositorio.RemoverAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AtualizarAsync_ComIdInexistente_LancaExcecao()
    {
        var senha = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Test",
            Usuario = "test@test.com",
            SenhaHash = _criptografia.Criptografar("test123"),
            Categoria = Categoria.Other
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repositorio.AtualizarAsync(senha));
    }

    [Fact]
    public async Task RemoverAsync_MoveParaLixeiraComDataDeExclusao()
    {
        var senha = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "GitHub",
            Usuario = "dev@github.com",
            SenhaHash = _criptografia.Criptografar("github123"),
            Categoria = Categoria.Work
        };

        await _repositorio.AdicionarAsync(senha);
        await _repositorio.RemoverAsync(senha.Id);

        var lixeira = await _repositorio.ListarLixeiraAsync();

        Assert.Single(lixeira);
        Assert.True(lixeira[0].NaLixeira);
        Assert.NotNull(lixeira[0].DataExclusao);
        Assert.Empty(await _repositorio.ListarTodosAsync());
    }

    [Fact]
    public async Task RestaurarAsync_TrazDeVoltaParaListarTodosELimpaDataExclusao()
    {
        var senha = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "GitHub",
            Usuario = "dev@github.com",
            SenhaHash = _criptografia.Criptografar("github123"),
            Categoria = Categoria.Work
        };

        await _repositorio.AdicionarAsync(senha);
        await _repositorio.RemoverAsync(senha.Id);
        await _repositorio.RestaurarAsync(senha.Id);

        var ativas = await _repositorio.ListarTodosAsync();

        Assert.Single(ativas);
        Assert.False(ativas[0].NaLixeira);
        Assert.Null(ativas[0].DataExclusao);
        Assert.Empty(await _repositorio.ListarLixeiraAsync());
    }

    [Fact]
    public async Task RemoverDefinitivamenteAsync_ApagaDaLixeiraParaSempre()
    {
        var senha = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "GitHub",
            Usuario = "dev@github.com",
            SenhaHash = _criptografia.Criptografar("github123"),
            Categoria = Categoria.Work
        };

        await _repositorio.AdicionarAsync(senha);
        await _repositorio.RemoverAsync(senha.Id);
        await _repositorio.RemoverDefinitivamenteAsync(senha.Id);

        Assert.Empty(await _repositorio.ListarLixeiraAsync());
        Assert.Empty(await _repositorio.ListarTodosAsync());
        Assert.Null(await _repositorio.ObterPorIdAsync(senha.Id));
    }

    [Fact]
    public async Task EsvaziarLixeiraAsync_RemoveSomenteOsItensNaLixeira()
    {
        var ativa = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Ativa",
            Usuario = "ativa@teste.com",
            SenhaHash = _criptografia.Criptografar("ativa123"),
            Categoria = Categoria.Personal
        };
        var excluida1 = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Excluida1",
            Usuario = "e1@teste.com",
            SenhaHash = _criptografia.Criptografar("e1"),
            Categoria = Categoria.Other
        };
        var excluida2 = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Excluida2",
            Usuario = "e2@teste.com",
            SenhaHash = _criptografia.Criptografar("e2"),
            Categoria = Categoria.Other
        };

        await _repositorio.AdicionarAsync(ativa);
        await _repositorio.AdicionarAsync(excluida1);
        await _repositorio.AdicionarAsync(excluida2);
        await _repositorio.RemoverAsync(excluida1.Id);
        await _repositorio.RemoverAsync(excluida2.Id);

        await _repositorio.EsvaziarLixeiraAsync();

        Assert.Empty(await _repositorio.ListarLixeiraAsync());
        var restantes = await _repositorio.ListarTodosAsync();
        Assert.Single(restantes);
        Assert.Equal("Ativa", restantes[0].NomeServico);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_pastaTemp))
                Directory.Delete(_pastaTemp, recursive: true);
        }
        catch
        {
        }
    }
}
