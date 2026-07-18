using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas
{
    internal readonly record struct CampoExtraDefinicao(string Chave, string ChaveRotulo)
    {
        public string Rotulo => Idioma.Texto(ChaveRotulo);
    }

    internal static class TemplatesCredencial
    {
        private static readonly TipoCredencial[] Tipos =
        {
            TipoCredencial.Login,
            TipoCredencial.Cartao,
            TipoCredencial.ChaveLicenca,
            TipoCredencial.WiFi,
            TipoCredencial.Servidor,
            TipoCredencial.BancoDados,
            TipoCredencial.DocumentoSeguro
        };

        public static string[] Rotulos => Tipos.Select(Idioma.RotuloTipoCredencial).ToArray();

        public static TipoCredencial ObterTipo(int indiceCombo) =>
            indiceCombo >= 0 && indiceCombo < Tipos.Length ? Tipos[indiceCombo] : TipoCredencial.Login;

        public static int ObterIndice(TipoCredencial tipo)
        {
            var indice = Array.IndexOf(Tipos, tipo);
            return indice >= 0 ? indice : 0;
        }

        public static string RotuloUsuario(TipoCredencial tipo) => tipo switch
        {
            TipoCredencial.Cartao => Idioma.Texto("CredField.CardHolder"),
            TipoCredencial.ChaveLicenca => Idioma.Texto("CredField.Product"),
            TipoCredencial.WiFi => Idioma.Texto("CredField.Ssid"),
            TipoCredencial.DocumentoSeguro => Idioma.Texto("CredField.DocTitle"),
            _ => Idioma.Texto("Entry.UserEmail")
        };

        public static string RotuloSenha(TipoCredencial tipo) => tipo switch
        {
            TipoCredencial.Cartao => Idioma.Texto("CredField.CardNumber"),
            TipoCredencial.ChaveLicenca => Idioma.Texto("CredField.LicenseKey"),
            TipoCredencial.WiFi => Idioma.Texto("CredField.WifiPassword"),
            TipoCredencial.DocumentoSeguro => Idioma.Texto("CredField.DocContent"),
            _ => Idioma.Texto("Entry.Password")
        };

        public static IReadOnlyList<CampoExtraDefinicao> CamposExtras(TipoCredencial tipo) => tipo switch
        {
            TipoCredencial.Cartao => new[]
            {
                new CampoExtraDefinicao("validade", "CredField.Validade"),
                new CampoExtraDefinicao("cvv", "CredField.Cvv"),
                new CampoExtraDefinicao("bandeira", "CredField.Bandeira")
            },
            TipoCredencial.WiFi => new[]
            {
                new CampoExtraDefinicao("seguranca", "CredField.Seguranca"),
                new CampoExtraDefinicao("banda", "CredField.Banda")
            },
            TipoCredencial.Servidor => new[]
            {
                new CampoExtraDefinicao("host", "CredField.Host"),
                new CampoExtraDefinicao("porta", "CredField.Porta"),
                new CampoExtraDefinicao("protocolo", "CredField.Protocolo")
            },
            TipoCredencial.BancoDados => new[]
            {
                new CampoExtraDefinicao("host", "CredField.Host"),
                new CampoExtraDefinicao("porta", "CredField.Porta"),
                new CampoExtraDefinicao("nome_banco", "CredField.NomeBanco"),
                new CampoExtraDefinicao("motor", "CredField.Motor")
            },
            _ => Array.Empty<CampoExtraDefinicao>()
        };
    }
}
