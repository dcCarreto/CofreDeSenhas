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
        private async void Exportar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var senhas = await _servicoSenha.ListarTodosAsync();
                if (senhas.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Message.ExportEmpty"),
                        Idioma.Texto("Common.Export"));
                    return;
                }

                var totalFiltrado = _senhasFiltradasAtuais.Count(s => !s.NaLixeira);
                var dlg = new JanelaSenhaExportacao(modoExportar: true, totalGeral: senhas.Count, totalFiltrado: totalFiltrado);
                if (!await AbrirDialogoAsync<bool>(dlg))
                    return;

                if (dlg.ExportarSomenteFiltrados)
                {
                    var idsFiltrados = new HashSet<Guid>(_senhasFiltradasAtuais.Where(s => !s.NaLixeira).Select(s => s.Id));
                    senhas = senhas.Where(s => idsFiltrados.Contains(s.Id)).ToList();
                }

                var arquivo = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = Idioma.Texto("ExportDialog.ExportTitle"),
                    SuggestedFileName = $"cofre-senhas-{DateTime.Now:yyyy-MM-dd}.gsenhas",
                    DefaultExtension = "gsenhas",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType(Idioma.Texto("Common.ExportedVaultFile")) { Patterns = new[] { "*.gsenhas" } },
                        new FilePickerFileType(Idioma.Texto("Common.AllFiles")) { Patterns = new[] { "*" } }
                    }
                });
                if (arquivo == null)
                    return;

                Scrim.Mostrar(this);
                MostrarProgresso("Export.Progress");
                List<SenhaExportada> itens;
                try
                {
                    itens = new List<SenhaExportada>();
                    for (int i = 0; i < senhas.Count; i++)
                    {
                        var s = senhas[i];
                        var plain = ObterSenhaPlain(s);
                        if (plain != null)
                        {
                            itens.Add(new SenhaExportada
                            {
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
                                Anexos = await ObterAnexosExportadosAsync(s),
                                Favorito = s.Favorito,
                                DataCriacao = s.DataCriacao,
                                DataAtualizacao = s.DataAtualizacao
                            });
                        }

                        AtualizarProgresso("Export.Progress", i + 1, senhas.Count);
                    }

                    await _servicoExportacao.ExportarAsync(arquivo.Path.LocalPath, itens, dlg.SenhaInformada);
                }
                finally
                {
                    EsconderProgresso();
                    Scrim.Ocultar(this);
                }

                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ExportSuccess", itens.Count),
                    Idioma.Texto("Common.Export"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ExportError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void Importar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var arquivos = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = Idioma.Texto("ExportDialog.ImportTitle"),
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(Idioma.Texto("Common.ExportedVaultFile")) { Patterns = new[] { "*.gsenhas" } },
                        new FilePickerFileType(Idioma.Texto("Common.AllFiles")) { Patterns = new[] { "*" } }
                    }
                });
                if (arquivos.Count == 0)
                    return;

                var dlg = new JanelaSenhaExportacao(modoExportar: false);
                if (!await AbrirDialogoAsync<bool>(dlg))
                    return;

                List<SenhaExportada> itens;
                try
                {
                    itens = await _servicoExportacao.ImportarAsync(arquivos[0].Path.LocalPath, dlg.SenhaInformada);
                }
                catch (ErroLocalizavel ex)
                {
                    await CaixaMensagem.MostrarAsync(this, ErrosUi.MensagemAmigavel(ex), Idioma.Texto("Common.Import"), TipoMensagem.Aviso);
                    return;
                }

                if (itens.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Message.ImportEmpty"),
                        Idioma.Texto("Common.Import"));
                    return;
                }

                var (adicionadas, invalidas, duplicadas) = await ImportarComProgressoAsync(itens);

                var msg = Idioma.Formatar("Message.ImportSuccess", adicionadas);
                if (invalidas > 0)
                    msg += "\n" + Idioma.Formatar("Message.ImportIgnored", invalidas);
                if (duplicadas > 0)
                    msg += "\n" + Idioma.Formatar("Message.ImportDuplicates", duplicadas);
                await CaixaMensagem.MostrarAsync(this, msg, Idioma.Texto("Common.Import"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ImportError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void ImportarCsv_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var arquivos = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = Idioma.Texto("Settings.ImportCsv"),
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(Idioma.Texto("Common.CsvFile")) { Patterns = new[] { "*.csv" } },
                        new FilePickerFileType(Idioma.Texto("Common.AllFiles")) { Patterns = new[] { "*" } }
                    }
                });
                if (arquivos.Count == 0)
                    return;

                ResultadoImportacaoCsv resultado;
                try
                {
                    resultado = _servicoImportacaoCsv.ImportarArquivo(arquivos[0].Path.LocalPath);
                }
                catch (ErroLocalizavel ex)
                {
                    await CaixaMensagem.MostrarAsync(this, ErrosUi.MensagemAmigavel(ex), Idioma.Texto("Settings.ImportCsv"), TipoMensagem.Aviso);
                    return;
                }

                if (resultado.Itens.Count == 0)
                {
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Message.CsvEmpty"),
                        Idioma.Texto("Settings.ImportCsv"));
                    return;
                }

                var itensPreview = resultado.Itens
                    .Select(i => $"{i.NomeServico} — {i.Usuario}")
                    .ToList();
                var confirmar = await CaixaMensagem.ConfirmarComListaAsync(this,
                    Idioma.Formatar("Message.CsvConfirm", resultado.FormatoDetectado, resultado.Itens.Count),
                    Idioma.Texto("Settings.ImportCsv"), itensPreview);
                if (!confirmar)
                    return;

                var (adicionadas, invalidas, duplicadas) = await ImportarComProgressoAsync(resultado.Itens);
                invalidas += resultado.LinhasIgnoradas;

                var msg = Idioma.Formatar("Message.ImportSuccess", adicionadas);
                if (invalidas > 0)
                    msg += "\n" + Idioma.Formatar("Message.CsvIgnored", invalidas);
                if (duplicadas > 0)
                    msg += "\n" + Idioma.Formatar("Message.CsvDuplicates", duplicadas);
                msg += "\n\n" + Idioma.Texto("Message.CsvSecurity");
                await CaixaMensagem.MostrarAsync(this, msg, Idioma.Texto("Settings.ImportCsv"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.ImportError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        internal async Task<(int adicionadas, int invalidas, int duplicadas)> AplicarImportacaoAsync(
            List<SenhaExportada> itens, Action<int, int>? aoProgredir = null)
        {
            var existentes = await _servicoSenha.ListarTodosAsync();
            var chaves = new HashSet<(string Nome, string Usuario)>(
                existentes.Select(s => ChaveDuplicata(s.NomeServico, s.Usuario)));

            int adicionadas = 0, invalidas = 0, duplicadas = 0, processadas = 0;
            try
            {
                foreach (var item in itens)
                {
                    if (string.IsNullOrWhiteSpace(item.NomeServico) ||
                        string.IsNullOrWhiteSpace(item.Usuario) ||
                        string.IsNullOrWhiteSpace(item.Senha))
                    {
                        invalidas++;
                    }
                    else if (!chaves.Add(ChaveDuplicata(item.NomeServico, item.Usuario)))
                    {
                        duplicadas++;
                    }
                    else
                    {
                        Senha? nova = null;
                        try
                        {
                            var totp = _totp.SegredoValido(item.TotpSegredo) ? item.TotpSegredo : null;
                            nova = await _servicoSenha.CriarSenhaAsync(
                                item.NomeServico, item.Usuario, item.Senha, item.Categoria, item.Url, item.Notas, totp, item.Etiquetas,
                                item.Tipo, item.CamposExtras);
                        }
                        catch (ErroLocalizavel)
                        {
                            chaves.Remove(ChaveDuplicata(item.NomeServico, item.Usuario));
                            invalidas++;
                        }

                        if (nova != null)
                        {
                            if (item.Favorito)
                                await _servicoSenha.MarcarComoFavoritoAsync(nova.Id);
                            RestaurarHistorico(nova, item.Historico);
                            if (item.CodigosRecuperacao is { Count: > 0 })
                                await _servicoSenha.AdicionarCodigosRecuperacaoAsync(nova.Id,
                                    item.CodigosRecuperacao.Select(c => (c.Codigo, c.Usado)));
                            await RestaurarAnexosAsync(nova, item.Anexos);
                            adicionadas++;
                        }
                    }

                    processadas++;
                    aoProgredir?.Invoke(processadas, itens.Count);
                }
            }
            finally
            {
                // Mesmo que um item no meio do lote lance algo além de ErroLocalizavel
                // (ex.: banco de dados conectado caiu na metade), o que já foi
                // adicionado até aqui não pode ficar só na memória, sem persistir e
                // sem refletir na lista — a exceção ainda propaga normalmente depois.
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }

            return (adicionadas, invalidas, duplicadas);
        }

        // Tupla, não concatenação de string ("nome + " " + usuario"): serviço="Banco X",
        // usuario="Contas Correntes" e serviço="Banco X Contas", usuario="Correntes" geravam
        // a mesma chave concatenada e um dos dois era descartado como "duplicata" na
        // importação sem nunca ter sido de fato duplicado.
        private static (string Nome, string Usuario) ChaveDuplicata(string nomeServico, string usuario) =>
            (nomeServico.ToLowerInvariant(), usuario.ToLowerInvariant());

        private void MostrarProgresso(string chaveMensagem)
        {
            BarraProgresso.Value = 0;
            LblProgresso.Text = Idioma.Formatar(chaveMensagem, 0, 0);
            PainelProgresso.IsVisible = true;
        }

        private void AtualizarProgresso(string chaveMensagem, int processadas, int total)
        {
            Dispatcher.UIThread.Post(() =>
            {
                BarraProgresso.Value = total == 0 ? 0 : processadas * 100.0 / total;
                LblProgresso.Text = Idioma.Formatar(chaveMensagem, processadas, total);
            });
        }

        private void EsconderProgresso() => PainelProgresso.IsVisible = false;

        private async Task<(int adicionadas, int invalidas, int duplicadas)> ImportarComProgressoAsync(List<SenhaExportada> itens)
        {
            Scrim.Mostrar(this);
            MostrarProgresso("Import.Progress");
            try
            {
                return await AplicarImportacaoAsync(itens, (processadas, total) => AtualizarProgresso("Import.Progress", processadas, total));
            }
            finally
            {
                EsconderProgresso();
                Scrim.Ocultar(this);
            }
        }

        private void RestaurarHistorico(Senha destino, List<HistoricoSenhaExportada>? historico)
        {
            if (historico == null || historico.Count == 0 || _criptografia == null)
                return;

            destino.Historico = historico
                .Where(h => !string.IsNullOrEmpty(h.Senha))
                .Select(h => new HistoricoSenha
                {
                    SenhaHash = _criptografia.Criptografar(h.Senha),
                    DataAlteracao = h.DataAlteracao
                })
                .ToList();
        }

        private async Task RestaurarAnexosAsync(Senha destino, List<AnexoExportado>? anexos)
        {
            if (anexos == null || anexos.Count == 0 || _servicoAnexos == null)
                return;

            foreach (var item in anexos)
            {
                try
                {
                    var bytes = Convert.FromBase64String(item.ConteudoBase64);
                    await _servicoAnexos.AdicionarAsync(destino, item.NomeArquivo, bytes);
                }
                catch
                {
                }
            }
        }
    }
}
