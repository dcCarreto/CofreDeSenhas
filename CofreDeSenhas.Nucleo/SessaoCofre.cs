using System.Security.Cryptography;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Nucleo;

public readonly record struct ItemConsulta(Guid Id, string Servico, string Usuario, string? Url);

public readonly record struct Credencial(string Usuario, string Senha);

public readonly record struct DetalheCredencial(
    Guid Id,
    string Servico,
    string Usuario,
    string? Url,
    string Categoria,
    string? Notas,
    bool Favorito);

public readonly record struct NovaCredencial(string? Servico, string? Usuario, string? Senha, string? Url);

public readonly record struct EdicaoCredencial(
    Guid Id,
    string? Servico,
    string? Usuario,
    string? Senha,
    string? Url,
    string? Categoria,
    string? Notas,
    bool? Favorito);

public readonly record struct ResultadoAdicao(bool Ok, string? Erro, ItemConsulta? Item);

public readonly record struct ResultadoEdicao(bool Ok, string? Erro, DetalheCredencial? Item);

public readonly record struct EstadoSessao(
    bool Destrancado,
    int Total,
    int BloqueioAutomaticoSegundos,
    int? ExpiraEmSegundos);

public sealed class SessaoCofre : IDisposable
{
    private readonly object _trava = new();
    private readonly string? _pastaApp;
    private readonly AutenticacaoMestra _auth;
    private readonly DescobertaCofreLocal _descoberta;
    private readonly TimeSpan _tempoInatividade;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _gravacao = new(1, 1);

    private byte[]? _chave;
    private ServicoCriptografia? _cripto;
    private List<Senha> _senhas = new();
    private EstadoArquivoCofre? _estadoArquivo;
    private DateTime _ultimaAtividade = DateTime.UtcNow;

    public SessaoCofre(string? pastaApp = null, TimeSpan? tempoInatividade = null)
    {
        _pastaApp = pastaApp;
        _auth = new AutenticacaoMestra(pastaApp);
        _descoberta = new DescobertaCofreLocal(pastaApp);
        _tempoInatividade = tempoInatividade ?? TimeSpan.FromMinutes(15);
        _timer = new Timer(_ => VerificarInatividade(), null,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public bool Destrancado
    {
        get { lock (_trava) return _chave != null; }
    }

    public int Total
    {
        get { lock (_trava) return _chave == null ? 0 : _senhas.Count; }
    }

    public EstadoSessao ObterEstado()
    {
        lock (_trava)
        {
            var destrancado = _chave != null;
            int? expiraEm = null;
            if (destrancado)
            {
                var restante = _tempoInatividade - (DateTime.UtcNow - _ultimaAtividade);
                expiraEm = Math.Max(0, (int)Math.Ceiling(restante.TotalSeconds));
            }

            return new EstadoSessao(
                destrancado,
                destrancado ? _senhas.Count : 0,
                (int)_tempoInatividade.TotalSeconds,
                expiraEm);
        }
    }

    public EstadoCofreLocal VerificarCofre() => _descoberta.Verificar();

    public async Task<bool> DestrancarAsync(string senhaMestra)
    {
        if (!VerificarCofre().Pronto)
            return false;

        var chave = _auth.Autenticar(senhaMestra);
        if (chave == null)
            return false;

        var cripto = new ServicoCriptografia(chave);
        var persistencia = new PersistenciaLocal(cripto, _pastaApp);
        var repo = new RepositorioSenha(persistencia, chave);
        var lista = await repo.ListarTodosAsync();
        var estadoArquivo = persistencia.ObterEstadoArquivo();

        lock (_trava)
        {
            ApagarChave();
            _chave = chave;
            _cripto = cripto;
            _senhas = lista;
            _estadoArquivo = estadoArquivo;
            _ultimaAtividade = DateTime.UtcNow;
        }
        return true;
    }

    public void Trancar()
    {
        lock (_trava) TrancarInterno();
    }

    public IReadOnlyList<ItemConsulta> Consultar(string dominio)
    {
        lock (_trava)
        {
            if (_chave == null)
                return Array.Empty<ItemConsulta>();

            _ultimaAtividade = DateTime.UtcNow;
            return _senhas
                .Where(s => Dominios.Casa(dominio, s.Url))
                .Select(MapearItem)
                .ToList();
        }
    }

    public IReadOnlyList<ItemConsulta> Buscar(string termo)
    {
        lock (_trava)
        {
            if (_chave == null)
                return Array.Empty<ItemConsulta>();

            var busca = termo.Trim();
            if (busca.Length == 0)
                return Array.Empty<ItemConsulta>();

            _ultimaAtividade = DateTime.UtcNow;
            return _senhas
                .Where(s => Contem(s.NomeServico, busca) ||
                    Contem(s.Usuario, busca) ||
                    Contem(s.Url, busca) ||
                    Dominios.Casa(busca, s.Url))
                .OrderBy(s => s.NomeServico)
                .ThenBy(s => s.Usuario)
                .Take(50)
                .Select(MapearItem)
                .ToList();
        }
    }

    public Credencial? ObterCredencial(Guid id)
    {
        lock (_trava)
        {
            if (_chave == null || _cripto == null)
                return null;

            _ultimaAtividade = DateTime.UtcNow;
            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            if (senha == null)
                return null;

            return new Credencial(senha.Usuario, _cripto.Descriptografar(senha.SenhaHash));
        }
    }

    public DetalheCredencial? ObterDetalhes(Guid id)
    {
        lock (_trava)
        {
            if (_chave == null)
                return null;

            _ultimaAtividade = DateTime.UtcNow;
            var senha = _senhas.FirstOrDefault(s => s.Id == id);
            return senha == null ? null : MapearDetalhe(senha);
        }
    }

    public async Task<ResultadoAdicao> AdicionarCredencialAsync(NovaCredencial entrada)
    {
        await _gravacao.WaitAsync();
        byte[]? chave = null;

        try
        {
            var servico = entrada.Servico?.Trim() ?? string.Empty;
            var usuario = entrada.Usuario?.Trim() ?? string.Empty;
            var senhaTexto = entrada.Senha ?? string.Empty;
            var urlValida = TentarNormalizarUrl(entrada.Url, out var url);

            if (servico.Length == 0)
                return new ResultadoAdicao(false, "servico_obrigatorio", null);
            if (usuario.Length == 0)
                return new ResultadoAdicao(false, "usuario_obrigatorio", null);
            if (senhaTexto.Length == 0)
                return new ResultadoAdicao(false, "senha_obrigatoria", null);
            if (!urlValida)
                return new ResultadoAdicao(false, "url_invalida", null);

            EstadoArquivoCofre? estadoEsperado;
            lock (_trava)
            {
                if (_chave == null)
                    return new ResultadoAdicao(false, "bloqueado", null);

                if (_senhas.Any(s => Duplicada(s, servico, usuario, url)))
                    return new ResultadoAdicao(false, "duplicado", null);

                chave = _chave.ToArray();
                estadoEsperado = _estadoArquivo;
            }

            if (!estadoEsperado.HasValue)
                return new ResultadoAdicao(false, "erro_gravacao", null);

            var agora = DateTime.UtcNow;
            var cripto = new ServicoCriptografia(chave);
            var nova = new Senha
            {
                Id = Guid.NewGuid(),
                NomeServico = servico,
                Usuario = usuario,
                SenhaHash = cripto.Criptografar(senhaTexto),
                Url = url,
                Categoria = Categoria.Other,
                DataCriacao = agora,
                DataAtualizacao = agora,
            };

            List<Senha> snapshot;
            lock (_trava)
            {
                if (_chave == null)
                    return new ResultadoAdicao(false, "bloqueado", null);

                if (_senhas.Any(s => Duplicada(s, servico, usuario, url)))
                    return new ResultadoAdicao(false, "duplicado", null);

                snapshot = _senhas
                    .Select(ClonarSenha)
                    .Append(ClonarSenha(nova))
                    .ToList();
            }

            var persistencia = new PersistenciaLocal(new ServicoCriptografia(chave), _pastaApp);
            var novoEstado = await persistencia.SalvarSenhasComSegurancaAsync(snapshot, chave, estadoEsperado.Value);

            lock (_trava)
            {
                if (_chave != null)
                {
                    _senhas.Add(nova);
                    _estadoArquivo = novoEstado;
                    _ultimaAtividade = DateTime.UtcNow;
                }
            }

            return new ResultadoAdicao(true, null, MapearItem(nova));
        }
        catch (ConflitoGravacaoCofreException)
        {
            return new ResultadoAdicao(false, "conflito_escrita", null);
        }
        catch (IntegridadeCofreException)
        {
            return new ResultadoAdicao(false, "integridade_invalida", null);
        }
        catch
        {
            return new ResultadoAdicao(false, "erro_gravacao", null);
        }
        finally
        {
            if (chave != null)
                CryptographicOperations.ZeroMemory(chave);
            _gravacao.Release();
        }
    }

    public async Task<ResultadoEdicao> EditarCredencialAsync(EdicaoCredencial entrada)
    {
        await _gravacao.WaitAsync();
        byte[]? chave = null;

        try
        {
            var servico = entrada.Servico?.Trim() ?? string.Empty;
            var usuario = entrada.Usuario?.Trim() ?? string.Empty;
            var senhaTexto = entrada.Senha ?? string.Empty;
            var urlValida = TentarNormalizarUrl(entrada.Url, out var url);
            var categoriaValida = TentarCategoria(entrada.Categoria, out var categoria);
            var notas = NormalizarTextoOpcional(entrada.Notas);
            var favorito = entrada.Favorito ?? false;

            if (servico.Length == 0)
                return new ResultadoEdicao(false, "servico_obrigatorio", null);
            if (usuario.Length == 0)
                return new ResultadoEdicao(false, "usuario_obrigatorio", null);
            if (!urlValida)
                return new ResultadoEdicao(false, "url_invalida", null);
            if (!categoriaValida)
                return new ResultadoEdicao(false, "categoria_invalida", null);

            Senha atualizada;
            List<Senha> snapshot;
            EstadoArquivoCofre? estadoEsperado;
            lock (_trava)
            {
                if (_chave == null)
                    return new ResultadoEdicao(false, "bloqueado", null);

                var existente = _senhas.FirstOrDefault(s => s.Id == entrada.Id);
                if (existente == null)
                    return new ResultadoEdicao(false, "nao_encontrado", null);

                if (_senhas.Any(s => s.Id != entrada.Id && Duplicada(s, servico, usuario, url)))
                    return new ResultadoEdicao(false, "duplicado", null);

                chave = _chave.ToArray();
                var cripto = new ServicoCriptografia(chave);
                atualizada = ClonarSenha(existente);
                atualizada.NomeServico = servico;
                atualizada.Usuario = usuario;
                atualizada.Url = url;
                atualizada.Categoria = categoria;
                atualizada.Notas = notas;
                atualizada.Favorito = favorito;
                atualizada.DataAtualizacao = DateTime.UtcNow;
                if (senhaTexto.Length > 0)
                    atualizada.SenhaHash = cripto.Criptografar(senhaTexto);

                snapshot = _senhas
                    .Select(s => s.Id == entrada.Id ? ClonarSenha(atualizada) : ClonarSenha(s))
                    .ToList();
                estadoEsperado = _estadoArquivo;
            }

            if (!estadoEsperado.HasValue)
                return new ResultadoEdicao(false, "erro_gravacao", null);

            var persistencia = new PersistenciaLocal(new ServicoCriptografia(chave), _pastaApp);
            var novoEstado = await persistencia.SalvarSenhasComSegurancaAsync(snapshot, chave, estadoEsperado.Value);

            lock (_trava)
            {
                if (_chave == null)
                    return new ResultadoEdicao(true, null, MapearDetalhe(atualizada));

                var indice = _senhas.FindIndex(s => s.Id == entrada.Id);
                if (indice < 0)
                    return new ResultadoEdicao(true, null, MapearDetalhe(atualizada));

                if (_senhas.Any(s => s.Id != entrada.Id && Duplicada(s, servico, usuario, url)))
                    return new ResultadoEdicao(false, "duplicado", null);

                _senhas[indice] = atualizada;
                _estadoArquivo = novoEstado;
                _ultimaAtividade = DateTime.UtcNow;
                return new ResultadoEdicao(true, null, MapearDetalhe(atualizada));
            }
        }
        catch (ConflitoGravacaoCofreException)
        {
            return new ResultadoEdicao(false, "conflito_escrita", null);
        }
        catch (IntegridadeCofreException)
        {
            return new ResultadoEdicao(false, "integridade_invalida", null);
        }
        catch
        {
            return new ResultadoEdicao(false, "erro_gravacao", null);
        }
        finally
        {
            if (chave != null)
                CryptographicOperations.ZeroMemory(chave);
            _gravacao.Release();
        }
    }

    private void VerificarInatividade()
    {
        lock (_trava)
        {
            if (_chave != null && DateTime.UtcNow - _ultimaAtividade > _tempoInatividade)
                TrancarInterno();
        }
    }

    private void TrancarInterno()
    {
        ApagarChave();
        _cripto = null;
        _senhas = new List<Senha>();
        _estadoArquivo = null;
    }

    private void ApagarChave()
    {
        if (_chave != null)
        {
            CryptographicOperations.ZeroMemory(_chave);
            _chave = null;
        }
    }

    private static ItemConsulta MapearItem(Senha senha) =>
        new(senha.Id, senha.NomeServico, senha.Usuario, senha.Url);

    private static DetalheCredencial MapearDetalhe(Senha senha) =>
        new(
            senha.Id,
            senha.NomeServico,
            senha.Usuario,
            senha.Url,
            senha.Categoria.ToString(),
            senha.Notas,
            senha.Favorito);

    private static bool Contem(string? valor, string termo) =>
        !string.IsNullOrWhiteSpace(valor) &&
        valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

    private static bool TentarNormalizarUrl(string? url, out string? normalizada)
    {
        var texto = url?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
        {
            normalizada = null;
            return true;
        }

        if (!texto.Contains("://", StringComparison.Ordinal))
            texto = "https://" + texto;

        if (Dominios.ExtrairHost(texto).Length == 0)
        {
            normalizada = null;
            return false;
        }

        normalizada = texto;
        return true;
    }

    private static bool TentarCategoria(string? valor, out Categoria categoria)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            categoria = Categoria.Other;
            return true;
        }

        return Enum.TryParse(valor.Trim(), ignoreCase: true, out categoria) &&
            Enum.IsDefined(categoria);
    }

    private static string? NormalizarTextoOpcional(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }

    private static bool Duplicada(Senha senha, string servico, string usuario, string? url)
    {
        if (!string.Equals(senha.NomeServico, servico, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.Equals(senha.Usuario, usuario, StringComparison.OrdinalIgnoreCase))
            return false;

        var dominioNovo = Dominios.Registravel(url);
        var dominioAtual = Dominios.Registravel(senha.Url);
        if (dominioNovo.Length == 0 && dominioAtual.Length == 0)
            return true;

        return dominioNovo.Length > 0 && dominioNovo == dominioAtual;
    }

    private static Senha ClonarSenha(Senha senha) => new()
    {
        Id = senha.Id,
        NomeServico = senha.NomeServico,
        Usuario = senha.Usuario,
        SenhaHash = senha.SenhaHash,
        Url = senha.Url,
        Categoria = senha.Categoria,
        Notas = senha.Notas,
        Favorito = senha.Favorito,
        IV = senha.IV.ToArray(),
        AuthTag = senha.AuthTag.ToArray(),
        DataCriacao = senha.DataCriacao,
        DataAtualizacao = senha.DataAtualizacao,
        CamposExtras = senha.CamposExtras == null
            ? null
            : new Dictionary<string, System.Text.Json.JsonElement>(senha.CamposExtras),
    };

    public void Dispose()
    {
        _timer.Dispose();
        Trancar();
        _gravacao.Dispose();
    }
}
