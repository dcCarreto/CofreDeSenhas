using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaPrincipal
    {
        internal void AplicarVerificarAtualizacoes(bool ligado)
        {
            Preferencias.VerificarAtualizacoes = ligado;
            Preferencias.Salvar();

            if (ligado)
                _ = VerificarAtualizacaoAsync();
            else
                OcultarAvisoAtualizacao();
        }

        private async Task VerificarAtualizacaoAsync()
        {
            if (!Preferencias.VerificarAtualizacoes)
                return;

            var atualizacao = await ServicoAtualizacao.VerificarNovaVersaoAsync();
            if (atualizacao is not { } info ||
                string.Equals(info.Tag, Preferencias.VersaoDispensada, StringComparison.OrdinalIgnoreCase))
                return;

            ExibirAtualizacaoDisponivel(info);
        }

        // internal só pra teste popular o painel de atualização sem depender da
        // chamada de rede de verdade que VerificarAtualizacaoAsync faz pra API do
        // GitHub — ver App.Testes (InternalsVisibleTo).
        internal void ExibirAtualizacaoDisponivel(AtualizacaoDisponivel info)
        {
            _versaoDisponivel = info.Tag;
            _notasVersaoDisponivel = info.NotasVersao;
            LblAtualizacaoDisponivel.Text = Idioma.Texto("Update.Available");
            AutomationProperties.SetName(LblAtualizacaoDisponivel, Idioma.Texto("Update.Available"));
            PainelAtualizacaoDisponivel.IsVisible = true;
        }

        private async void AtualizarAgora_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_versaoDisponivel) || _atualizando)
                return;

            // Sem isto, "Atualizar agora" já disparava o download e a instalação
            // silenciosa direto — o usuário nunca via o que mudava antes de o app se
            // fechar sozinho pra aplicar a atualização.
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
                var resultado = await ServicoAtualizacao.AtualizarAgoraAsync(_versaoDisponivel);
                switch (resultado.Tipo)
                {
                    case ResultadoAtualizacaoTipo.Sucesso:
                        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                        return;
                    case ResultadoAtualizacaoTipo.Falha:
                        var mensagemFalha = string.IsNullOrEmpty(resultado.Mensagem)
                            ? Idioma.Texto("Update.Failed")
                            : Idioma.Texto("Update.Failed") + "\n\n" + resultado.Mensagem;
                        await CaixaMensagem.MostrarAsync(this, mensagemFalha, Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                        AbrirPaginaReleases();
                        break;
                    case ResultadoAtualizacaoTipo.NaoSuportado:
                        AbrirPaginaReleases();
                        break;
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

        private static void AbrirPaginaReleases()
        {
            try { Process.Start(new ProcessStartInfo(ServicoAtualizacao.UrlPaginaReleases) { UseShellExecute = true }); }
            catch { }
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

        private void DispensarAtualizacao_Click(object? sender, RoutedEventArgs e)
        {
            Preferencias.VersaoDispensada = _versaoDisponivel;
            Preferencias.Salvar();
            OcultarAvisoAtualizacao();
        }

        private void OcultarAvisoAtualizacao()
        {
            PainelAtualizacaoDisponivel.IsVisible = false;
            _versaoDisponivel = null;
            _notasVersaoDisponivel = null;
        }
    }
}
