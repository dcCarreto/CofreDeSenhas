using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaPrincipal
    {
        internal void AplicarAtualizacaoAutomatica(bool ligado)
        {
            Preferencias.AtualizacaoAutomatica = ligado;
            Preferencias.Salvar();

            if (ligado)
                AtualizacaoAutomatica.Iniciar();
            else
                AtualizacaoAutomatica.Parar();
        }

        // Chamado ao abrir o cofre. A atualização é baixada em segundo plano e instalada
        // no próximo bloqueio (App.Bloquear); o rodapé avisa quando ela está pronta e
        // oferece instalar na hora.
        private void IniciarAtualizacaoAutomatica()
        {
            AtualizacaoAutomatica.ProntaMudou += AoMudarAtualizacaoPronta;
            Closed += (s, e) => AtualizacaoAutomatica.ProntaMudou -= AoMudarAtualizacaoPronta;

            if (AtualizacaoAutomatica.Pronta)
                ExibirAtualizacaoPronta(AtualizacaoAutomatica.NotasVersao);
            AtualizacaoAutomatica.Iniciar();
        }

        private void AoMudarAtualizacaoPronta() => Dispatcher.UIThread.Post(() =>
        {
            if (AtualizacaoAutomatica.Pronta)
                ExibirAtualizacaoPronta(AtualizacaoAutomatica.NotasVersao);
            else
                OcultarAvisoAtualizacao();
        });

        private static readonly TimeSpan DuracaoAvisoAtualizacaoAutomatica = TimeSpan.FromMinutes(1);

        // Aviso único para quem já usava o app quando a atualização passou a ser
        // automática (Preferencias.Carregar marca). Fica no rodapé, sem caixa de diálogo
        // nem botão para fechar: nada da atualização pode depender de um clique. Some
        // sozinho depois de um minuto e já conta como visto ao aparecer; o texto
        // completo fica na dica e no texto de ajuda do leitor de tela.
        //
        // Só onde a atualização de fato roda: em Debug, com COFRE_BASE ou no .tar.gz do
        // Linux não haveria o que avisar; na Loja quem atualiza é ela.
        internal void MostrarAvisoAtualizacaoAutomatica()
        {
            if (!Preferencias.AvisoAtualizacaoAutomaticaPendente)
                return;

            Preferencias.AvisoAtualizacaoAutomaticaPendente = false;
            Preferencias.Salvar();

            if (PacoteLoja.Empacotado || !ServicoAtualizacao.Suportada || _atualizacaoPronta)
                return;

            _mostrandoAvisoAtualizacaoAutomatica = true;
            LblAtualizacaoDisponivel.Text = Idioma.Texto("Update.AutoNoticeTitle");
            AutomationProperties.SetName(LblAtualizacaoDisponivel, LblAtualizacaoDisponivel.Text);
            AutomationProperties.SetHelpText(LblAtualizacaoDisponivel, Idioma.Texto("Update.AutoNoticeMessage"));
            ToolTip.SetTip(LblAtualizacaoDisponivel, Idioma.Texto("Update.AutoNoticeMessage"));
            BtnAtualizarAgora.IsVisible = false;
            PainelAtualizacaoDisponivel.IsVisible = true;

            DispatcherTimer.RunOnce(() =>
            {
                if (_mostrandoAvisoAtualizacaoAutomatica)
                    EncerrarAvisoAtualizacaoAutomatica();
            }, DuracaoAvisoAtualizacaoAutomatica);
        }

        private void EncerrarAvisoAtualizacaoAutomatica()
        {
            _mostrandoAvisoAtualizacaoAutomatica = false;
            AutomationProperties.SetHelpText(LblAtualizacaoDisponivel, null);
            ToolTip.SetTip(LblAtualizacaoDisponivel, null);
            BtnAtualizarAgora.IsVisible = true;
            PainelAtualizacaoDisponivel.IsVisible = false;
        }

        // internal só pra teste popular o painel sem depender de um download de verdade
        // (ver App.Testes, InternalsVisibleTo).
        internal void ExibirAtualizacaoPronta(string? notasVersao)
        {
            if (_mostrandoAvisoAtualizacaoAutomatica)
                EncerrarAvisoAtualizacaoAutomatica();

            _atualizacaoPronta = true;
            _notasVersaoDisponivel = notasVersao;
            if (_avisoAtualizacaoOculto)
                return;

            LblAtualizacaoDisponivel.Text = TextoAtualizacaoPronta();
            AutomationProperties.SetName(LblAtualizacaoDisponivel, LblAtualizacaoDisponivel.Text);
            BtnAtualizarAgora.IsVisible = true;
            PainelAtualizacaoDisponivel.IsVisible = true;
        }

        private static string TextoAtualizacaoPronta() =>
            Idioma.Texto(AtualizacaoAutomatica.ExigeAdministrador ? "Update.AvailableAdmin" : "Update.Available");

        private async void AtualizarAgora_Click(object? sender, RoutedEventArgs e)
        {
            if (!_atualizacaoPronta || _atualizando)
                return;

            // A instalação automática acontece no bloqueio; este botão antecipa e fecha o
            // app na hora — por isso mostra antes o que muda e pede confirmação.
            var confirmou = await CaixaMensagem.ConfirmarComListaAsync(this,
                Idioma.Texto("Update.ConfirmMessage"),
                Idioma.Texto("Update.ConfirmTitle"),
                QuebrarNotasDaVersao(_notasVersaoDisponivel),
                TipoMensagem.Info);
            if (!confirmou)
                return;

            _atualizando = true;
            BtnAtualizarAgora.IsEnabled = false;
            LblBtnAtualizarAgora.Text = Idioma.Texto("Update.Downloading");
            AutomationProperties.SetName(BtnAtualizarAgora, LblBtnAtualizarAgora.Text);
            try
            {
                if (PacoteLoja.Empacotado)
                {
                    // Se instalar, o Windows fecha e reabre o app antes de voltar aqui.
                    if (!await AtualizacaoAutomatica.InstalarLojaAsync())
                        await CaixaMensagem.MostrarAsync(this, Idioma.Texto("Update.Failed"), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                }
                else if (await AtualizacaoAutomatica.AplicarLocalAsync())
                {
                    (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                    return;
                }
                else
                {
                    await CaixaMensagem.MostrarAsync(this, Idioma.Texto("Update.Failed"), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                }
            }
            finally
            {
                _atualizando = false;
                BtnAtualizarAgora.IsEnabled = true;
                LblBtnAtualizarAgora.Text = Idioma.Texto("Update.Now");
                AutomationProperties.SetName(BtnAtualizarAgora, LblBtnAtualizarAgora.Text);
            }
        }

        // As notas vêm em markdown puro da API do GitHub — sem um renderizador de
        // markdown no app, mostra linha a linha (títulos com # e itens com - ainda
        // saem legíveis como texto simples) em vez de tentar interpretar a formatação.
        private static List<string> QuebrarNotasDaVersao(string? notas)
        {
            if (string.IsNullOrWhiteSpace(notas))
                return new List<string> { Idioma.Texto("Update.NoReleaseNotes") };

            return notas.Replace("\r\n", "\n").Split('\n')
                .Select(linha => linha.Trim())
                .Where(linha => linha.Length > 0)
                .ToList();
        }

        // Só esconde o aviso nesta sessão; a atualização continua marcada para ser
        // instalada no próximo bloqueio.
        private void DispensarAtualizacao_Click(object? sender, RoutedEventArgs e)
        {
            // Fechar o aviso de "atualização automática ligada" não pode esconder o de
            // "atualização pronta" que chegar depois.
            if (_mostrandoAvisoAtualizacaoAutomatica)
            {
                EncerrarAvisoAtualizacaoAutomatica();
                return;
            }

            _avisoAtualizacaoOculto = true;
            PainelAtualizacaoDisponivel.IsVisible = false;
        }

        private void OcultarAvisoAtualizacao()
        {
            PainelAtualizacaoDisponivel.IsVisible = false;
            _atualizacaoPronta = false;
            _notasVersaoDisponivel = null;
        }
    }
}
