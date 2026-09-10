using System;

namespace GerenciadorDeSenhas.Servicos
{
    public static class ProtecaoKdf
    {
        public const int MemoriaKbMaxima = 1_048_576;
        public const int IteracoesArgonMaximas = 64;
        public const int ParalelismoMaximo = 16;
        public const int IteracoesPbkdf2Maximas = 10_000_000;

        public static bool Argon2idDentroDoLimite(int iteracoes, int memoriaKb, int paralelismo) =>
            iteracoes is >= 1 and <= IteracoesArgonMaximas &&
            paralelismo is >= 1 and <= ParalelismoMaximo &&
            memoriaKb >= 8 && memoriaKb <= MemoriaKbMaxima;

        public static bool Pbkdf2DentroDoLimite(int iteracoes) =>
            iteracoes is >= 1 and <= IteracoesPbkdf2Maximas;

        public static void GarantirArgon2id(int iteracoes, int memoriaKb, int paralelismo)
        {
            if (!Argon2idDentroDoLimite(iteracoes, memoriaKb, paralelismo))
                throw new ArgumentOutOfRangeException(nameof(memoriaKb),
                    "Parâmetros de derivação Argon2id fora dos limites aceitos.");
        }

        public static void GarantirPbkdf2(int iteracoes)
        {
            if (!Pbkdf2DentroDoLimite(iteracoes))
                throw new ArgumentOutOfRangeException(nameof(iteracoes),
                    "Número de iterações PBKDF2 fora dos limites aceitos.");
        }
    }
}
