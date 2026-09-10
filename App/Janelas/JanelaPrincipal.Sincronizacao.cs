using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CofreDeSenhas.Controles;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaPrincipal
    {
        private void AjustarTimerSincronizacao()
        {
            var perfil = Preferencias.Sincronizacao;
            if (perfil == null || perfil.FrequenciaMinutos <= 0 || _servicoSincronizacao == null)
            {
                _timerSincronizacao.Stop();
                return;
            }

            _timerSincronizacao.Interval = TimeSpan.FromMinutes(perfil.FrequenciaMinutos);
            _timerSincronizacao.Start();
        }

        private async void Sincronizacao_Click(object? sender, RoutedEventArgs e)
        {
            if (_repositorioLocal == null || _criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Sync.Title"), TipoMensagem.Aviso);
                return;
            }

            var dlg = new JanelaSincronizacao(_servicoSincronizacao,
                servico => _servicoSincronizacao = servico,
                () => SincronizarAsync(silencioso: false),
                () => _sincronizando,
                janela => AbrirDialogoAsync<bool>(janela),
                AjustarTimerSincronizacao);

            await AbrirDialogoAsync<bool>(dlg);
            AjustarTimerSincronizacao();
        }

        private async Task<bool> SincronizarAsync(bool silencioso)
        {
            if (_servicoSincronizacao == null || Preferencias.Sincronizacao is not { } perfil || _sincronizando)
                return false;

            _sincronizando = true;
            try
            {
                var caminho = Path.Combine(perfil.Pasta, ServicoSincronizacao.NomeArquivo);

                var locais = await ConstruirListaExportavelAsync();

                var remotas = await _servicoSincronizacao.LerAsync(caminho);
                var mescladas = ServicoSincronizacao.MesclarListas(locais, remotas);

                // Sem estas guardas, todo ciclo re-cifra e regrava o cofre e joga a rolagem pro topo.
                bool localMudou = !MesmoConteudoSync(locais, mescladas);
                bool remotoMudou = !MesmoConteudoSync(remotas, mescladas);

                if (localMudou)
                {
                    foreach (var item in mescladas)
                        await _servicoSenha.AplicarSincronizadoAsync(item);
                    await _servicoSenha.PersistirAsync();
                }

                if (remotoMudou)
                {
                    var salt = Convert.FromBase64String(perfil.Salt);
                    await _servicoSincronizacao.EscreverAsync(caminho, salt, perfil.Kdf, perfil.Iteracoes,
                        perfil.MemoriaKb, perfil.Paralelismo, mescladas);
                }

                perfil.UltimaSincronizacao = DateTime.UtcNow;
                Preferencias.Salvar();

                if (localMudou)
                    await CarregarSenhasAsync(silencioso);
                return true;
            }
            catch (Exception ex)
            {
                // Sem isto, uma pasta de sincronização com dado que quebra o merge
                // (ex.: sincronizacao.dat corrompido por outra versão do app) falha do
                // mesmo jeito silenciosamente em toda tentativa futura, sem nenhum
                // rastro pra investigar — silencioso=true (sync automática) já não
                // mostra diálogo nenhum ao usuário.
                Diagnostico.Registrar(ex, "Sincronizar");

                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Sync.Error"), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                return false;
            }
            finally
            {
                _sincronizando = false;
            }
        }

        private static readonly JsonSerializerOptions _opcoesAssinaturaSync = new();

        private static bool MesmoConteudoSync(List<SenhaExportada> a, List<SenhaExportada> b)
        {
            if (a.Count != b.Count)
                return false;

            using var ea = a.OrderBy(s => s.Id).GetEnumerator();
            using var eb = b.OrderBy(s => s.Id).GetEnumerator();
            while (ea.MoveNext() && eb.MoveNext())
            {
                if (ea.Current.Id != eb.Current.Id ||
                    JsonSerializer.Serialize(ea.Current, _opcoesAssinaturaSync) !=
                    JsonSerializer.Serialize(eb.Current, _opcoesAssinaturaSync))
                    return false;
            }
            return true;
        }

        private async Task<List<SenhaExportada>> ConstruirListaExportavelAsync()
        {
            var locais = new List<SenhaExportada>();
            var todasLocais = (await _servicoSenha.ListarTodosAsync()).Concat(await _servicoSenha.ListarLixeiraAsync());
            foreach (var s in todasLocais)
            {
                var plain = ObterSenhaPlain(s);
                if (plain == null)
                    continue;

                locais.Add(new SenhaExportada
                {
                    Id = s.Id,
                    NomeServico = s.NomeServico,
                    Usuario = s.Usuario,
                    Senha = plain,
                    Url = s.Url,
                    Categoria = s.Categoria,
                    Etiquetas = s.Etiquetas.ToList(),
                    Notas = s.Notas,
                    Tipo = s.Tipo,
                    CamposExtras = ObterCamposExtrasPlain(s),
                    TotpSegredo = ObterTotpPlain(s),
                    Historico = ObterHistoricoPlain(s),
                    CodigosRecuperacao = ObterCodigosRecuperacaoPlain(s),
                    Favorito = s.Favorito,
                    Fixado = s.Fixado,
                    NaLixeira = s.NaLixeira,
                    DataExclusao = s.DataExclusao,
                    DataCriacao = s.DataCriacao,
                    DataAtualizacao = s.DataAtualizacao
                });
            }
            return locais;
        }

        private async void ConflitosSincronizacao_Click(object? sender, RoutedEventArgs e)
        {
            if (_repositorioEspelhado == null)
                return;

            var dlg = new JanelaConflitosSincronizacao(_repositorioEspelhado.UltimosConflitos);
            await AbrirDialogoAsync<bool>(dlg);
        }

        // Entre a troca de senha mestra gravada em disco e o restart, a janela ainda
        // opera na chave antiga (ver AlterarSenhaMestra_Click). IsEnabled bloqueia a UI
        // de ponteiro — menus, botões, linhas, painel de detalhes; a flag bloqueia os
        // atalhos de teclado, que ainda percorrem a árvore de eventos com a janela
    }
}
