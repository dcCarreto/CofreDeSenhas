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
        private void Linha_SelecaoAlterada(object? sender, Senha senha)
        {
            if (sender is not LinhaSenha linha)
                return;

            if (linha.Selecionada)
                _selecionados.Add(senha.Id);
            else
                _selecionados.Remove(senha.Id);

            AtualizarPainelAcoesLote();
        }

        private void AtualizarPainelAcoesLote()
        {
            PainelAcoesLote.IsVisible = _selecionados.Count > 0;
            LblContagemSelecao.Text = Idioma.Plural(_selecionados.Count,
                "Batch.CountSingular", "Batch.CountPlural");
            PainelAcoesLoteBotoes.IsVisible = true;
            PainelAcoesLoteEtiqueta.IsVisible = false;
        }

        private void LoteCancelarSelecao_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var linha in _linhasSenha)
                linha.DefinirSelecionada(false);
            _selecionados.Clear();
            AtualizarPainelAcoesLote();
        }

        private async void LoteFavoritar_Click(object? sender, RoutedEventArgs e)
        {
            var ids = _selecionados.ToList();
            try
            {
                foreach (var id in ids)
                    await _servicoSenha.MarcarComoFavoritoAsync(id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.FavoriteError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void LoteMoverParaLixeira_Click(object? sender, RoutedEventArgs e)
        {
            var ids = _selecionados.ToList();

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Batch.TrashConfirm", ids.Count),
                Idioma.Texto("Message.DeleteTitle"), TipoMensagem.Aviso);
            if (!confirmar)
                return;

            try
            {
                foreach (var id in ids)
                    await _servicoSenha.RemoverSenhaAsync(id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.DeleteError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void LoteAdicionarEtiqueta_Click(object? sender, RoutedEventArgs e)
        {
            PainelAcoesLoteBotoes.IsVisible = false;
            PainelAcoesLoteEtiqueta.IsVisible = true;
            TxtLoteEtiqueta.Text = "";
            TxtLoteEtiqueta.Focus();
        }

        private void LoteCancelarEtiqueta_Click(object? sender, RoutedEventArgs e)
        {
            PainelAcoesLoteBotoes.IsVisible = true;
            PainelAcoesLoteEtiqueta.IsVisible = false;
        }

        private async void LoteAplicarEtiqueta_Click(object? sender, RoutedEventArgs e)
        {
            var etiqueta = (TxtLoteEtiqueta.Text ?? "").Trim();
            if (string.IsNullOrEmpty(etiqueta))
                return;

            var itens = _senhasAtuais.Where(s => _selecionados.Contains(s.Id)).ToList();
            try
            {
                foreach (var item in itens)
                {
                    var plain = ObterSenhaPlain(item);
                    if (plain == null)
                        continue;

                    var etiquetas = new List<string>(item.Etiquetas);
                    if (!etiquetas.Contains(etiqueta, StringComparer.OrdinalIgnoreCase))
                        etiquetas.Add(etiqueta);

                    await _servicoSenha.AtualizarSenhaAsync(item.Id, item.NomeServico, item.Usuario, plain,
                        item.Categoria, item.Url, item.Notas, etiquetas, item.Tipo, null);
                }
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.UpdateError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task RenomearServicoAsync(Senha s, string novoNome)
        {
            try
            {
                string nome = novoNome.Trim();
                if (string.IsNullOrWhiteSpace(nome) ||
                    string.Equals(nome, s.NomeServico, StringComparison.Ordinal))
                    return;

                var plain = ObterSenhaPlain(s);
                if (string.IsNullOrEmpty(plain))
                    throw new InvalidOperationException(Idioma.Texto("Message.RenameDecryptError"));

                await _servicoSenha.AtualizarSenhaAsync(s.Id, nome, s.Usuario, plain, s.Categoria, s.Url, s.Notas, s.Etiquetas);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.RenameError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                throw;
            }
        }
    }
}
