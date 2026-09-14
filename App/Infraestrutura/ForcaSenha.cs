using Avalonia.Media;

namespace CofreDeSenhas
{
    public static class ForcaSenha
    {
        public static int Calcular(string senha)
        {
            if (string.IsNullOrEmpty(senha)) return 0;

            bool temMaiuscula = false, temMinuscula = false, temDigito = false, temNaoAlfanumerico = false;
            int palavras = 0, tamanhoParte = 0;
            bool parteSoLetras = true;

            foreach (var c in senha)
            {
                if (c >= 'A' && c <= 'Z') temMaiuscula = true;
                else if (c >= 'a' && c <= 'z') temMinuscula = true;
                else if (c >= '0' && c <= '9') temDigito = true;
                else temNaoAlfanumerico = true;

                if (c is '-' or '_' or '.' or ' ')
                {
                    if (tamanhoParte >= 3 && parteSoLetras) palavras++;
                    tamanhoParte = 0;
                    parteSoLetras = true;
                }
                else
                {
                    tamanhoParte++;
                    if (!char.IsLetter(c)) parteSoLetras = false;
                }
            }
            if (tamanhoParte >= 3 && parteSoLetras) palavras++;

            int forca = 0;
            if (senha.Length >= 8) forca++;
            if (senha.Length >= 12) forca++;
            if (temMaiuscula && temMinuscula) forca++;
            if (temDigito) forca++;

            bool ehPassphrase = palavras >= 4;
            if (ehPassphrase)
                forca = Math.Max(forca, Math.Min(4, palavras - 1));

            // Sem isto, comprimento + maiúscula/minúscula + dígito já batem o teto
            // sozinhos e o nível nunca reflete se a senha tem algum símbolo — a mesma
            // senha aparecia "Excelente" aqui e "Fraca" no Relatório de Segurança
            // (ServicoAuditoriaSenha.SenhaForteParaAuditoria exige símbolo pra senhas
            // que não são passphrase). Passphrase continua isenta, mesmo critério do
            // relatório (EhPassphraseForte).
            if (!ehPassphrase && forca >= 4 && !temNaoAlfanumerico)
                forca = 3;

            return Math.Min(forca, 4);
        }

        public static (string Texto, Color Cor) Descrever(int nivel) => nivel switch
        {
            1 => (Idioma.Texto("Generator.StrengthWeak"), Tema.StrengthWeak),
            2 => (Idioma.Texto("Generator.StrengthMedium"), Tema.StrengthMedium),
            3 => (Idioma.Texto("Generator.StrengthStrong"), Tema.StrengthStrong),
            4 => (Idioma.Texto("Generator.StrengthExcellent"), Tema.StrengthExcellent),
            _ => ("—", Tema.TextSecondary)
        };
    }
}
