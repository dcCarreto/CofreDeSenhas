namespace GerenciadorDeSenhas.Servicos
{
    public interface IServicoCriptografia
    {
        string Criptografar(string plaintext);
        string Descriptografar(string ciphertext);
    }
}
