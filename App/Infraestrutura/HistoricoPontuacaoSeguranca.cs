using System.Text.Json;

namespace CofreDeSenhas
{
    public sealed class PontoPontuacaoSeguranca
    {
        public DateTime DataUtc { get; set; }
        public int Pontuacao { get; set; }
    }

    public static class HistoricoPontuacaoSeguranca
    {
        private const int MaximoPontos = 90;

        // Setado pelos testes pra apontar pra uma pasta descartável — sem isso, cada teste
        // que abre a janela do relatório gravaria de verdade no %APPDATA% da máquina.
        internal static string? CaminhoOverride { get; set; }

        private static string Caminho => CaminhoOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            CaminhosApp.PastaDados, "pontuacao-historico.json");

        public static List<PontoPontuacaoSeguranca> Carregar()
        {
            try
            {
                if (!File.Exists(Caminho))
                    return new List<PontoPontuacaoSeguranca>();

                var pontos = JsonSerializer.Deserialize<List<PontoPontuacaoSeguranca>>(File.ReadAllText(Caminho));
                return pontos ?? new List<PontoPontuacaoSeguranca>();
            }
            catch
            {
                return new List<PontoPontuacaoSeguranca>();
            }
        }

        public static void RegistrarPontuacao(int pontuacao)
        {
            try
            {
                var pontos = Carregar();
                var hoje = DateTime.UtcNow.Date;
                var existente = pontos.FirstOrDefault(p => p.DataUtc.Date == hoje);
                if (existente != null)
                    existente.Pontuacao = pontuacao;
                else
                    pontos.Add(new PontoPontuacaoSeguranca { DataUtc = DateTime.UtcNow, Pontuacao = pontuacao });

                pontos = pontos.OrderBy(p => p.DataUtc).ToList();
                if (pontos.Count > MaximoPontos)
                    pontos = pontos.Skip(pontos.Count - MaximoPontos).ToList();

                var caminho = Caminho;
                var dir = Path.GetDirectoryName(caminho)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(caminho, JsonSerializer.Serialize(pontos));
            }
            catch
            {
            }
        }
    }
}
