using System.Runtime.InteropServices;
using System.Security.Cryptography;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class MemoriaTravadaTests
{
    [Fact]
    public void TravarEDestravar_BufferPinado_NaoLancaEDestravaEmSeguida()
    {
        var buffer = new byte[32];
        var pino = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            // Não afirmamos "true": num runner com RLIMIT_MEMLOCK apertado a trava
            // pode falhar, e isso é aceitável (melhor esforço). O contrato é: não
            // lançar, e Destravar ser seguro logo depois.
            MemoriaTravada.Travar(pino.AddrOfPinnedObject(), (nuint)buffer.Length);
            MemoriaTravada.Destravar(pino.AddrOfPinnedObject(), (nuint)buffer.Length);
        }
        finally
        {
            pino.Free();
        }
    }

    [Fact]
    public void ServicoCriptografia_ComTravaNaMemoria_CifraDecifraEZeraSemVazarOPino()
    {
        var chave = new byte[32];
        RandomNumberGenerator.Fill(chave);

        var cripto = new ServicoCriptografia(chave, travarNaMemoria: true);
        var claro = "senha muito secreta 123";
        var cifrado = cripto.Criptografar(claro);

        Assert.Equal(claro, cripto.Descriptografar(cifrado));

        cripto.ZerarChave();

        Assert.Throws<ObjectDisposedException>(() => cripto.Criptografar("x"));
        Assert.All(chave, b => Assert.Equal(0, b));
    }
}
