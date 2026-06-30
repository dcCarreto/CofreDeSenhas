namespace CofreDeSenhas.Agent;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, @"Local\CofreDeSenhas.Agent", out bool criado);
        if (!criado)
            return;

        ApplicationConfiguration.Initialize();
        Application.Run(new AgentContext());
    }
}
