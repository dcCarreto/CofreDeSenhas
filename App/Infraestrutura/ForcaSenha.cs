using System.Text.RegularExpressions;

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
            if (palavras >= 4)
                forca = Math.Max(forca, Math.Min(4, palavras - 1));

            return Math.Min(forca, 4);
        }
    }
}
