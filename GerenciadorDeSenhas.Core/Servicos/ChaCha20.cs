using System.Buffers.Binary;
using System.Numerics;

namespace GerenciadorDeSenhas.Servicos
{
    internal sealed class ChaCha20
    {
        private readonly uint[] _estado = new uint[16];
        private readonly byte[] _bloco = new byte[64];
        private int _posicao = 64;

        public ChaCha20(ReadOnlySpan<byte> chave, ReadOnlySpan<byte> nonce, uint contador = 0)
        {
            if (chave.Length != 32 || nonce.Length != 12)
                throw new ArgumentException("ChaCha20 exige chave de 32 bytes e nonce de 12 bytes.");

            _estado[0] = 0x61707865;
            _estado[1] = 0x3320646e;
            _estado[2] = 0x79622d32;
            _estado[3] = 0x6b206574;
            for (int i = 0; i < 8; i++)
                _estado[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(chave.Slice(i * 4));
            _estado[12] = contador;
            for (int i = 0; i < 3; i++)
                _estado[13 + i] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(i * 4));
        }

        public void Aplicar(Span<byte> dados)
        {
            for (int i = 0; i < dados.Length; i++)
            {
                if (_posicao == 64)
                {
                    GerarBloco();
                    _posicao = 0;
                }
                dados[i] ^= _bloco[_posicao++];
            }
        }

        private void GerarBloco()
        {
            Span<uint> x = stackalloc uint[16];
            _estado.CopyTo(x);
            for (int i = 0; i < 10; i++)
            {
                Quarto(x, 0, 4, 8, 12);
                Quarto(x, 1, 5, 9, 13);
                Quarto(x, 2, 6, 10, 14);
                Quarto(x, 3, 7, 11, 15);
                Quarto(x, 0, 5, 10, 15);
                Quarto(x, 1, 6, 11, 12);
                Quarto(x, 2, 7, 8, 13);
                Quarto(x, 3, 4, 9, 14);
            }
            for (int i = 0; i < 16; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(_bloco.AsSpan(i * 4), x[i] + _estado[i]);
            _estado[12]++;
        }

        private static void Quarto(Span<uint> x, int a, int b, int c, int d)
        {
            x[a] += x[b]; x[d] = BitOperations.RotateLeft(x[d] ^ x[a], 16);
            x[c] += x[d]; x[b] = BitOperations.RotateLeft(x[b] ^ x[c], 12);
            x[a] += x[b]; x[d] = BitOperations.RotateLeft(x[d] ^ x[a], 8);
            x[c] += x[d]; x[b] = BitOperations.RotateLeft(x[b] ^ x[c], 7);
        }
    }
}
