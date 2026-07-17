using System;

namespace GerenciadorDeSenhas.Modelos
{
    public class CodigoRecuperacao
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Codigo { get; set; } = string.Empty;

        public bool Usado { get; set; }
    }
}
