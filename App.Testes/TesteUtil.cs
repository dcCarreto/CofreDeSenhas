using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace App.Testes
{
    internal static class TesteUtil
    {
        private static readonly List<string> _pastasCriadas = new();
        private static bool _limpezaRegistrada;

        public static string CriarPastaTemporaria()
        {
            var pasta = Path.Combine(Path.GetTempPath(), "CofreDeSenhasTestes", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pasta);

            // Nenhum teste chama uma contraparte "Apagar" pra essas pastas (diferente
            // de PastaTemporariaTeste em GerenciadorDeSenhas.Testes) — sem isto, elas
            // acumulam indefinidamente em %TEMP%\CofreDeSenhasTestes a cada execução.
            // Registrar a limpeza aqui, uma vez por processo, cobre todo mundo que já
            // chama este método sem precisar tocar em cada teste.
            lock (_pastasCriadas)
            {
                _pastasCriadas.Add(pasta);
                if (!_limpezaRegistrada)
                {
                    _limpezaRegistrada = true;
                    AppDomain.CurrentDomain.ProcessExit += (_, _) => LimparPastasCriadas();
                }
            }

            return pasta;
        }

        private static void LimparPastasCriadas()
        {
            List<string> pastas;
            lock (_pastasCriadas)
                pastas = new List<string>(_pastasCriadas);

            foreach (var pasta in pastas)
            {
                try { if (Directory.Exists(pasta)) Directory.Delete(pasta, recursive: true); } catch { }
            }
        }

        public static T Encontrar<T>(this Window janela, string nome) where T : Control =>
            janela.FindControl<T>(nome) ?? throw new InvalidOperationException($"Controle '{nome}' não encontrado.");

        public static Button BotaoPorTexto(this Visual raiz, string texto) =>
            raiz.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => Equals(b.Content, texto))
                ?? throw new InvalidOperationException($"Botão com texto '{texto}' não encontrado.");

        public static Button BotaoPorNomeAutomacao(this Visual raiz, string nome) =>
            raiz.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => AutomationProperties.GetName(b) == nome)
                ?? throw new InvalidOperationException($"Botão com nome de automação '{nome}' não encontrado.");

        public static TextBlock TextoPorConteudo(this Visual raiz, string texto) =>
            raiz.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Text == texto)
                ?? throw new InvalidOperationException($"TextBlock com texto '{texto}' não encontrado.");

        public static async Task AguardarAsync(Func<bool> condicao, int tentativas = 50)
        {
            for (var i = 0; i < tentativas && !condicao(); i++)
            {
                Dispatcher.UIThread.RunJobs();
                await Task.Delay(10);
            }
            Dispatcher.UIThread.RunJobs();
        }
    }
}
