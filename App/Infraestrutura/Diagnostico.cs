using System.Text.Json;

namespace CofreDeSenhas
{
    internal static class Diagnostico
    {
        private const long TamanhoMaximoBytes = 1_000_000;

        // Só os testes atribuem, pra não competir pelo erros.log real entre processos.
        internal static string? CaminhoLogTestes;

        private static string CaminhoLog =>
            CaminhoLogTestes ?? Path.Combine(CaminhosApp.PastaDados, "logs", "erros.log");

        public static void Registrar(Exception ex, string? contexto = null)
        {
            var prefixoContexto = contexto != null ? $" [{contexto}]" : "";
            var causa = ex.InnerException != null ? $" | Causa: {Descrever(ex.InnerException)}" : "";
            Gravar($"{prefixoContexto} {Descrever(ex)}{causa}");
        }

        // A Message de uma JsonException carrega um trecho do JSON sendo lido — e no caso
        // de senhas.json.enc, dos backups e do config.json esse JSON já está descifrado em
        // memória. Registra só o caminho estrutural e a posição, nunca a Message.
        private static string Descrever(Exception ex) => ex switch
        {
            JsonException je => $"JsonException em {je.Path ?? "?"} " +
                $"(linha {je.LineNumber?.ToString() ?? "?"}, pos {je.BytePositionInLine?.ToString() ?? "?"})",
            _ => $"{ex.GetType().Name}: {Redigir(ex.Message)}"
        };

        // Usado por eventos que não são exceções mas ainda merecem um rastro
        // persistente — ex.: um conflito de sincronização detectado. Sem isto, o único
        // registro era a lista em memória de RepositorioSenhaEspelhado.UltimosConflitos,
        // que se perde a cada reconexão/reinício se o usuário não abrir a tela de
        // conflitos a tempo de ver.
        public static void Registrar(string mensagem, string? contexto = null)
        {
            var prefixoContexto = contexto != null ? $" [{contexto}]" : "";
            Gravar($"{prefixoContexto} {Redigir(mensagem)}");
        }

        private static void Gravar(string corpo)
        {
            try
            {
                var caminho = CaminhoLog;
                var pasta = Path.GetDirectoryName(caminho)!;
                if (!Directory.Exists(pasta))
                    Directory.CreateDirectory(pasta);

                if (File.Exists(caminho) && new FileInfo(caminho).Length > TamanhoMaximoBytes)
                    File.Delete(caminho);

                var linha = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z{corpo}{Environment.NewLine}";
                File.AppendAllText(caminho, linha);
            }
            catch
            {
            }
        }

        // Evita gravar o caminho do perfil do Windows (que carrega o nome de usuário
        // do SO) quando ele aparece em mensagens de exceção de I/O, ex.: "Could not
        // find file 'C:\Users\alguem\AppData\...\senhas.json.enc'".
        internal static string Redigir(string mensagem)
        {
            var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.IsNullOrEmpty(perfil)
                ? mensagem
                : mensagem.Replace(perfil, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }
    }
}
