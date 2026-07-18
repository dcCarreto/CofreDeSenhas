namespace GerenciadorDeSenhas.Servicos
{
    public interface IServicoCriptografia
    {
        string Criptografar(string plaintext);
        string Descriptografar(string ciphertext);
        byte[] CriptografarBytes(byte[] plaintext);
        byte[] DescriptografarBytes(byte[] ciphertext);
        void ZerarChave();
    }
}
