namespace GerenciadorDeSenhas.Servicos
{
    public static class EspecificacaoCriptografica
    {
        public const int TamanhoSalt = 16;
        public const int TamanhoChave = 32;
        public const int IteracoesPbkdf2 = 100_000;
        public const int TamanhoVerificador = 32;
        public const int TamanhoIvAesGcm = 12;
        public const int TamanhoTagAesGcm = 16;
        public const string Kdf = "PBKDF2-SHA256";
        public const string Criptografia = "AES-256-GCM";
        public const string FormatoCofre = "aes-256-gcm-base64";

        public static int TamanhoMinimoAuth => TamanhoSalt + TamanhoVerificador;

        public static int TamanhoMinimoPayloadCriptografado => TamanhoIvAesGcm + TamanhoTagAesGcm;
    }
}
