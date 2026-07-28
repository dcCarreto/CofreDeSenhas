using System.Text.RegularExpressions;

namespace App.Testes
{
    public class IdiomaParidadeTests
    {
        private static readonly string[] Arquivos =
        {
            "Idioma.PtBr.cs", "Idioma.En.cs", "Idioma.Es.cs", "Idioma.Fr.cs", "Idioma.De.cs", "Idioma.It.cs"
        };

        [Fact]
        public void TodosOsIdiomas_TemAsMesmasChaves()
        {
            var pasta = LocalizarPastaInfraestrutura();
            var chavesPorArquivo = Arquivos.ToDictionary(
                arquivo => arquivo,
                arquivo => ExtrairChaves(Path.Combine(pasta, arquivo)));

            var referencia = chavesPorArquivo["Idioma.PtBr.cs"];

            foreach (var arquivo in Arquivos)
            {
                if (arquivo == "Idioma.PtBr.cs")
                    continue;

                var chaves = chavesPorArquivo[arquivo];
                var faltando = referencia.Except(chaves).OrderBy(c => c).ToList();
                var extras = chaves.Except(referencia).OrderBy(c => c).ToList();

                Assert.True(faltando.Count == 0,
                    $"{arquivo} está sem as chaves que existem em Idioma.PtBr.cs: {string.Join(", ", faltando)}");
                Assert.True(extras.Count == 0,
                    $"{arquivo} tem chaves que não existem em Idioma.PtBr.cs: {string.Join(", ", extras)}");
            }
        }

        private static HashSet<string> ExtrairChaves(string caminhoArquivo)
        {
            var conteudo = File.ReadAllText(caminhoArquivo);
            var chaves = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(conteudo, "\\(\"([A-Za-z0-9_.]+)\",\\s*\""))
                chaves.Add(m.Groups[1].Value);
            return chaves;
        }

        private static string LocalizarPastaInfraestrutura()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CofreDeSenhas.sln")))
                dir = dir.Parent;

            if (dir == null)
                throw new InvalidOperationException(
                    "Não foi possível localizar a raiz do repositório a partir de " + AppContext.BaseDirectory);

            return Path.Combine(dir.FullName, "App", "Infraestrutura");
        }
    }
}
