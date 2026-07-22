namespace GerenciadorDeSenhas.Excecoes
{
    public interface ILocalizavel
    {
        string Chave { get; }
        object?[] Argumentos { get; }
    }
}
