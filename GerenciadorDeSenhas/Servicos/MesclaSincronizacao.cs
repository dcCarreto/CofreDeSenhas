namespace GerenciadorDeSenhas.Servicos
{
    public static class MesclaSincronizacao
    {
        public static List<T> Mesclar<T>(IReadOnlyList<T> locais, IReadOnlyList<T> remotos,
            Func<T, Guid> obterId, Func<T, DateTime> obterDataAtualizacao)
        {
            var resultado = new Dictionary<Guid, T>();

            foreach (var item in locais)
                resultado[obterId(item)] = item;

            foreach (var item in remotos)
            {
                var id = obterId(item);
                if (!resultado.TryGetValue(id, out var existente) || obterDataAtualizacao(item) > obterDataAtualizacao(existente))
                    resultado[id] = item;
            }

            return resultado.Values.ToList();
        }
    }
}
