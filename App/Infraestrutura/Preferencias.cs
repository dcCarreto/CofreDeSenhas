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
        public string? SenhaCifrada { get; set; }
        public bool Conectado { get; set; }
    }

    public static class Preferencias
    {
        private class Dados
        {
            public bool ModoEscuro { get; set; }
            public PerfilBanco? UltimoBanco { get; set; }
            public int MinutosBloqueio { get; set; } = 5;
            public string? Idioma { get; set; }
            public string? Daltonismo { get; set; }
            public bool AltoContraste { get; set; }
            public double EscalaInterface { get; set; } = 1.0;
            public bool ReduzirAnimacoes { get; set; }
            public bool LeitorTela { get; set; }
            public bool IconesOnline { get; set; }
            public int SegundosLimpezaClipboard { get; set; } = 30;
            public string FrequenciaBackup { get; set; } = "Semanal";
            public int MaximoBackups { get; set; } = 10;
        }

        private static readonly string _caminho = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GerenciadorSenhas", "config.json");

        public static bool ModoEscuro { get; set; }
        public static PerfilBanco? UltimoBanco { get; set; }
        public static int MinutosBloqueio { get; set; } = 5;
        public static string? Idioma { get; set; }
        public static string? Daltonismo { get; set; }
        public static bool AltoContraste { get; set; }
        public static double EscalaInterface { get; set; } = 1.0;
        public static bool ReduzirAnimacoes { get; set; }
        public static bool LeitorTela { get; set; }
        public static bool IconesOnline { get; set; }
        public static int SegundosLimpezaClipboard { get; set; } = 30;
        public static string FrequenciaBackup { get; set; } = "Semanal";
        public static int MaximoBackups { get; set; } = 10;

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
                        MinutosBloqueio = d.MinutosBloqueio;
                        Idioma = d.Idioma;
                        Daltonismo = d.Daltonismo;
                        AltoContraste = d.AltoContraste;
                        EscalaInterface = d.EscalaInterface <= 0 ? 1.0 : d.EscalaInterface;
                        ReduzirAnimacoes = d.ReduzirAnimacoes;
                        LeitorTela = d.LeitorTela;
                        IconesOnline = d.IconesOnline;
                        SegundosLimpezaClipboard = d.SegundosLimpezaClipboard;
                        FrequenciaBackup = string.IsNullOrEmpty(d.FrequenciaBackup) ? "Semanal" : d.FrequenciaBackup;
                        MaximoBackups = d.MaximoBackups <= 0 ? 10 : d.MaximoBackups;
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
                File.WriteAllText(_caminho, JsonSerializer.Serialize(new Dados { ModoEscuro = ModoEscuro, UltimoBanco = UltimoBanco, MinutosBloqueio = MinutosBloqueio, Idioma = Idioma, Daltonismo = Daltonismo, AltoContraste = AltoContraste, EscalaInterface = EscalaInterface, ReduzirAnimacoes = ReduzirAnimacoes, LeitorTela = LeitorTela, IconesOnline = IconesOnline, SegundosLimpezaClipboard = SegundosLimpezaClipboard, FrequenciaBackup = FrequenciaBackup, MaximoBackups = MaximoBackups }));
            }
            catch { }
        }
    }
}
