using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas.Cli
{
    internal enum TipoSelecao { Nenhuma, Encontrada, Ambigua }

    internal readonly record struct ResultadoSelecao(
        TipoSelecao Tipo, Senha? Unica, IReadOnlyList<Senha> Candidatas);

    internal static class SeletorCredencial
    {
        // Casa um termo com uma credencial: nome de serviço exato primeiro, depois
        // "contém" no serviço, depois "contém" no usuário. --usuario restringe o
        // universo antes de tudo. Empate em qualquer etapa devolve Ambigua.
        public static ResultadoSelecao Selecionar(IReadOnlyList<Senha> todas, string termo, string? usuario)
        {
            IEnumerable<Senha> universo = todas;
            if (!string.IsNullOrWhiteSpace(usuario))
                universo = universo.Where(s => Contem(s.Usuario, usuario));

            var lista = universo.ToList();

            foreach (var candidatas in new[]
            {
                lista.Where(s => string.Equals(s.NomeServico, termo, StringComparison.OrdinalIgnoreCase)).ToList(),
                lista.Where(s => Contem(s.NomeServico, termo)).ToList(),
                lista.Where(s => Contem(s.Usuario, termo)).ToList(),
            })
            {
                if (candidatas.Count == 1)
                    return new ResultadoSelecao(TipoSelecao.Encontrada, candidatas[0], candidatas);
                if (candidatas.Count > 1)
                    return new ResultadoSelecao(TipoSelecao.Ambigua, null, candidatas);
            }

            return new ResultadoSelecao(TipoSelecao.Nenhuma, null, Array.Empty<Senha>());
        }

        private static bool Contem(string? alvo, string termo) =>
            alvo != null && alvo.Contains(termo, StringComparison.OrdinalIgnoreCase);
    }
}
