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

        private static string Caminho => CaminhoOverride ?? Path.Combine(CaminhosApp.PastaDados, "pontuacao-historico.json");

        public static List<PontoPontuacaoSeguranca> Carregar()
        {
            try
            {
                if (!File.Exists(Caminho))
                    return new List<PontoPontuacaoSeguranca>();

                var pontos = JsonSerializer.Deserialize<List<PontoPontuacaoSeguranca>>(File.ReadAllText(Caminho));
                return pontos ?? new List<PontoPontuacaoSeguranca>();
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "HistoricoPontuacaoSeguranca.Carregar");
                return new List<PontoPontuacaoSeguranca>();
            }
        }

        // Excluir cofre promete apagar todo rastro do cofre (ver JanelaPrincipal,
        // ExcluirCofre_Click) — sem isto, o histórico de pontuação sobrevivia sozinho
        // em texto puro no %APPDATA%, revelando datas e a evolução da postura de
        // segurança de um cofre que o usuário já tinha decidido apagar por completo.
        public static void Limpar()
        {
            try
            {
                if (File.Exists(Caminho))
                    File.Delete(Caminho);
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "HistoricoPontuacaoSeguranca.Limpar");
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
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "HistoricoPontuacaoSeguranca.RegistrarPontuacao");
            }
        }
    }
}
