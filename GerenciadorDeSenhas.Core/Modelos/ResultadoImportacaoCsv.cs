using System.Collections.Generic;

namespace GerenciadorDeSenhas.Modelos
{
    public class ResultadoImportacaoCsv
    {
        public List<SenhaExportada> Itens { get; set; } = new();
        public string FormatoDetectado { get; set; } = string.Empty;
        public int LinhasIgnoradas { get; set; }
    }
}
