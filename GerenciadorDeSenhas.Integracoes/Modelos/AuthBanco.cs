namespace GerenciadorDeSenhas.Modelos
{
    public sealed record AuthBanco(byte[] Salt, byte[] Verificador, byte Kdf, int Custo, int MemoriaKb, int Paralelismo);
}
