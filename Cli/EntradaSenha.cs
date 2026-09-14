namespace CofreDeSenhas.Cli
{
    internal static class EntradaSenha
    {
        // Lê a senha mestra sem eco. Se a entrada estiver redirecionada (pipe),
        // lê uma linha normal — permite scripts, mas não é a forma divulgada.
        public static string LerOculto(string prompt)
        {
            if (Console.IsInputRedirected)
                return Console.ReadLine() ?? string.Empty;

            Console.Write(prompt);
            var buffer = new System.Text.StringBuilder();
            while (true)
            {
                var tecla = Console.ReadKey(intercept: true);
                if (tecla.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return buffer.ToString();
                }
                if (tecla.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                        buffer.Remove(buffer.Length - 1, 1);
                    continue;
                }
                if (tecla.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    return string.Empty;
                }
                if (!char.IsControl(tecla.KeyChar))
                    buffer.Append(tecla.KeyChar);
            }
        }
    }
}
