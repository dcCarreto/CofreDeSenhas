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
                return Ok(new { destrancado = _sessao.Destrancado, total = _sessao.Total });

            case "unlock":
                if (_sessao.Destrancado)
                    return Ok(new { status = "unlocked" });
                var destrancou = _solicitarUnlock();
                return Ok(new { status = destrancou ? "unlocked" : "cancelled" });

            case "lock":
                _sessao.Trancar();
                return Ok(new { });

            case "query":
                if (!_sessao.Destrancado)
                    return Erro("bloqueado");
                if (string.IsNullOrWhiteSpace(req.Dominio))
                    return Erro("dominio_ausente");
                var itens = _sessao.Consultar(req.Dominio)
                    .Select(i => new { id = i.Id.ToString(), servico = i.Servico, usuario = i.Usuario })
                    .ToList();
                return Ok(new { itens });

            case "getCredential":
                if (!_sessao.Destrancado)
                    return Erro("bloqueado");
                if (!Guid.TryParse(req.Id, out var id))
                    return Erro("id_invalido");
                var cred = _sessao.ObterCredencial(id);
                return cred is null
                    ? Erro("nao_encontrado")
                    : Ok(new { usuario = cred.Value.Usuario, senha = cred.Value.Senha });

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

    private sealed class Requisicao
    {
        public string? Tipo { get; set; }
        public string? Dominio { get; set; }
        public string? Id { get; set; }
    }
}
