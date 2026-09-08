namespace CofreDeSenhas
{
    public sealed class PerfilAparencia
    {
        public string Nome { get; set; } = "";
        public string ModoTema { get; set; } = "Escuro";
        public string CorDestaque { get; set; } = "Ambar";
        public string Densidade { get; set; } = "Confortavel";
        public string LayoutDetalhe { get; set; } = "Lateral";
        public string Daltonismo { get; set; } = "Nenhum";
        public bool AltoContraste { get; set; }
        public double EscalaInterface { get; set; } = Acessibilidade.EscalaNormal;
        public bool ReduzirAnimacoes { get; set; }

        public bool MesmaAparencia(PerfilAparencia outro) =>
            string.Equals(ModoTema, outro.ModoTema, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(CorDestaque, outro.CorDestaque, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Densidade, outro.Densidade, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(LayoutDetalhe, outro.LayoutDetalhe, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Daltonismo, outro.Daltonismo, StringComparison.OrdinalIgnoreCase) &&
            AltoContraste == outro.AltoContraste &&
            Math.Abs(EscalaInterface - outro.EscalaInterface) < 0.001 &&
            ReduzirAnimacoes == outro.ReduzirAnimacoes;
    }

    public static class Aparencia
    {
        public static readonly IReadOnlyList<(string Id, PerfilAparencia Perfil)> Presets = new (string, PerfilAparencia)[]
        {
            ("ClassicoEscuro", new PerfilAparencia()),
            ("ClaroCompacto", new PerfilAparencia { ModoTema = "Claro", Densidade = "Compacto" }),
            ("AltoContraste", new PerfilAparencia { AltoContraste = true, EscalaInterface = Acessibilidade.EscalaGrande })
        };

        public static IReadOnlyList<PerfilAparencia> Salvos =>
            Preferencias.PerfisAparencia ?? new List<PerfilAparencia>();

        public static PerfilAparencia Capturar() => new()
        {
            ModoTema = Acessibilidade.Modo.ToString(),
            CorDestaque = Acessibilidade.Destaque.ToString(),
            Densidade = Acessibilidade.Densidade.ToString(),
            LayoutDetalhe = Acessibilidade.LayoutDetalhe.ToString(),
            Daltonismo = Acessibilidade.Daltonismo.ToString(),
            AltoContraste = Acessibilidade.AltoContraste,
            EscalaInterface = Acessibilidade.Escala,
            ReduzirAnimacoes = Acessibilidade.ReduzirAnimacoes
        };

        // Id do preset, nome do perfil salvo, ou null (= "Personalizado").
        public static string? IdentificacaoAtual()
        {
            var atual = Capturar();

            foreach (var (id, perfil) in Presets)
                if (atual.MesmaAparencia(perfil))
                    return id;

            foreach (var perfil in Salvos)
                if (atual.MesmaAparencia(perfil))
                    return perfil.Nome;

            return null;
        }

        public static void Aplicar(PerfilAparencia perfil)
        {
            Acessibilidade.AplicarPerfil(
                Enum.TryParse<ModoTema>(perfil.ModoTema, true, out var m) ? m : ModoTema.Escuro,
                Enum.TryParse<CorDestaque>(perfil.CorDestaque, true, out var c) ? c : CorDestaque.Ambar,
                Enum.TryParse<Densidade>(perfil.Densidade, true, out var d) ? d : Densidade.Confortavel,
                Enum.TryParse<LayoutDetalhe>(perfil.LayoutDetalhe, true, out var l) ? l : LayoutDetalhe.Lateral,
                Enum.TryParse<TipoDaltonismo>(perfil.Daltonismo, true, out var t) ? t : TipoDaltonismo.Nenhum,
                perfil.AltoContraste,
                perfil.EscalaInterface,
                perfil.ReduzirAnimacoes);

            Preferencias.ModoTema = Acessibilidade.Modo.ToString();
            Preferencias.CorDestaque = Acessibilidade.Destaque.ToString();
            Preferencias.Densidade = Acessibilidade.Densidade.ToString();
            Preferencias.LayoutDetalhe = Acessibilidade.LayoutDetalhe.ToString();
            Preferencias.Daltonismo = Acessibilidade.Daltonismo.ToString();
            Preferencias.AltoContraste = Acessibilidade.AltoContraste;
            Preferencias.EscalaInterface = Acessibilidade.Escala;
            Preferencias.ReduzirAnimacoes = Acessibilidade.ReduzirAnimacoes;
            Preferencias.Salvar();
        }

        public static void Salvar(string nome)
        {
            nome = nome.Trim();
            if (string.IsNullOrEmpty(nome))
                return;

            var lista = new List<PerfilAparencia>(Salvos);
            lista.RemoveAll(p => string.Equals(p.Nome, nome, StringComparison.OrdinalIgnoreCase));

            var perfil = Capturar();
            perfil.Nome = nome;
            lista.Add(perfil);

            Preferencias.PerfisAparencia = lista;
            Preferencias.Salvar();
        }

        public static void Excluir(string nome)
        {
            var lista = new List<PerfilAparencia>(Salvos);
            if (lista.RemoveAll(p => string.Equals(p.Nome, nome, StringComparison.OrdinalIgnoreCase)) == 0)
                return;

            Preferencias.PerfisAparencia = lista;
            Preferencias.Salvar();
        }
    }
}
