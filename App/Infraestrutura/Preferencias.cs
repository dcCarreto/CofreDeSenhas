using System.Text.Json;

namespace CofreDeSenhas
{
    public static class Preferencias
    {
        private class Dados { public bool ModoEscuro { get; set; } }

        private static readonly string _caminho = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GerenciadorSenhas", "config.json");

        public static bool ModoEscuro { get; set; }

        public static void Carregar()
        {
            try
            {
                if (File.Exists(_caminho))
                {
                    var d = JsonSerializer.Deserialize<Dados>(File.ReadAllText(_caminho));
                    if (d != null) ModoEscuro = d.ModoEscuro;
                }
            }
            catch { }
        }

        public static void Salvar()
        {
            try
            {
                var dir = Path.GetDirectoryName(_caminho)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_caminho, JsonSerializer.Serialize(new Dados { ModoEscuro = ModoEscuro }));
            }
            catch { }
        }
    }
}
