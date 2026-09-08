using System.Text.RegularExpressions;

namespace CofreDeSenhas
{
    internal static class ManualAjuda
    {
        public const string TopicoPadrao = "introducao";

        public static readonly IReadOnlyList<(string Id, string Chave)> Topicos = new (string Id, string Chave)[]
        {
            ("introducao", "Ajuda.Topico.Introducao"),
            ("primeiros-passos", "Ajuda.Topico.PrimeirosPassos"),
            ("senha-mestra", "Ajuda.Topico.SenhaMestra"),
            ("gerador", "Ajuda.Topico.Gerador"),
            ("organizacao", "Ajuda.Topico.Organizacao"),
            ("seguranca", "Ajuda.Topico.Seguranca"),
            ("backup", "Ajuda.Topico.Backup"),
            ("sincronizacao", "Ajuda.Topico.Sincronizacao"),
            ("banco-de-dados", "Ajuda.Topico.BancoDeDados"),
            ("windows-hello", "Ajuda.Topico.WindowsHello"),
            ("importar-exportar", "Ajuda.Topico.ImportarExportar"),
            ("privacidade-rede", "Ajuda.Topico.PrivacidadeRede"),
            ("aparencia-acessibilidade", "Ajuda.Topico.AparenciaAcessibilidade"),
            ("atalhos", "Ajuda.Topico.Atalhos"),
            ("faq", "Ajuda.Topico.Faq")
        };

        private static readonly Lazy<IReadOnlyDictionary<string, string>> _conteudo = new(Carregar);

        public static bool TopicoExiste(string? id) => id != null && Topicos.Any(t => t.Id == id);

        public static string NormalizarId(string? id) => TopicoExiste(id) ? id! : TopicoPadrao;

        public static string Titulo(string id)
        {
            foreach (var (topicoId, chave) in Topicos)
                if (topicoId == id)
                    return Idioma.Texto(chave);
            return Idioma.Texto("Ajuda.Titulo");
        }

        public static string Conteudo(string id) => Conteudo(id, Idioma.Atual.Codigo);

        internal static string Conteudo(string id, string idioma)
        {
            var mapa = _conteudo.Value;
            if (mapa.TryGetValue(Chave(idioma, id), out var texto)) return texto;
            if (mapa.TryGetValue(Chave("pt-BR", id), out texto)) return texto;
            return "";
        }

        private static string Chave(string idioma, string id) => idioma + "/" + id;

        private static IReadOnlyDictionary<string, string> Carregar()
        {
            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var asm = typeof(ManualAjuda).Assembly;
            var rx = new Regex(@"\.Ajuda\.(?<lang>[A-Za-z]{2}(?:[_-][A-Za-z]{2})?)\.(?<id>.+)\.md$", RegexOptions.IgnoreCase);

            foreach (var nome in asm.GetManifestResourceNames())
            {
                var m = rx.Match(nome);
                if (!m.Success) continue;

                using var stream = asm.GetManifestResourceStream(nome);
                if (stream == null) continue;
                using var leitor = new StreamReader(stream);

                var idioma = m.Groups["lang"].Value.Replace('_', '-');
                mapa[Chave(idioma, m.Groups["id"].Value)] = leitor.ReadToEnd();
            }

            return mapa;
        }
    }
}
