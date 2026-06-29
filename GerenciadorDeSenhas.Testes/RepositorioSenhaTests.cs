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
        var total = await _repositorio.ContarAsync();

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
        var total = await _repositorio.ContarAsync();

        Assert.Equal(0, total);
    }

    [Fact]
    public async Task BuscarPorCategoriaAsync_RetornaApenasCategoriaSolicitada()
    {
        var senhaWork = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Jira",
            Usuario = "dev@company.com",
            SenhaHash = _criptografia.Criptografar("jira123"),
            Categoria = Categoria.Work
        };

        var senhaPersonal = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Gmail",
            Usuario = "user@gmail.com",
            SenhaHash = _criptografia.Criptografar("gmail123"),
            Categoria = Categoria.Personal
        };

        await _repositorio.AdicionarAsync(senhaWork);
        await _repositorio.AdicionarAsync(senhaPersonal);

        var workSenhas = await _repositorio.BuscarPorCategoriaAsync(Categoria.Work);

        Assert.Single(workSenhas);
        Assert.Equal("Jira", workSenhas[0].NomeServico);
    }

    [Fact]
    public async Task BuscarPorServicoAsync_RetornaBuscaInsensitiva()
    {
        var senha = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "GmAiL",
            Usuario = "user@gmail.com",
            SenhaHash = _criptografia.Criptografar("senha123"),
            Categoria = Categoria.Personal
        };

        await _repositorio.AdicionarAsync(senha);

        var resultado = await _repositorio.BuscarPorServicoAsync("gmail");

        Assert.Single(resultado);
        Assert.Equal("GmAiL", resultado[0].NomeServico);
    }

    [Fact]
    public async Task ListarFavoritosAsync_RetornaApenasMarkedFavorites()
    {
        var senhaFavorita = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "Gmail",
            Usuario = "user@gmail.com",
            SenhaHash = _criptografia.Criptografar("senha123"),
            Categoria = Categoria.Personal,
            Favorito = true
        };

        var senhaNormal = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "GitHub",
            Usuario = "dev@github.com",
            SenhaHash = _criptografia.Criptografar("github123"),
            Categoria = Categoria.Work,
            Favorito = false
        };

        await _repositorio.AdicionarAsync(senhaFavorita);
        await _repositorio.AdicionarAsync(senhaNormal);

        var favoritos = await _repositorio.ListarFavoritosAsync();

        Assert.Single(favoritos);
        Assert.Equal("Gmail", favoritos[0].NomeServico);
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

    [Fact]
    public async Task ContarAsync_ComMultiplasSenhas_RetornaQuantidadeCorreta()
    {
        for (int i = 0; i < 5; i++)
        {
            var senha = new Senha
            {
                Id = Guid.NewGuid(),
                NomeServico = $"Servico{i}",
                Usuario = $"user{i}@example.com",
                SenhaHash = _criptografia.Criptografar($"senha{i}"),
                Categoria = Categoria.Personal
            };
            await _repositorio.AdicionarAsync(senha);
        }

        var total = await _repositorio.ContarAsync();

        Assert.Equal(5, total);
    }
}
