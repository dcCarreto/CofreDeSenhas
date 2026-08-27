using System.Text.RegularExpressions;
using Avalonia.Media;

namespace CofreDeSenhas
{
    public static class ForcaSenha
    {
        public static int Calcular(string senha)
        {
            int forca = 0;
            if (string.IsNullOrEmpty(senha)) return 0;
            if (senha.Length >= 8) forca++;
            if (senha.Length >= 12) forca++;
            if (Regex.IsMatch(senha, "[A-Z]") && Regex.IsMatch(senha, "[a-z]")) forca++;
            if (Regex.IsMatch(senha, "[0-9]")) forca++;

            var partes = senha.Split(new[] { '-', '_', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int palavras = partes.Count(p => p.Length >= 3 && p.All(char.IsLetter));
            bool ehPassphrase = palavras >= 4;
            if (ehPassphrase)
                forca = Math.Max(forca, Math.Min(4, palavras - 1));

            // Sem isto, comprimento + maiúscula/minúscula + dígito já batem o teto
            // sozinhos e o nível nunca reflete se a senha tem algum símbolo — a mesma
            // senha aparecia "Excelente" aqui e "Fraca" no Relatório de Segurança
            // (ServicoAuditoriaSenha.SenhaForteParaAuditoria exige símbolo pra senhas
            // que não são passphrase). Passphrase continua isenta, mesmo critério do
            // relatório (EhPassphraseForte).
            if (!ehPassphrase && forca >= 4 && !Regex.IsMatch(senha, @"[^A-Za-z0-9]"))
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
