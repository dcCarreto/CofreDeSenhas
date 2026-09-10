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
        private async Task RegistrarCopiaLinhaAsync(Senha senha, TipoCampoCopiado campo)
        {
            await _servicoSenha.RegistrarCopiaAsync(senha.Id, campo);
            await _servicoSenha.PersistirAsync();

            if (_senhaDetalhe != null && _senhaDetalhe.Id == senha.Id)
                AtualizarHistoricoDetalhes();
        }

        private async Task FavoritarToggle(Senha s)
        {
            try
            {
                if (s.Favorito) await _servicoSenha.RemoverDeFavoritoAsync(s.Id);
                else await _servicoSenha.MarcarComoFavoritoAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                AtualizarFiltroOrganizacao();
                FiltrarSenhas();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.FavoriteError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task FixarToggle(Senha s)
        {
            try
            {
                if (s.Fixado) await _servicoSenha.RemoverFixacaoAsync(s.Id);
                else await _servicoSenha.MarcarComoFixadoAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                AtualizarFiltroOrganizacao();
                FiltrarSenhas();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.PinError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        // internal só pra teste chamar direto e confirmar o bloqueio do modo
        // privacidade sem precisar simular o clique no botão da linha (que nem chega a
        // testar o guard, já que RaiseEvent não respeita IsEnabled) — ver App.Testes
        // (InternalsVisibleTo).
        internal async void EditarSenha(Senha s)
        {
            // Mesma proteção de AbrirDetalhes: o botão da linha já fica desabilitado no
            // modo privacidade, mas checar aqui de novo cobre qualquer outro caminho que
            // chegue a este método sem passar pelo botão.
            if (_modoPrivacidade)
                return;

            var dlg = new JanelaEditarSenha(_servicoSenha, s, _criptografia, _servicoAnexos);
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            // Invalida só os caches desta entrada; o resto da auditoria continua válido.
            _cachePlain.Remove(s.Id);
            _itensAuditoria.Remove(s.Id);
            _vazamentosPorId.Remove(s.Id);
            AtualizarFiltroOrganizacao();
            FiltrarSenhas();
        }

        private async Task ExcluirSenhaAsync(Senha s)
        {
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Message.DeletePrompt", s.NomeServico, Idioma.Texto("Message.MoveToTrash")),
                Idioma.Texto("Message.DeleteTitle"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                await _servicoSenha.RemoverSenhaAsync(s.Id);
                await _servicoSenha.PersistirAsync();
                RemoverSenhaDaLista(s.Id);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.DeleteError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void RemoverSenhaDaLista(Guid id)
        {
            _senhasAtuais.RemoveAll(s => s.Id == id);
            _itensAuditoria.Remove(id);
            _vazamentosPorId.Remove(id);
            _cachePlain.Remove(id);
            if (_senhaDetalhe?.Id == id)
                FecharDetalhes();
            AtualizarFiltroOrganizacao();
            FiltrarSenhas();
            AtualizarContador();
        }
    }
}
