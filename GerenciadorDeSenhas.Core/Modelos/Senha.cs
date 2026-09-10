using System;

namespace GerenciadorDeSenhas.Modelos
{
    public class Senha
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string NomeServico { get; set; }
        public required string Usuario { get; set; }
        public required string SenhaHash { get; set; }

        public string? Url { get; set; }
        public Categoria Categoria { get; set; }
        public List<string> Etiquetas { get; set; } = new();
        public string? Notas { get; set; }

        public TipoCredencial Tipo { get; set; } = TipoCredencial.Login;
        public Dictionary<string, string> CamposExtras { get; set; } = new();

        public string? TotpSegredo { get; set; }

        public List<HistoricoSenha> Historico { get; set; } = new();

        public List<CodigoRecuperacao> CodigosRecuperacao { get; set; } = new();

        public List<AnexoSenha> Anexos { get; set; } = new();

        public bool Favorito { get; set; }
        public bool Fixado { get; set; }

        public bool NaLixeira { get; set; }
        public DateTime? DataExclusao { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

        public DateTime? DataUltimaCopiaSenha { get; set; }
        public DateTime? DataUltimaCopiaUsuario { get; set; }
        public DateTime? DataUltimaCopiaTotp { get; set; }
    }
}
