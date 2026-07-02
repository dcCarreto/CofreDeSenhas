using System.Text.Json;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas
{
    public sealed class PerfilBanco
    {
        public TipoBanco Tipo { get; set; }
        public string? Host { get; set; }
        public int Porta { get; set; }
        public string? Banco { get; set; }
        public string? Usuario { get; set; }
    }

    public static class Preferencias
    {
        private class Dados
        {
            public bool ModoEscuro { get; set; }
            public PerfilBanco? UltimoBanco { get; set; }
        }

        private static readonly string _caminho = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GerenciadorSenhas", "config.json");

        public static bool ModoEscuro { get; set; }
        public static PerfilBanco? UltimoBanco { get; set; }

        public static void Carregar()
        {
            try
            {
                if (File.Exists(_caminho))
                {
                    var d = JsonSerializer.Deserialize<Dados>(File.ReadAllText(_caminho));
                    if (d != null)
                    {
                        ModoEscuro = d.ModoEscuro;
                        UltimoBanco = d.UltimoBanco;
                    }
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
                File.WriteAllText(_caminho, JsonSerializer.Serialize(new Dados { ModoEscuro = ModoEscuro, UltimoBanco = UltimoBanco }));
            }
            catch { }
        }
    }
}
