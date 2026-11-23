using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CofreDeSenhas
{
    internal static class Diagnostico
    {
        private const long TamanhoMaximoBytes = 1_000_000;
        private static readonly object Trava = new();

        // Só os testes atribuem, pra não competir pelo erros.log real entre processos.
        internal static string? CaminhoLogTestes;

        private static string CaminhoLog =>
            CaminhoLogTestes ?? Path.Combine(CaminhosApp.PastaDados, "logs", "erros.log");

        private static string CaminhoLogAnterior => CaminhoLog + ".1";

        // Erros que derrubariam o app sem passar por nenhum try/catch: antes não deixavam
        // rastro nenhum, justamente os mais importantes para um relato de bug.
        public static void InstalarCapturaGlobal()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    Registrar(ex, e.IsTerminating ? "Fatal" : "NaoTratada");
            };
            TaskScheduler.UnobservedTaskException += (_, e) => Registrar(e.Exception, "TarefaNaoObservada");
        }

        // Uma linha por abertura do app, para separar as sessões no log e saber em que
        // versão cada erro aconteceu.
        public static void RegistrarInicio() =>
            Gravar($" [Inicio] {VersaoApp()} | {RuntimeInformation.OSDescription} " +
                   $"{RuntimeInformation.ProcessArchitecture} | {RuntimeInformation.FrameworkDescription}");

        public static void Registrar(Exception ex, string? contexto = null)
        {
            var prefixoContexto = contexto != null ? $" [{contexto}]" : "";
            var texto = new StringBuilder($"{prefixoContexto} {Descrever(ex)}");
            for (var causa = ex.InnerException; causa != null; causa = causa.InnerException)
                texto.Append($" | Causa: {Descrever(causa)}");
            if (ex is AggregateException agregada)
                foreach (var interna in agregada.InnerExceptions.Skip(1))
                    texto.Append($" | Também: {Descrever(interna)}");

            var pilha = PilhaCompleta(ex);
            if (pilha.Length > 0)
                texto.Append(Environment.NewLine).Append(pilha);
            Gravar(texto.ToString());
        }

        // A Message de uma JsonException ecoa o JSON já descifrado — registra só o
        // caminho e a posição, nunca a Message.
        private static string Descrever(Exception ex) => ex switch
        {
            JsonException je => $"JsonException em {je.Path ?? "?"} " +
                $"(linha {je.LineNumber?.ToString() ?? "?"}, pos {je.BytePositionInLine?.ToString() ?? "?"})",
            _ => $"{ex.GetType().Name}: {Redigir(ex.Message)}"
        };

        // Pilha de cada exceção da cadeia, indentada. Só nomes de método e linhas —
        // nada do conteúdo do cofre passa por aqui.
        private static string PilhaCompleta(Exception ex)
        {
            var sb = new StringBuilder();
            for (Exception? atual = ex; atual != null; atual = atual.InnerException)
            {
                if (string.IsNullOrWhiteSpace(atual.StackTrace))
                    continue;
                if (sb.Length > 0)
                    sb.AppendLine($"    --- causa: {atual.GetType().Name} ---");
                foreach (var linha in atual.StackTrace.Split('\n'))
                    sb.AppendLine("    " + Redigir(linha.Trim()));
            }
            return sb.ToString().TrimEnd();
        }

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
                // Os ganchos globais gravam de outras threads.
                lock (Trava)
                {
                    var caminho = CaminhoLog;
                    var pasta = Path.GetDirectoryName(caminho)!;
                    if (!Directory.Exists(pasta))
                        Directory.CreateDirectory(pasta);

                    // Guarda o arquivo cheio como .1 em vez de apagar: o erro que motivou
                    // um relato pode estar logo antes da virada.
                    if (File.Exists(caminho) && new FileInfo(caminho).Length > TamanhoMaximoBytes)
                        File.Move(caminho, CaminhoLogAnterior, overwrite: true);

                    var linha = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z{corpo}{Environment.NewLine}";
                    File.AppendAllText(caminho, linha);
                }
            }
            catch
            {
            }
        }

        // Relatório que acompanha um relato de bug: ambiente + o fim do log. Vai para
        // uma issue pública, então tudo passa por Redigir de novo.
        public static string MontarRelatorio(int maxCaracteresLog = 45_000)
        {
            var processo = Process.GetCurrentProcess();
            var sb = new StringBuilder();
            sb.AppendLine("== Ambiente ==");
            sb.AppendLine($"Versão: {VersaoApp()}");
            sb.AppendLine($"Distribuição: {(PacoteLoja.Empacotado ? "Microsoft Store" : GerenciadorDeSenhas.AmbienteCofre.Isolado ? "Desenvolvimento (isolado)" : string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE")) ? "Instalado" : "AppImage")}");
            sb.AppendLine($"Sistema: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
            sb.AppendLine($"Processo: {RuntimeInformation.ProcessArchitecture}, {RuntimeInformation.FrameworkDescription}");
            sb.AppendLine($"Idioma do app: {Idioma.Atual.Cultura.Name} | Cultura do sistema: {CultureInfo.CurrentCulture.Name}");
            sb.AppendLine($"Tema: {Preferencias.ModoTema ?? "padrão"} | Escala: {Preferencias.EscalaInterface} | Leitor de tela: {Preferencias.LeitorTela}");
            sb.AppendLine($"Aberto há: {DateTime.Now - processo.StartTime:d\\.hh\\:mm\\:ss} | Memória: {processo.WorkingSet64 / 1_048_576} MB | Processadores: {Environment.ProcessorCount}");
            sb.AppendLine($"Horário (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("== Log (mais recente no fim) ==");
            sb.Append(FimDoLog(maxCaracteresLog));
            return NomeServicoConflito.Replace(Redigir(sb.ToString()), "$1\"***\"");
        }

        private static string FimDoLog(int maxCaracteres)
        {
            try
            {
                lock (Trava)
                {
                    var texto = string.Concat(
                        File.Exists(CaminhoLogAnterior) ? File.ReadAllText(CaminhoLogAnterior) : "",
                        File.Exists(CaminhoLog) ? File.ReadAllText(CaminhoLog) : "");
                    if (texto.Length == 0)
                        return "(vazio)";
                    return texto.Length <= maxCaracteres ? texto : "[…]" + texto[^maxCaracteres..];
                }
            }
            catch (Exception ex)
            {
                return $"(não foi possível ler o log: {ex.GetType().Name})";
            }
        }

        internal static string VersaoApp() =>
            typeof(Diagnostico).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Diagnostico).Assembly.GetName().Version?.ToString() ?? "?";

        // O log local guarda o nome do serviço do item em conflito para o usuário revisar
        // (JanelaPrincipal.Banco); numa issue pública ele vira vazamento de metadado do cofre.
        private static readonly Regex NomeServicoConflito = new(
            @"(\[ConflitoSincronizacao\] \S+ em )""[^""\r\n]*""", RegexOptions.Compiled);

        // Senhas de string de conexão que um driver de banco ecoe numa mensagem de erro.
        private static readonly Regex SegredoConexao = new(
            @"\b(password|pwd|senha)\s*=\s*[^;\s'""]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Evita gravar o caminho do perfil do Windows (que carrega o nome de usuário
        // do SO) quando ele aparece em mensagens de exceção de I/O, ex.: "Could not
        // find file 'C:\Users\alguem\AppData\...\senhas.json.enc'". O nome do usuário
        // e o do computador também saem, pois o relatório pode virar uma issue pública.
        internal static string Redigir(string mensagem)
        {
            var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(perfil))
                mensagem = mensagem.Replace(perfil, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
            mensagem = SegredoConexao.Replace(mensagem, m => m.Groups[1].Value + "=***");
            mensagem = SubstituirPalavra(mensagem, Environment.UserName, "%USERNAME%");
            return SubstituirPalavra(mensagem, Environment.MachineName, "%COMPUTERNAME%");
        }

        // Palavra inteira, para um usuário "ana" não estragar "Mana" ou "banana" no log.
        private static string SubstituirPalavra(string texto, string palavra, string marcador) =>
            palavra.Length < 3
                ? texto
                : Regex.Replace(texto, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(palavra)}(?![\p{{L}}\p{{N}}])",
                    marcador, RegexOptions.IgnoreCase);
    }
}
