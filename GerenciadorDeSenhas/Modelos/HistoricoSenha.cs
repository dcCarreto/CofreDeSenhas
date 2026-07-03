using System;

namespace GerenciadorDeSenhas.Modelos
{
    public class HistoricoSenha
    {
        public string SenhaHash { get; set; } = string.Empty;

        public DateTime DataAlteracao { get; set; } = DateTime.UtcNow;
    }
}
