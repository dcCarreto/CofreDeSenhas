using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public static class MesclaSincronizacao
    {
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

                var etiquetas = MesclarListaAditiva(vencedor.Etiquetas, perdedor.Etiquetas);
                var historico = MesclarHistorico(vencedor.Historico, perdedor.Historico);

                if (etiquetas.Count != vencedor.Etiquetas.Count || historico.Count != vencedor.Historico.Count)
                    vencedor = ComListasMescladas(vencedor, etiquetas, historico);

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

                var etiquetas = MesclarListaAditiva(vencedor.Etiquetas, perdedor.Etiquetas);
                var historico = MesclarHistoricoExportado(vencedor.Historico, perdedor.Historico);

                if (etiquetas.Count != vencedor.Etiquetas.Count || historico.Count != vencedor.Historico.Count)
                    vencedor = ComListasMescladas(vencedor, etiquetas, historico);

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

        private static List<HistoricoSenha> MesclarHistorico(List<HistoricoSenha> vencedor, List<HistoricoSenha> perdedor)
        {
            var existentes = new HashSet<(string, DateTime)>(vencedor.Select(h => (h.SenhaHash, h.DataAlteracao)));
            var resultado = new List<HistoricoSenha>(vencedor);
            foreach (var item in perdedor)
                if (existentes.Add((item.SenhaHash, item.DataAlteracao)))
                    resultado.Add(item);
            return resultado.OrderBy(h => h.DataAlteracao).ToList();
        }

        private static List<HistoricoSenhaExportada> MesclarHistoricoExportado(
            List<HistoricoSenhaExportada> vencedor, List<HistoricoSenhaExportada> perdedor)
        {
            var existentes = new HashSet<(string, DateTime)>(vencedor.Select(h => (h.Senha, h.DataAlteracao)));
            var resultado = new List<HistoricoSenhaExportada>(vencedor);
            foreach (var item in perdedor)
                if (existentes.Add((item.Senha, item.DataAlteracao)))
                    resultado.Add(item);
            return resultado.OrderBy(h => h.DataAlteracao).ToList();
        }

        private static Senha ComListasMescladas(Senha origem, List<string> etiquetas, List<HistoricoSenha> historico) => new()
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
            CodigosRecuperacao = origem.CodigosRecuperacao,
            Anexos = origem.Anexos,
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
            SenhaExportada origem, List<string> etiquetas, List<HistoricoSenhaExportada> historico) => new()
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
            CodigosRecuperacao = origem.CodigosRecuperacao,
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
