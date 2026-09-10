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
        private const int ColunasListaPadrao = (int)global::CofreDeSenhas.ColunasLista.Todas;

        private class Dados
        {
            public PerfilBanco? UltimoBanco { get; set; }
            public int MinutosBloqueio { get; set; } = MinutosBloqueioPadrao;
            public string? Idioma { get; set; }
            public string? ModoTema { get; set; }
            public string? CorDestaque { get; set; }
            public string? Densidade { get; set; }
            public string? LayoutDetalhe { get; set; }
            public string? OrdenacaoColuna { get; set; }
            public bool OrdenacaoDescendente { get; set; }
            public int ColunasLista { get; set; } = ColunasListaPadrao;
            public List<PerfilAparencia>? PerfisAparencia { get; set; }
            public string? Daltonismo { get; set; }
            public bool AltoContraste { get; set; }
            public string? NivelContraste { get; set; }
            public double EscalaInterface { get; set; } = EscalaInterfacePadrao;
            public bool EscalaAutomatica { get; set; }
            public bool ReduzirAnimacoes { get; set; }
            public string? NivelMovimento { get; set; }
            public string? FonteLeitura { get; set; }
            public string? EspacamentoTexto { get; set; }
            public string? VerbosidadeLeitor { get; set; }
            public bool SublinharLinks { get; set; }
            public bool FocoReforcado { get; set; }
            public bool AnunciarAcoes { get; set; }
            public bool AvisarAntesBloqueio { get; set; }
            public bool LeitorTela { get; set; }
            public bool IconesOnline { get; set; }
            public int SegundosLimpezaClipboard { get; set; } = SegundosLimpezaClipboardPadrao;
            public string FrequenciaBackup { get; set; } = FrequenciaBackupPadrao;
            public int MaximoBackups { get; set; } = MaximoBackupsPadrao;
            public bool RegistrarHistoricoUso { get; set; } = true;
            public bool VerificarAtualizacoes { get; set; }
            public string? VersaoDispensada { get; set; }
            public PerfilSincronizacao? Sincronizacao { get; set; }
        }

        private static readonly string _caminho = Path.Combine(CaminhosApp.PastaDados, "config.json");

        public static PerfilBanco? UltimoBanco { get; set; }
        public static int MinutosBloqueio { get; set; } = MinutosBloqueioPadrao;
        public static string? Idioma { get; set; }
        public static string? ModoTema { get; set; }
        public static string? CorDestaque { get; set; }
        public static string? Densidade { get; set; }
        public static string? LayoutDetalhe { get; set; }
        public static string? OrdenacaoColuna { get; set; }
        public static bool OrdenacaoDescendente { get; set; }
        public static int ColunasLista { get; set; } = ColunasListaPadrao;
        public static List<PerfilAparencia>? PerfisAparencia { get; set; }
        public static string? Daltonismo { get; set; }
        public static bool AltoContraste { get; set; }
        public static string? NivelContraste { get; set; }
        public static double EscalaInterface { get; set; } = EscalaInterfacePadrao;
        public static bool EscalaAutomatica { get; set; }
        public static bool ReduzirAnimacoes { get; set; }
        public static string? NivelMovimento { get; set; }
        public static string? FonteLeitura { get; set; }
        public static string? EspacamentoTexto { get; set; }
        public static string? VerbosidadeLeitor { get; set; }
        public static bool SublinharLinks { get; set; }
        public static bool FocoReforcado { get; set; }
        public static bool AnunciarAcoes { get; set; }
        public static bool AvisarAntesBloqueio { get; set; }
        public static bool LeitorTela { get; set; }
        public static bool IconesOnline { get; set; }
        public static int SegundosLimpezaClipboard { get; set; } = SegundosLimpezaClipboardPadrao;
        public static string FrequenciaBackup { get; set; } = FrequenciaBackupPadrao;
        public static int MaximoBackups { get; set; } = MaximoBackupsPadrao;
        public static bool RegistrarHistoricoUso { get; set; } = true;
        public static bool VerificarAtualizacoes { get; set; }
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
                        ModoTema = d.ModoTema;
                        CorDestaque = d.CorDestaque;
                        Densidade = d.Densidade;
                        LayoutDetalhe = d.LayoutDetalhe;
                        OrdenacaoColuna = d.OrdenacaoColuna;
                        OrdenacaoDescendente = d.OrdenacaoDescendente;
                        ColunasLista = d.ColunasLista;
                        PerfisAparencia = d.PerfisAparencia;
                        Daltonismo = d.Daltonismo;
                        AltoContraste = d.AltoContraste;
                        NivelContraste = d.NivelContraste;
                        EscalaInterface = d.EscalaInterface <= 0 ? EscalaInterfacePadrao : d.EscalaInterface;
                        EscalaAutomatica = d.EscalaAutomatica;
                        ReduzirAnimacoes = d.ReduzirAnimacoes;
                        NivelMovimento = d.NivelMovimento;
                        FonteLeitura = d.FonteLeitura;
                        EspacamentoTexto = d.EspacamentoTexto;
                        VerbosidadeLeitor = d.VerbosidadeLeitor;
                        SublinharLinks = d.SublinharLinks;
                        FocoReforcado = d.FocoReforcado;
                        AnunciarAcoes = d.AnunciarAcoes;
                        AvisarAntesBloqueio = d.AvisarAntesBloqueio;
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
                File.WriteAllText(_caminho, JsonSerializer.Serialize(new Dados { UltimoBanco = UltimoBanco, MinutosBloqueio = MinutosBloqueio, Idioma = Idioma, ModoTema = ModoTema, CorDestaque = CorDestaque, Densidade = Densidade, LayoutDetalhe = LayoutDetalhe, OrdenacaoColuna = OrdenacaoColuna, OrdenacaoDescendente = OrdenacaoDescendente, ColunasLista = ColunasLista, PerfisAparencia = PerfisAparencia, Daltonismo = Daltonismo, AltoContraste = AltoContraste, NivelContraste = NivelContraste, EscalaInterface = EscalaInterface, EscalaAutomatica = EscalaAutomatica, ReduzirAnimacoes = ReduzirAnimacoes, NivelMovimento = NivelMovimento, FonteLeitura = FonteLeitura, EspacamentoTexto = EspacamentoTexto, VerbosidadeLeitor = VerbosidadeLeitor, SublinharLinks = SublinharLinks, FocoReforcado = FocoReforcado, AnunciarAcoes = AnunciarAcoes, AvisarAntesBloqueio = AvisarAntesBloqueio, LeitorTela = LeitorTela, IconesOnline = IconesOnline, SegundosLimpezaClipboard = SegundosLimpezaClipboard, FrequenciaBackup = FrequenciaBackup, MaximoBackups = MaximoBackups, RegistrarHistoricoUso = RegistrarHistoricoUso, VerificarAtualizacoes = VerificarAtualizacoes, VersaoDispensada = VersaoDispensada, Sincronizacao = Sincronizacao }));
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "Preferencias.Salvar");
            }
        }
    }
}
