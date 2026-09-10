using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas.Cli
{
    internal static class Formatar
    {
        public static string Duracao(TimeSpan t)
        {
            if (t.TotalSeconds < 1) return "menos de 1 s";
            if (t.TotalSeconds < 60) return $"{(int)t.TotalSeconds} s";
            if (t.TotalMinutes < 60)
            {
                var s = t.Seconds;
                return s > 0 ? $"{(int)t.TotalMinutes} min {s} s" : $"{(int)t.TotalMinutes} min";
            }
            var min = t.Minutes;
            return min > 0 ? $"{(int)t.TotalHours} h {min} min" : $"{(int)t.TotalHours} h";
        }

        public static string CategoriaLabel(Categoria c) => c switch
        {
            Categoria.Work => "Trabalho",
            Categoria.Personal => "Pessoal",
            Categoria.Finance => "Finanças",
            Categoria.Social => "Social",
            _ => "Outro",
        };

        public static bool TentarCategoria(string bruto, out Categoria categoria)
        {
            switch (bruto.Trim().ToLowerInvariant())
            {
                case "trabalho": case "work": categoria = Categoria.Work; return true;
                case "pessoal": case "personal": categoria = Categoria.Personal; return true;
                case "financas": case "finanças": case "finance": categoria = Categoria.Finance; return true;
                case "social": categoria = Categoria.Social; return true;
                case "outro": case "other": categoria = Categoria.Other; return true;
                default: categoria = default; return false;
            }
        }

        public static string TipoLabel(TipoCredencial t) => t switch
        {
            TipoCredencial.Login => "Login",
            TipoCredencial.Cartao => "Cartão",
            TipoCredencial.ChaveLicenca => "Chave de licença",
            TipoCredencial.WiFi => "Wi-Fi",
            TipoCredencial.Servidor => "Servidor",
            TipoCredencial.BancoDados => "Banco de dados",
            _ => t.ToString(),
        };

        public static string Data(DateTime d) => d.ToLocalTime().ToString("yyyy-MM-dd");
    }
}
