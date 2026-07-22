using System.Security.Cryptography;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class PersistenciaLocalTests : IDisposable
{
    private readonly byte[] _chave;
    private readonly IServicoCriptografia _criptografia;
    private readonly PersistenciaLocal _persistencia;
    private readonly string _pastaTemp;
    private readonly string _caminhoSenhas;
    private readonly string _pastaBackup;

    public PersistenciaLocalTests()
    {
        _chave = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(_chave);

        _pastaTemp = Path.Combine(Path.GetTempPath(), "GS_Persist_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pastaTemp);

        _criptografia = new ServicoCriptografia(_chave);
        _persistencia = new PersistenciaLocal(_criptografia, _pastaTemp);

        _caminhoSenhas = Path.Combine(_pastaTemp, "senhas.json.enc");
        _pastaBackup = Path.Combine(_pastaTemp, "backups");
    }

    [Fact]
    public async Task SalvarSenhasAsync_ComListaValida_CriaArquivoCriptografado()
    {
        var senhas = new List<Senha>
        {
            new()
            {
                Id = Guid.NewGuid(),
                NomeServico = "Gmail",
                Usuario = "user@gmail.com",
                SenhaHash = _criptografia.Criptografar("senha123"),
                Categoria = Categoria.Personal,
                Favorito = true
            }
        };

        await _persistencia.SalvarSenhasAsync(senhas, _chave);

        Assert.True(File.Exists(_caminhoSenhas), "Arquivo senhas.json.enc deveria ter sido criado");
        Assert.True(new FileInfo(_caminhoSenhas).Length > 0, "Arquivo não deveria estar vazio");
    }

    [Fact]
    public async Task CarregarSenhasAsync_ComArquivoSalvo_RetornaListaIntacta()
    {
        var senhaOriginal = new Senha
        {
            Id = Guid.NewGuid(),
            NomeServico = "GitHub",
            Usuario = "dev@example.com",
            SenhaHash = _criptografia.Criptografar("github123"),
            Categoria = Categoria.Work,
            Favorito = false,
            Url = "https://github.com"
        };

        var senhas = new List<Senha> { senhaOriginal };

        await _persistencia.SalvarSenhasAsync(senhas, _chave);
        var senhasCarregadas = await _persistencia.CarregarSenhasAsync(_chave);

        Assert.Single(senhasCarregadas);
        Assert.Equal("GitHub", senhasCarregadas[0].NomeServico);
        Assert.Equal("dev@example.com", senhasCarregadas[0].Usuario);
        Assert.Equal(Categoria.Work, senhasCarregadas[0].Categoria);
    }

    [Fact]
    public async Task CarregarSenhasAsync_SemArquivoExistente_RetornaListaVazia()
    {
        var senhas = await _persistencia.CarregarSenhasAsync(_chave);

        Assert.NotNull(senhas);
        Assert.Empty(senhas);
    }

    [Fact]
    public async Task BackupAutomaticoAsync_ComListaValida_CriaArquivoBackup()
    {
        var senhas = new List<Senha>
        {
            new()
            {
                Id = Guid.NewGuid(),
                NomeServico = "AWS",
                Usuario = "admin@company.com",
                SenhaHash = _criptografia.Criptografar("aws123"),
                Categoria = Categoria.Finance
            }
        };

        await _persistencia.BackupAutomaticoAsync(senhas, _chave);

        Assert.True(Directory.Exists(_pastaBackup), "Pasta de backups deveria ter sido criada");

        var backups = Directory.GetFiles(_pastaBackup, "senhas_backup_*.json.enc");
        Assert.NotEmpty(backups);
    }

    [Fact]
    public async Task ListarBackups_ComVariosBackups_RetornaOrdenadosPorDataDecrescente()
    {
        var senhas = new List<Senha> { NovaSenhaDeTeste("Serviço1") };

        await _persistencia.BackupAutomaticoAsync(senhas, _chave);
        await Task.Delay(10);
        await _persistencia.BackupAutomaticoAsync(senhas, _chave);
        await Task.Delay(10);
        await _persistencia.BackupAutomaticoAsync(senhas, _chave);

        var backups = _persistencia.ListarBackups();

        Assert.Equal(3, backups.Count);
        Assert.True(backups[0].DataUtc >= backups[1].DataUtc);
        Assert.True(backups[1].DataUtc >= backups[2].DataUtc);
    }

    [Fact]
    public async Task BackupAutomaticoAsync_ComQuantidadeMaximaMenor_MantemApenasAsMaisRecentes()
    {
        var senhas = new List<Senha> { NovaSenhaDeTeste("Serviço1") };

        for (int i = 0; i < 5; i++)
        {
            await _persistencia.BackupAutomaticoAsync(senhas, _chave, quantidadeMaxima: 3);
            await Task.Delay(10);
        }

        var backups = _persistencia.ListarBackups();

        Assert.Equal(3, backups.Count);
    }

    [Fact]
    public async Task CarregarBackupAsync_ComBackupSalvo_RetornaListaIntacta()
    {
        var senhas = new List<Senha> { NovaSenhaDeTeste("ServicoBackup") };

        await _persistencia.BackupAutomaticoAsync(senhas, _chave);
        var backup = Assert.Single(_persistencia.ListarBackups());

        var restauradas = await _persistencia.CarregarBackupAsync(backup.Caminho);

        Assert.Single(restauradas);
        Assert.Equal("ServicoBackup", restauradas[0].NomeServico);
    }

    [Fact]
    public void ListarBackups_SemBackupsRealizados_RetornaListaVazia()
    {
        Assert.Empty(_persistencia.ListarBackups());
    }

    private Senha NovaSenhaDeTeste(string nomeServico) => new()
    {
        Id = Guid.NewGuid(),
        NomeServico = nomeServico,
        Usuario = "user@example.com",
        SenhaHash = _criptografia.Criptografar("senha123"),
        Categoria = Categoria.Personal
    };

    [Fact]
    public async Task ValidarIntegridade_ComEstruturaValida_RetornaTrue()
    {
        var senhas = new List<Senha>
        {
            new()
            {
                Id = Guid.NewGuid(),
                NomeServico = "Test",
                Usuario = "test@test.com",
                SenhaHash = _criptografia.Criptografar("test123"),
                Categoria = Categoria.Other
            }
        };
        await _persistencia.SalvarSenhasAsync(senhas, _chave);

        var resultado = _persistencia.ValidarIntegridade();

        Assert.True(resultado, "ValidarIntegridade deveria retornar true com estrutura válida");
    }

    [Fact]
    public async Task ApagarTudoAsync_ApagaArquivoDeSenhasEPastaDeBackup()
    {
        var senhas = new List<Senha>
        {
            new()
            {
                Id = Guid.NewGuid(),
                NomeServico = "Test",
                Usuario = "test@test.com",
                SenhaHash = _criptografia.Criptografar("test123"),
                Categoria = Categoria.Other
            }
        };
        await _persistencia.SalvarSenhasAsync(senhas, _chave);
        await _persistencia.BackupAutomaticoAsync(senhas, _chave);
        Assert.True(File.Exists(_caminhoSenhas));
        Assert.True(Directory.Exists(_pastaBackup));

        await _persistencia.ApagarTudoAsync();

        Assert.False(File.Exists(_caminhoSenhas));
        Assert.False(Directory.Exists(_pastaBackup));
    }

    [Fact]
    public async Task ApagarTudoAsync_SemNadaParaApagar_NaoLancaExcecao()
    {
        await _persistencia.ApagarTudoAsync();

        Assert.False(File.Exists(_caminhoSenhas));
    }

    [Fact]
    public async Task SalvarSenhasAsync_ComChaveNula_LancaExcecao()
    {
        var senhas = new List<Senha>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _persistencia.SalvarSenhasAsync(senhas, null!));
    }

    [Fact]
    public async Task CarregarSenhasAsync_ComChaveNula_LancaExcecao()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _persistencia.CarregarSenhasAsync(null!));
    }

    [Fact]
    public async Task CarregarSenhasAsync_ComChaveIncorreta_RetornaErro()
    {
        var senhas = new List<Senha>
        {
            new()
            {
                Id = Guid.NewGuid(),
                NomeServico = "Test",
                Usuario = "test@test.com",
                SenhaHash = _criptografia.Criptografar("test123"),
                Categoria = Categoria.Other
            }
        };

        await _persistencia.SalvarSenhasAsync(senhas, _chave);

        var chaveIncorreta = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(chaveIncorreta);

        var servicoIncorreto = new ServicoCriptografia(chaveIncorreta);
        var persistenciaIncorreta = new PersistenciaLocal(servicoIncorreto, _pastaTemp);

        var ex = await Assert.ThrowsAsync<ErroLocalizavel>(() =>
            persistenciaIncorreta.CarregarSenhasAsync(chaveIncorreta));

        Assert.Equal("Vault.Error.WrongKeyOrCorrupt", ex.Chave);
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
