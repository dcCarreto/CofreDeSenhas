using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using GerenciadorDeSenhas.Servicos;

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
        public string? Notas { get; set; }

        public bool Favorito { get; set; }

        public byte[] IV { get; set; } = new byte[EspecificacaoCriptografica.TamanhoIvAesGcm];
        public byte[] AuthTag { get; set; } = new byte[EspecificacaoCriptografica.TamanhoTagAesGcm];

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? CamposExtras { get; set; }
    }
}
