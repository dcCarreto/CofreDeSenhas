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
        public bool ReconciliacaoInicialConcluida { get; set; }
        public bool ExigirCertificadoValido { get; set; }
        public bool ExigirIntegridade { get; set; }
    }

    public sealed class PerfilSincronizacao
    {
        public string Pasta { get; set; } = "";
        public string Salt { get; set; } = "";
        public string? Kdf { get; set; }
        public int Iteracoes { get; set; }
        public int? MemoriaKb { get; set; }
        public int? Paralelismo { get; set; }
        public int FrequenciaMinutos { get; set; } = 15;
        public DateTime? UltimaSincronizacao { get; set; }
    }

    public static class Preferencias
    {
        private const int MinutosBloqueioPadrao = 5;
        private const double EscalaInterfacePadrao = 1.0;
        private const int SegundosLimpezaClipboardPadrao = 30;
        private const string FrequenciaBackupPadrao = "Semanal";
        private const int MaximoBackupsPadrao = 10;

        private class Dados
        {
            public PerfilBanco? UltimoBanco { get; set; }
            public int MinutosBloqueio { get; set; } = MinutosBloqueioPadrao;
            public string? Idioma { get; set; }
            public string? Daltonismo { get; set; }
            public bool AltoContraste { get; set; }
            public double EscalaInterface { get; set; } = EscalaInterfacePadrao;
            public bool ReduzirAnimacoes { get; set; }
            public bool LeitorTela { get; set; }
            public bool IconesOnline { get; set; }
            public int SegundosLimpezaClipboard { get; set; } = SegundosLimpezaClipboardPadrao;
            public string FrequenciaBackup { get; set; } = FrequenciaBackupPadrao;
            public int MaximoBackups { get; set; } = MaximoBackupsPadrao;
            public bool RegistrarHistoricoUso { get; set; } = true;
            public bool VerificarAtualizacoes { get; set; } = true;
            public string? VersaoDispensada { get; set; }
            public PerfilSincronizacao? Sincronizacao { get; set; }
        }

        private static readonly string _caminho = Path.Combine(CaminhosApp.PastaDados, "config.json");

        public static PerfilBanco? UltimoBanco { get; set; }
        public static int MinutosBloqueio { get; set; } = MinutosBloqueioPadrao;
        public static string? Idioma { get; set; }
        public static string? Daltonismo { get; set; }
        public static bool AltoContraste { get; set; }
        public static double EscalaInterface { get; set; } = EscalaInterfacePadrao;
        public static bool ReduzirAnimacoes { get; set; }
        public static bool LeitorTela { get; set; }
        public static bool IconesOnline { get; set; }
        public static int SegundosLimpezaClipboard { get; set; } = SegundosLimpezaClipboardPadrao;
        public static string FrequenciaBackup { get; set; } = FrequenciaBackupPadrao;
        public static int MaximoBackups { get; set; } = MaximoBackupsPadrao;
        public static bool RegistrarHistoricoUso { get; set; } = true;
        public static bool VerificarAtualizacoes { get; set; } = true;
        public static string? VersaoDispensada { get; set; }
        public static PerfilSincronizacao? Sincronizacao { get; set; }

        public static GerenciadorDeSenhas.Servicos.FrequenciaBackup FrequenciaBackupAtual =>
            Enum.TryParse<GerenciadorDeSenhas.Servicos.FrequenciaBackup>(FrequenciaBackup, out var frequencia)
                ? frequencia
                : GerenciadorDeSenhas.Servicos.FrequenciaBackup.Semanal;

        public static void Carregar()
        {
            try
            {
                if (File.Exists(_caminho))
                {
                    var d = JsonSerializer.Deserialize<Dados>(File.ReadAllText(_caminho));
                    if (d != null)
                    {
                        UltimoBanco = d.UltimoBanco;
                        MinutosBloqueio = d.MinutosBloqueio;
                        Idioma = d.Idioma;
                        Daltonismo = d.Daltonismo;
                        AltoContraste = d.AltoContraste;
                        EscalaInterface = d.EscalaInterface <= 0 ? EscalaInterfacePadrao : d.EscalaInterface;
                        ReduzirAnimacoes = d.ReduzirAnimacoes;
                        LeitorTela = d.LeitorTela;
                        IconesOnline = d.IconesOnline;
                        SegundosLimpezaClipboard = d.SegundosLimpezaClipboard;
                        FrequenciaBackup = string.IsNullOrEmpty(d.FrequenciaBackup) ? FrequenciaBackupPadrao : d.FrequenciaBackup;
                        MaximoBackups = d.MaximoBackups <= 0 ? MaximoBackupsPadrao : d.MaximoBackups;
                        RegistrarHistoricoUso = d.RegistrarHistoricoUso;
                        VerificarAtualizacoes = d.VerificarAtualizacoes;
                        VersaoDispensada = d.VersaoDispensada;
                        Sincronizacao = d.Sincronizacao;
                    }
                }
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "Preferencias.Carregar");
            }
        }

        public static void Salvar()
        {
            try
            {
                var dir = Path.GetDirectoryName(_caminho)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_caminho, JsonSerializer.Serialize(new Dados { UltimoBanco = UltimoBanco, MinutosBloqueio = MinutosBloqueio, Idioma = Idioma, Daltonismo = Daltonismo, AltoContraste = AltoContraste, EscalaInterface = EscalaInterface, ReduzirAnimacoes = ReduzirAnimacoes, LeitorTela = LeitorTela, IconesOnline = IconesOnline, SegundosLimpezaClipboard = SegundosLimpezaClipboard, FrequenciaBackup = FrequenciaBackup, MaximoBackups = MaximoBackups, RegistrarHistoricoUso = RegistrarHistoricoUso, VerificarAtualizacoes = VerificarAtualizacoes, VersaoDispensada = VersaoDispensada, Sincronizacao = Sincronizacao }));
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "Preferencias.Salvar");
            }
        }
    }
}
