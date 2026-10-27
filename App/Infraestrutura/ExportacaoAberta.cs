using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas
{
    internal static class ExportacaoAberta
    {
        private static readonly string[] CabecalhoBitwarden =
        {
            "folder", "favorite", "type", "name", "notes", "fields", "reprompt",
            "login_uri", "login_username", "login_password", "login_totp"
        };

        public static string CsvBitwarden(IEnumerable<SenhaExportada> itens)
        {
            var sb = new StringBuilder();
            sb.Append(string.Join(",", CabecalhoBitwarden)).Append('\n');
            foreach (var s in itens)
            {
                var campos = CamposPersonalizados(s).Select(c => $"{c.Nome}: {c.Valor}");
                var celulas = new[]
                {
                    Idioma.RotuloCategoria(s.Categoria), s.Favorito ? "1" : "", "login", s.NomeServico,
                    s.Notas ?? "", string.Join("\n", campos), "0",
                    s.Url ?? "", s.Usuario, s.Senha, s.TotpSegredo ?? ""
                };
                sb.Append(string.Join(",", celulas.Select(Celula))).Append('\n');
            }
            return sb.ToString();
        }

        public static string JsonBitwarden(IEnumerable<SenhaExportada> itens)
        {
            var lista = itens.ToList();
            var pastas = lista.Select(s => s.Categoria).Distinct()
                .ToDictionary(c => c, _ => Guid.NewGuid().ToString());

            var raiz = new JsonObject
            {
                ["encrypted"] = false,
                ["folders"] = new JsonArray(pastas.Select(p => (JsonNode)new JsonObject
                {
                    ["id"] = p.Value,
                    ["name"] = Idioma.RotuloCategoria(p.Key)
                }).ToArray()),
                ["items"] = new JsonArray(lista.Select(s => (JsonNode)new JsonObject
                {
                    ["id"] = s.Id.ToString(),
                    ["folderId"] = pastas[s.Categoria],
                    ["type"] = 1,
                    ["reprompt"] = 0,
                    ["name"] = s.NomeServico,
                    ["notes"] = s.Notas,
                    ["favorite"] = s.Favorito,
                    ["fields"] = new JsonArray(CamposPersonalizados(s).Select(c => (JsonNode)new JsonObject
                    {
                        ["name"] = c.Nome,
                        ["value"] = c.Valor,
                        ["type"] = c.Oculto ? 1 : 0
                    }).ToArray()),
                    ["login"] = new JsonObject
                    {
                        ["uris"] = string.IsNullOrEmpty(s.Url)
                            ? new JsonArray()
                            : new JsonArray(new JsonObject { ["match"] = null, ["uri"] = s.Url }),
                        ["username"] = s.Usuario,
                        ["password"] = s.Senha,
                        ["totp"] = s.TotpSegredo
                    },
                    ["passwordHistory"] = new JsonArray(s.Historico.Select(h => (JsonNode)new JsonObject
                    {
                        ["lastUsedDate"] = Data(h.DataAlteracao),
                        ["password"] = h.Senha
                    }).ToArray()),
                    ["creationDate"] = Data(s.DataCriacao),
                    ["revisionDate"] = Data(s.DataAtualizacao)
                }).ToArray())
            };

            return raiz.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private static IEnumerable<(string Nome, string Valor, bool Oculto)> CamposPersonalizados(SenhaExportada s)
        {
            if (s.Tipo != TipoCredencial.Login)
                yield return (Idioma.Texto("Entry.Type"), Idioma.RotuloTipoCredencial(s.Tipo), false);

            var definicoes = TemplatesCredencial.CamposExtras(s.Tipo);
            foreach (var (chave, valor) in s.CamposExtras)
            {
                var definicao = definicoes.FirstOrDefault(d => d.Chave == chave);
                var nome = definicao.ChaveRotulo != null ? Idioma.Texto(definicao.ChaveRotulo) : chave;
                yield return (nome, valor, chave == "cvv");
            }

            if (s.Etiquetas.Count > 0)
                yield return (Idioma.Texto("Export.Field.Tags"), string.Join(", ", s.Etiquetas), false);

            foreach (var codigo in s.CodigosRecuperacao)
                yield return (Idioma.Texto(codigo.Usado ? "Export.Field.RecoveryCodeUsed" : "A11y.RecoveryCode"), codigo.Codigo, true);
        }

        private static string Celula(string valor) =>
            valor.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
                ? "\"" + valor.Replace("\"", "\"\"") + "\""
                : valor;

        private static string Data(DateTime data) =>
            DateTime.SpecifyKind(data, data.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : data.Kind)
                .ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
    }
}
