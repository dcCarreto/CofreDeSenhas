namespace GerenciadorDeSenhas.Servicos
{
    public interface IServicoCriptografia
    {
        string Criptografar(string plaintext);
        string Descriptografar(string ciphertext);
        byte[] CriptografarBytes(byte[] plaintext);
        byte[] DescriptografarBytes(byte[] ciphertext);
        string CalcularHmacIntegridade(string dados);
        bool VerificarHmacIntegridade(string dados, string? hmacBase64);
        void ZerarChave();
    }
}
