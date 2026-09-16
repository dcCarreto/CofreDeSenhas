namespace GerenciadorDeSenhas.Modelos
{
    public class AnexoSenha
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string NomeArquivo { get; set; }
        public long TamanhoBytes { get; set; }
        public DateTime DataAdicao { get; set; } = DateTime.UtcNow;

        // Tumba de exclusão pra sincronização por pasta compartilhada: remover um
        // anexo marca estes dois campos em vez de tirar a entrada da lista, senão a
        // mesclagem aditiva (por Id) resgataria de volta um anexo já apagado que outro
        // dispositivo ainda não sincronizou a remoção. NomeArquivo/TamanhoBytes ficam
        // como estavam (não precisam ser apagados) — quem decide se mostra ou não é
        // sempre o filtro por Excluido, nunca o conteúdo dos outros campos.
        public bool Excluido { get; set; }
        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
    }
}
