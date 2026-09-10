namespace CofreDeSenhas.Cli
{
    // Parser mínimo: separa flags dos argumentos posicionais. Uma flag consome o
    // token seguinte como valor, a menos que esse token pareça outra flag; para
    // um valor que começa com "-" (ex.: separador "-"), use --flag=valor.
    internal sealed class Argumentos
    {
        // As flags de uma letra que levam valor. Sem esta lista, "--x -c 12" faria
        // "-c" ser engolido como valor de "--x".
        private static readonly HashSet<string> FlagsCurtasComValor =
            new(StringComparer.Ordinal) { "-c", "-n", "-p", "-s", "-u", "-e", "-C" };

        private readonly List<string> _posicionais = new();
        private readonly Dictionary<string, string?> _flags = new(StringComparer.Ordinal);

        public Argumentos(IReadOnlyList<string> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];

                if (t.StartsWith("--", StringComparison.Ordinal))
                {
                    var igual = t.IndexOf('=');
                    if (igual > 2)
                    {
                        _flags[t[..igual]] = t[(igual + 1)..];
                        continue;
                    }
                    ConsumirComPossivelValor(t, tokens, ref i);
                }
                else if (FlagsCurtasComValor.Contains(t))
                {
                    ConsumirComPossivelValor(t, tokens, ref i);
                }
                else
                {
                    _posicionais.Add(t);
                }
            }
        }

        private void ConsumirComPossivelValor(string flag, IReadOnlyList<string> tokens, ref int i)
        {
            var proximo = i + 1 < tokens.Count ? tokens[i + 1] : null;
            if (proximo != null && !ParecFlag(proximo))
            {
                _flags[flag] = proximo;
                i++;
            }
            else
            {
                _flags[flag] = null;
            }
        }

        private static bool ParecFlag(string t) =>
            t.StartsWith("--", StringComparison.Ordinal) || FlagsCurtasComValor.Contains(t);

        public IReadOnlyList<string> Posicionais => _posicionais;

        public string TermoUnido() => string.Join(' ', _posicionais);

        public bool Tem(params string[] nomes) => nomes.Any(_flags.ContainsKey);

        public string? Valor(params string[] nomes)
        {
            foreach (var n in nomes)
                if (_flags.TryGetValue(n, out var v) && v != null)
                    return v;
            return null;
        }

        public int ValorInt(int padrao, params string[] nomes)
        {
            var bruto = Valor(nomes);
            return bruto != null && int.TryParse(bruto, out var n) ? n : padrao;
        }
    }
}
