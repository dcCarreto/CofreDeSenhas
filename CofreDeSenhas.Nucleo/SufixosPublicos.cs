namespace CofreDeSenhas.Nucleo;

internal static class SufixosPublicos
{
    private static readonly HashSet<string> Compostos = new(StringComparer.Ordinal)
    {
        "co.uk", "org.uk", "me.uk", "ltd.uk", "plc.uk", "net.uk", "sch.uk",
        "ac.uk", "gov.uk", "nhs.uk", "police.uk", "mod.uk",
        "com.br", "net.br", "org.br", "gov.br", "edu.br", "art.br", "blog.br",
        "eco.br", "jus.br", "leg.br", "mil.br", "tv.br", "rec.br", "inf.br",
        "com.au", "net.au", "org.au", "edu.au", "gov.au", "asn.au", "id.au",
        "co.jp", "or.jp", "ne.jp", "ac.jp", "go.jp", "ad.jp", "ed.jp", "gr.jp", "lg.jp",
        "co.nz", "net.nz", "org.nz", "govt.nz", "ac.nz", "geek.nz", "school.nz",
        "co.za", "org.za", "net.za", "gov.za", "ac.za", "web.za",
        "co.in", "net.in", "org.in", "gen.in", "firm.in", "ind.in",
        "gov.in", "ac.in", "edu.in", "res.in",
        "co.kr", "or.kr", "ne.kr", "re.kr", "go.kr", "ac.kr", "pe.kr",
        "com.mx", "net.mx", "org.mx", "edu.mx", "gob.mx",
        "com.ar", "net.ar", "org.ar", "gob.ar", "edu.ar",
        "com.tr", "net.tr", "org.tr", "gov.tr", "edu.tr", "bel.tr", "web.tr",
        "com.cn", "net.cn", "org.cn", "gov.cn", "edu.cn", "ac.cn",
        "com.sg", "net.sg", "org.sg", "gov.sg", "edu.sg",
        "com.hk", "net.hk", "org.hk", "gov.hk", "edu.hk", "idv.hk",
        "co.il", "net.il", "org.il", "gov.il", "ac.il", "k12.il",
        "com.ru", "net.ru", "org.ru",
        "com.ua", "net.ua", "org.ua", "in.ua",
        "com.pl", "net.pl", "org.pl", "edu.pl", "gov.pl",
        "com.es", "org.es", "edu.es", "gob.es",
    };

    public static bool Contem(string doisUltimosRotulos) => Compostos.Contains(doisUltimosRotulos);
}
