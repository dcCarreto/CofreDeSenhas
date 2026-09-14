namespace GerenciadorDeSenhas
{
    // Pasta única onde o cofre grava tudo em disco: auth.dat, senhas.json.enc,
    // anexos, config.json, logs e cache de ícones. Concentrar a decisão aqui garante
    // que uma execução de desenvolvimento — F5, `dotnet run`, `dotnet test` ou o
    // .exe de Debug — nunca escreva por cima do cofre de um app de verdade que já
    // esteja instalado na mesma máquina.
    public static class AmbienteCofre
    {
        public const string NomePastaDados = "GerenciadorSenhas";

        public static string PastaDados { get; } = Resolver();

        // true em qualquer execução que NÃO seja o app instalado: build Debug ou
        // COFRE_BASE explícito (suíte de testes, skill verify). Recursos de escopo
        // global do Windows — a credencial do Windows Hello, que é por conta do
        // Windows e não por pasta — têm que virar no-op nesse caso, senão um teste
        // ou verificação apaga/sobrescreve o Windows Hello do cofre instalado.
        public static bool Isolado { get; } = CalcularIsolado();

        private static bool CalcularIsolado()
        {
            if (Environment.GetEnvironmentVariable("COFRE_BASE") != null)
                return true;
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        private static string Resolver()
        {
            // COFRE_BASE vence tudo — é como a verificação de runtime e qualquer
            // sandbox pontual isolam o cofre sem recompilar.
            var baseExplicita = Environment.GetEnvironmentVariable("COFRE_BASE");
            if (!string.IsNullOrWhiteSpace(baseExplicita))
                return Path.Combine(baseExplicita, NomePastaDados);

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#if DEBUG
            // O instalador publica sempre em Release, então só um build de
            // desenvolvimento chega neste ramo. Pasta irmã, pra não encostar no
            // cofre real de quem também tem o app instalado.
            return Path.Combine(appData, NomePastaDados + ".dev");
#else
            return Path.Combine(appData, NomePastaDados);
#endif
        }
    }
}
