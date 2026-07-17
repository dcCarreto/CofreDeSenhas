using System.Globalization;
using Avalonia;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas
{
    internal sealed class IdiomaDisponivel
    {
        public IdiomaDisponivel(string codigo, string nomeNativo, string cultura)
        {
            Codigo = codigo;
            NomeNativo = nomeNativo;
            Cultura = CultureInfo.GetCultureInfo(cultura);
        }

        public string Codigo { get; }
        public string NomeNativo { get; }
        public CultureInfo Cultura { get; }

        public override string ToString() => NomeNativo;
    }

    internal static partial class Idioma
    {
        private const string CodigoPadrao = "pt-BR";

        private static readonly IReadOnlyDictionary<string, string> PtBr = CriarPtBr();
        private static readonly IReadOnlyDictionary<string, string> En = CriarEn();
        private static readonly IReadOnlyDictionary<string, string> Es = CriarEs();
        private static readonly IReadOnlyDictionary<string, string> Fr = CriarFr();
        private static readonly IReadOnlyDictionary<string, string> De = CriarDe();
        private static readonly IReadOnlyDictionary<string, string> It = CriarIt();

        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Traducoes =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                [CodigoPadrao] = PtBr,
                ["en"] = En,
                ["es"] = Es,
                ["fr"] = Fr,
                ["de"] = De,
                ["it"] = It,
            };

        public static IReadOnlyList<IdiomaDisponivel> Idiomas { get; } = new[]
        {
            new IdiomaDisponivel("pt-BR", "Português (Brasil)", "pt-BR"),
            new IdiomaDisponivel("en", "English", "en-US"),
            new IdiomaDisponivel("es", "Español", "es-ES"),
            new IdiomaDisponivel("fr", "Français", "fr-FR"),
            new IdiomaDisponivel("de", "Deutsch", "de-DE"),
            new IdiomaDisponivel("it", "Italiano", "it-IT")
        };

        public static event EventHandler? Alterado;

        public static IdiomaDisponivel Atual { get; private set; } = Idiomas[0];

        public static CultureInfo CulturaAtual => Atual.Cultura;

        public static void Definir(string? codigo)
        {
            var novo = Resolver(codigo);
            bool mudou = !string.Equals(Atual.Codigo, novo.Codigo, StringComparison.OrdinalIgnoreCase);

            Atual = novo;
            CultureInfo.CurrentCulture = novo.Cultura;
            CultureInfo.CurrentUICulture = novo.Cultura;
            AplicarRecursos();

            if (mudou)
                Alterado?.Invoke(null, EventArgs.Empty);
        }

        public static string Texto(string chave)
        {
            var traducoes = Traducoes.TryGetValue(Atual.Codigo, out var atual)
                ? atual
                : PtBr;

            if (traducoes.TryGetValue(chave, out var texto) || PtBr.TryGetValue(chave, out texto))
                return texto;

            return chave;
        }

        public static string Formatar(string chave, params object?[] args) =>
            string.Format(CulturaAtual, Texto(chave), args);

        public static string Plural(int quantidade, string chaveSingular, string chavePlural) =>
            Formatar(quantidade == 1 ? chaveSingular : chavePlural, quantidade);

        public static string RotuloCategoria(Categoria categoria) => categoria switch
        {
            Categoria.Work => Texto("Category.Work"),
            Categoria.Personal => Texto("Category.Personal"),
            Categoria.Finance => Texto("Category.Finance"),
            Categoria.Social => Texto("Category.Social"),
            _ => Texto("Category.Other")
        };

        public static IEnumerable<string> RotulosCategoria(Categoria categoria)
        {
            var chave = categoria switch
            {
                Categoria.Work => "Category.Work",
                Categoria.Personal => "Category.Personal",
                Categoria.Finance => "Category.Finance",
                Categoria.Social => "Category.Social",
                _ => "Category.Other"
            };

            return Traducoes.Values
                .Select(t => t.TryGetValue(chave, out var texto) ? texto : PtBr[chave])
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static IdiomaDisponivel Resolver(string? codigo)
        {
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var normalizado = codigo.Trim();
                var direto = Idiomas.FirstOrDefault(i =>
                    string.Equals(i.Codigo, normalizado, StringComparison.OrdinalIgnoreCase));
                if (direto != null)
                    return direto;

                var porPrefixo = Idiomas.FirstOrDefault(i =>
                    normalizado.StartsWith(i.Codigo + "-", StringComparison.OrdinalIgnoreCase) ||
                    i.Codigo.StartsWith(normalizado + "-", StringComparison.OrdinalIgnoreCase));
                if (porPrefixo != null)
                    return porPrefixo;
            }

            return Idiomas[0];
        }

        private static void AplicarRecursos()
        {
            if (Application.Current == null)
                return;

            foreach (var chave in PtBr.Keys)
                Application.Current.Resources[chave] = Texto(chave);
        }

        private static IReadOnlyDictionary<string, string> Criar(IEnumerable<(string Chave, string Valor)> entradas) =>
            entradas.ToDictionary(e => e.Chave, e => e.Valor, StringComparer.OrdinalIgnoreCase);

        private static IReadOnlyDictionary<string, string> Mesclar(IEnumerable<(string Chave, string Valor)> entradas)
        {
            var dicionario = new Dictionary<string, string>(PtBr, StringComparer.OrdinalIgnoreCase);
            foreach (var (chave, valor) in entradas)
                dicionario[chave] = valor;
            return dicionario;
        }
    }
}

