using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CofreDeSenhas.Nucleo;

public sealed class Processador
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly SessaoCofre _sessao;
    private readonly Func<bool> _solicitarUnlock;

    public Processador(SessaoCofre sessao, Func<bool> solicitarUnlock)
    {
        _sessao = sessao;
        _solicitarUnlock = solicitarUnlock;
    }

    public string Processar(string requisicaoJson)
    {
        Requisicao? req;
        try
        {
            req = JsonSerializer.Deserialize<Requisicao>(requisicaoJson, Opcoes);
        }
        catch (JsonException)
        {
            return Erro("json_invalido");
        }

        if (req?.Tipo is not { Length: > 0 } tipo)
            return Erro("tipo_ausente");

        switch (tipo)
        {
            case "status":
                var sessao = _sessao.ObterEstado();
                return Ok(new
                {
                    destrancado = sessao.Destrancado,
                    total = sessao.Total,
                    sessao = MapearSessao(sessao),
                    cofre = MapearCofre(_sessao.VerificarCofre()),
                });

            case "unlock":
                var estadoUnlock = _sessao.VerificarCofre();
                if (!estadoUnlock.Pronto)
                    return ErroCofre(estadoUnlock);
                if (_sessao.Destrancado)
                    return Ok(new { status = "unlocked" });
                var destrancou = _solicitarUnlock();
                return Ok(new { status = destrancou ? "unlocked" : "cancelled" });

            case "lock":
                _sessao.Trancar();
                return Ok(new { });

            case "query":
                var estadoConsulta = _sessao.VerificarCofre();
                if (!estadoConsulta.Pronto && !_sessao.Destrancado)
                    return ErroCofre(estadoConsulta);
                if (!_sessao.Destrancado)
                    return Erro("bloqueado");
                if (string.IsNullOrWhiteSpace(req.Dominio))
                    return Erro("dominio_ausente");
                var itens = _sessao.Consultar(req.Dominio)
                    .Select(MapearItem)
                    .ToList();
                return Ok(new { itens });

            case "search":
                var estadoBusca = _sessao.VerificarCofre();
                if (!estadoBusca.Pronto && !_sessao.Destrancado)
                    return ErroCofre(estadoBusca);
                if (!_sessao.Destrancado)
                    return Erro("bloqueado");
                if (string.IsNullOrWhiteSpace(req.Termo))
                    return Ok(new { itens = Array.Empty<object>() });
                var encontrados = _sessao.Buscar(req.Termo)
                    .Select(MapearItem)
                    .ToList();
                return Ok(new { itens = encontrados });

            case "getCredential":
                var estadoCredencial = _sessao.VerificarCofre();
                if (!estadoCredencial.Pronto && !_sessao.Destrancado)
                    return ErroCofre(estadoCredencial);
                if (!_sessao.Destrancado)
                    return Erro("bloqueado");
                if (!Guid.TryParse(req.Id, out var id))
                    return Erro("id_invalido");
                var cred = _sessao.ObterCredencial(id);
                return cred is null
                    ? Erro("nao_encontrado")
                    : Ok(new { usuario = cred.Value.Usuario, senha = cred.Value.Senha });

            case "getItem":
                var estadoItem = _sessao.VerificarCofre();
                if (!estadoItem.Pronto && !_sessao.Destrancado)
                    return ErroCofre(estadoItem);
                if (!_sessao.Destrancado)
                    return Erro("bloqueado");
                if (!Guid.TryParse(req.Id, out var idItem))
                    return Erro("id_invalido");
                var detalhe = _sessao.ObterDetalhes(idItem);
                return detalhe is null
                    ? Erro("nao_encontrado")
                    : Ok(new { item = MapearDetalhe(detalhe.Value) });

            case "addCredential":
                var estadoAdicao = _sessao.VerificarCofre();
                if (!estadoAdicao.Pronto)
                    return ErroCofre(estadoAdicao);
                if (!_sessao.Destrancado)
                    return Erro("bloqueado");
                var resultado = _sessao.AdicionarCredencialAsync(
                    new NovaCredencial(req.Servico, req.Usuario, req.Senha, req.Url))
                    .GetAwaiter()
                    .GetResult();
                return resultado.Ok && resultado.Item is { } item
                    ? Ok(new { item = MapearItem(item) })
                    : Erro(resultado.Erro ?? "erro_gravacao");

            case "updateCredential":
                var estadoEdicao = _sessao.VerificarCofre();
                if (!estadoEdicao.Pronto)
                    return ErroCofre(estadoEdicao);
                if (!_sessao.Destrancado)
                    return Erro("bloqueado");
                if (!Guid.TryParse(req.Id, out var idEdicao))
                    return Erro("id_invalido");
                var edicao = _sessao.EditarCredencialAsync(
                    new EdicaoCredencial(
                        idEdicao,
                        req.Servico,
                        req.Usuario,
                        req.Senha,
                        req.Url,
                        req.Categoria,
                        req.Notas,
                        req.Favorito))
                    .GetAwaiter()
                    .GetResult();
                return edicao.Ok && edicao.Item is { } editado
                    ? Ok(new { item = MapearDetalhe(editado) })
                    : Erro(edicao.Erro ?? "erro_gravacao");

            default:
                return Erro("tipo_desconhecido");
        }
    }

    private static string Ok(object dados)
    {
        var node = JsonSerializer.SerializeToNode(dados, Opcoes)!.AsObject();
        node["ok"] = true;
        return node.ToJsonString();
    }

    private static string Erro(string codigo) =>
        JsonSerializer.Serialize(new { ok = false, erro = codigo }, Opcoes);

    private static string ErroCofre(EstadoCofreLocal estado) =>
        JsonSerializer.Serialize(new
        {
            ok = false,
            erro = CodigoErroCofre(estado),
            cofre = MapearCofre(estado),
        }, Opcoes);

    private static string CodigoErroCofre(EstadoCofreLocal estado) =>
        estado.Estado switch
        {
            "sem_permissao" => "cofre_sem_permissao",
            "formato_invalido" => "cofre_formato_invalido",
            _ => "cofre_nao_encontrado",
        };

    private static object MapearCofre(EstadoCofreLocal estado) => new
    {
        encontrado = estado.Encontrado,
        pronto = estado.Pronto,
        estado = estado.Estado,
        formato = estado.Formato,
        erro = estado.Erro,
    };

    private static object MapearSessao(EstadoSessao estado) => new
    {
        destrancado = estado.Destrancado,
        total = estado.Total,
        bloqueioAutomaticoSegundos = estado.BloqueioAutomaticoSegundos,
        expiraEmSegundos = estado.ExpiraEmSegundos,
    };

    private static object MapearItem(ItemConsulta item) => new
    {
        id = item.Id.ToString(),
        servico = item.Servico,
        usuario = item.Usuario,
        url = item.Url,
    };

    private static object MapearDetalhe(DetalheCredencial item) => new
    {
        id = item.Id.ToString(),
        servico = item.Servico,
        usuario = item.Usuario,
        url = item.Url,
        categoria = item.Categoria,
        notas = item.Notas,
        favorito = item.Favorito,
    };

    private sealed class Requisicao
    {
        public string? Tipo { get; set; }
        public string? Dominio { get; set; }
        public string? Termo { get; set; }
        public string? Id { get; set; }
        public string? Servico { get; set; }
        public string? Usuario { get; set; }
        public string? Senha { get; set; }
        public string? Url { get; set; }
        public string? Categoria { get; set; }
        public string? Notas { get; set; }
        public bool? Favorito { get; set; }
    }
}
