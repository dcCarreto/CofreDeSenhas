namespace GerenciadorDeSenhas.Modelos
{
    public class SenhaExportada
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NomeServico { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? Url { get; set; }
        public Categoria Categoria { get; set; }
        public List<string> Etiquetas { get; set; } = new();
        public string? Notas { get; set; }
        public TipoCredencial Tipo { get; set; } = TipoCredencial.Login;
        public Dictionary<string, string> CamposExtras { get; set; } = new();
        public string? TotpSegredo { get; set; }
        public List<HistoricoSenhaExportada> Historico { get; set; } = new();
        public List<CodigoRecuperacaoExportado> CodigosRecuperacao { get; set; } = new();
        public List<AnexoExportado> Anexos { get; set; } = new();
        public bool Favorito { get; set; }
        public bool Fixado { get; set; }
        public bool NaLixeira { get; set; }
        public DateTime? DataExclusao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }

    public class HistoricoSenhaExportada
    {
        public string Senha { get; set; } = string.Empty;
        public DateTime DataAlteracao { get; set; }
    }

    public class CodigoRecuperacaoExportado
    {
        public string Codigo { get; set; } = string.Empty;
        public bool Usado { get; set; }
    }

    public class AnexoExportado
    {
        // Sem Id em versões antigas de .gsenhas (o formato de exportação nunca
        // precisou identificar um anexo entre execuções) — um valor novo aleatório
        // aqui é inofensivo pra importação, que só recria os arquivos do zero. Já
        // pra sincronização por pasta, o Id é o que liga esta entrada ao mesmo anexo
        // entre dispositivos (mesmo papel que Senha.Id já tem pra credenciais).
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NomeArquivo { get; set; } = string.Empty;
        public string ConteudoBase64 { get; set; } = string.Empty;

        // Só usados pela sincronização por pasta (exportação .gsenhas não lida com
        // tumba: um anexo excluído simplesmente não entra na lista exportada, e
        // ConteudoBase64 já carrega o arquivo inteiro ali, então TamanhoBytes seria
        // redundante). Sem os bytes aqui — a sincronização é sob demanda: só o
        // metadado viaja pelo sincronizacao.dat, o conteúdo é buscado à parte
        // (ServicoSincronizacao.BaixarAnexoAsync) quando alguém abre o anexo.
        public long TamanhoBytes { get; set; }
        public DateTime DataAdicao { get; set; } = DateTime.UtcNow;
        public bool Excluido { get; set; }
        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
    }
}
