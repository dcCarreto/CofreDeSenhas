using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public static class MesclaSincronizacao
    {
        // Sinal de que uma linha do banco é uma tumba de exclusão definitiva
        // (RepositorioSenhaBanco.EsvaziarLinhaAsync) e não um item de verdade — usado
        // tanto pra pular a mesclagem aditiva de etiquetas/histórico/códigos quanto
        // pra remover o item por completo do lado local em vez de sobrescrevê-lo com
        // uma cópia em branco sentada na lixeira do outro dispositivo.
        public static bool EhTumbaDeExclusaoDefinitiva(Senha senha) =>
            senha.NaLixeira && senha.NomeServico.Length == 0 && senha.Usuario.Length == 0 && senha.SenhaHash.Length == 0;

        // Mesmo sinal que a sobrecarga acima, mas pro lado da pasta de sincronização —
        // que não tem uma "linha de banco" persistente pra esvaziar, então a tumba só
        // existe dentro do próprio sincronizacao.dat (ver JanelaPrincipal,
        // PublicarTumbasNaPastaDeSincronizacaoAsync, chamada no momento da exclusão
        // definitiva, já que depois disso o item não deixa nenhum rastro local).
        public static bool EhTumbaDeExclusaoDefinitiva(SenhaExportada item) =>
            item.NaLixeira && item.NomeServico.Length == 0 && item.Usuario.Length == 0 && item.Senha.Length == 0;

        public static List<Senha> MesclarSenhas(IReadOnlyList<Senha> locais, IReadOnlyList<Senha> remotos)
        {
            var resultado = new Dictionary<Guid, Senha>();
            foreach (var item in locais)
                resultado[item.Id] = item;

            foreach (var remoto in remotos)
            {
                if (!resultado.TryGetValue(remoto.Id, out var local))
                {
                    resultado[remoto.Id] = remoto;
                    continue;
                }

                var vencedor = remoto.DataAtualizacao > local.DataAtualizacao ? remoto : local;
                var perdedor = ReferenceEquals(vencedor, remoto) ? local : remoto;
                var vencedorEhRemoto = ReferenceEquals(vencedor, remoto);

                // Sem este corte, a mesclagem aditiva de baixo resgataria de volta
                // etiquetas/histórico/códigos do lado perdedor — e histórico pode conter
                // justamente as senhas antigas que a exclusão definitiva deveria apagar.
                if (vencedorEhRemoto && EhTumbaDeExclusaoDefinitiva(vencedor))
                {
                    resultado[remoto.Id] = vencedor;
                    continue;
                }

                var etiquetas = MesclarListaAditiva(vencedor.Etiquetas, perdedor.Etiquetas);
                var historico = MesclarHistorico(vencedor.Historico, perdedor.Historico);
                var codigosRecuperacao = MesclarCodigosRecuperacaoAditiva(vencedor.CodigosRecuperacao, perdedor.CodigosRecuperacao);

                // Anexos nunca têm coluna no banco (decisão de produto: ficam só no
                // dispositivo que os criou) — o objeto remoto sempre chega com Anexos
                // vazio, então usar ele como vencedor sem ajuste apagaria os anexos que
                // o lado local já tinha. Local sempre vence pra esse campo específico.
                if (etiquetas.Count != vencedor.Etiquetas.Count || historico.Count != vencedor.Historico.Count
                    || codigosRecuperacao.Count != vencedor.CodigosRecuperacao.Count || vencedorEhRemoto)
                    vencedor = ComListasMescladas(vencedor, etiquetas, historico, codigosRecuperacao, local.Anexos);

                resultado[remoto.Id] = vencedor;
            }

            return resultado.Values.ToList();
        }

        public static List<SenhaExportada> MesclarSenhasExportadas(
            IReadOnlyList<SenhaExportada> locais, IReadOnlyList<SenhaExportada> remotos)
        {
            var resultado = new Dictionary<Guid, SenhaExportada>();
            foreach (var item in locais)
                resultado[item.Id] = item;

            foreach (var remoto in remotos)
            {
                if (!resultado.TryGetValue(remoto.Id, out var local))
                {
                    resultado[remoto.Id] = remoto;
                    continue;
                }

                var vencedor = remoto.DataAtualizacao > local.DataAtualizacao ? remoto : local;
                var perdedor = ReferenceEquals(vencedor, remoto) ? local : remoto;
                var vencedorEhRemoto = ReferenceEquals(vencedor, remoto);

                // Mesmo corte que MesclarSenhas já faz pro banco: sem isto, a mesclagem
                // aditiva de baixo resgataria de volta etiquetas/histórico/códigos do
                // lado perdedor por cima de uma tumba.
                if (vencedorEhRemoto && EhTumbaDeExclusaoDefinitiva(vencedor))
                {
                    resultado[remoto.Id] = vencedor;
                    continue;
                }

                var etiquetas = MesclarListaAditiva(vencedor.Etiquetas, perdedor.Etiquetas);
                var historico = MesclarHistoricoExportado(vencedor.Historico, perdedor.Historico);
                var codigosRecuperacao = MesclarCodigosRecuperacaoExportadaAditiva(vencedor.CodigosRecuperacao, perdedor.CodigosRecuperacao);

                if (etiquetas.Count != vencedor.Etiquetas.Count || historico.Count != vencedor.Historico.Count
                    || codigosRecuperacao.Count != vencedor.CodigosRecuperacao.Count)
                    vencedor = ComListasMescladas(vencedor, etiquetas, historico, codigosRecuperacao);

                resultado[remoto.Id] = vencedor;
            }

            return resultado.Values.ToList();
        }

        private static List<string> MesclarListaAditiva(List<string> vencedora, List<string> perdedora)
        {
            var resultado = new List<string>(vencedora);
            var existentes = new HashSet<string>(vencedora, StringComparer.OrdinalIgnoreCase);
            foreach (var item in perdedora)
                if (existentes.Add(item))
                    resultado.Add(item);
            return resultado;
        }

        // Mesclagem aditiva genérica: mantém tudo do vencedor e acrescenta do perdedor
        // só o que não colide na chave de dedup — mesmo formato pros quatro pares
        // Senha/SenhaExportada (histórico e códigos de recuperação), que só diferem no
        // tipo do item e em qual campo identifica uma entrada como "a mesma".
        private static List<T> MesclarListaAditivaGenerica<T, TChave>(
            List<T> vencedor, List<T> perdedor, Func<T, TChave> chaveDedup, Func<T, DateTime>? ordenarPor = null)
        {
            var existentes = new HashSet<TChave>(vencedor.Select(chaveDedup));
            var resultado = new List<T>(vencedor);
            foreach (var item in perdedor)
                if (existentes.Add(chaveDedup(item)))
                    resultado.Add(item);
            return ordenarPor != null ? resultado.OrderBy(ordenarPor).ToList() : resultado;
        }

        private static List<HistoricoSenha> MesclarHistorico(List<HistoricoSenha> vencedor, List<HistoricoSenha> perdedor) =>
            MesclarListaAditivaGenerica(vencedor, perdedor, h => (h.SenhaHash, h.DataAlteracao), h => h.DataAlteracao);

        private static List<CodigoRecuperacao> MesclarCodigosRecuperacaoAditiva(List<CodigoRecuperacao> vencedor, List<CodigoRecuperacao> perdedor) =>
            MesclarListaAditivaGenerica(vencedor, perdedor, c => c.Id);

        private static List<CodigoRecuperacaoExportado> MesclarCodigosRecuperacaoExportadaAditiva(List<CodigoRecuperacaoExportado> vencedor, List<CodigoRecuperacaoExportado> perdedor) =>
            MesclarListaAditivaGenerica(vencedor, perdedor, c => c.Codigo);

        private static List<HistoricoSenhaExportada> MesclarHistoricoExportado(
            List<HistoricoSenhaExportada> vencedor, List<HistoricoSenhaExportada> perdedor) =>
            MesclarListaAditivaGenerica(vencedor, perdedor, h => (h.Senha, h.DataAlteracao), h => h.DataAlteracao);

        private static Senha ComListasMescladas(Senha origem, List<string> etiquetas, List<HistoricoSenha> historico, List<CodigoRecuperacao> codigosRecuperacao, List<AnexoSenha> anexos) => new()
        {
            Id = origem.Id,
            NomeServico = origem.NomeServico,
            Usuario = origem.Usuario,
            SenhaHash = origem.SenhaHash,
            Url = origem.Url,
            Categoria = origem.Categoria,
            Etiquetas = etiquetas,
            Notas = origem.Notas,
            Tipo = origem.Tipo,
            CamposExtras = origem.CamposExtras,
            TotpSegredo = origem.TotpSegredo,
            Historico = historico,
            CodigosRecuperacao = codigosRecuperacao,
            Anexos = anexos,
            Favorito = origem.Favorito,
            Fixado = origem.Fixado,
            NaLixeira = origem.NaLixeira,
            DataExclusao = origem.DataExclusao,
            DataCriacao = origem.DataCriacao,
            DataAtualizacao = origem.DataAtualizacao,
            DataUltimaCopiaSenha = origem.DataUltimaCopiaSenha,
            DataUltimaCopiaUsuario = origem.DataUltimaCopiaUsuario,
            DataUltimaCopiaTotp = origem.DataUltimaCopiaTotp
        };

        private static SenhaExportada ComListasMescladas(
            SenhaExportada origem, List<string> etiquetas, List<HistoricoSenhaExportada> historico, List<CodigoRecuperacaoExportado> codigosRecuperacao) => new()
        {
            Id = origem.Id,
            NomeServico = origem.NomeServico,
            Usuario = origem.Usuario,
            Senha = origem.Senha,
            Url = origem.Url,
            Categoria = origem.Categoria,
            Etiquetas = etiquetas,
            Notas = origem.Notas,
            Tipo = origem.Tipo,
            CamposExtras = origem.CamposExtras,
            TotpSegredo = origem.TotpSegredo,
            Historico = historico,
            CodigosRecuperacao = codigosRecuperacao,
            Anexos = origem.Anexos,
            Favorito = origem.Favorito,
            Fixado = origem.Fixado,
            NaLixeira = origem.NaLixeira,
            DataExclusao = origem.DataExclusao,
            DataCriacao = origem.DataCriacao,
            DataAtualizacao = origem.DataAtualizacao
        };
    }
}
