using Avalonia.Controls;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas
{
    internal static class RecuperacaoUi
    {
        // Mostra a chave recém-gerada e exige um "anotei" antes de fechar.
        public static Task MostrarChaveAsync(Window dono, string chave) =>
            CaixaMensagem.ConfirmarComListaAsync(dono,
                Idioma.Texto("Recovery.ShowInstruction"),
                Idioma.Texto("Recovery.ShowTitle"),
                new[] { chave },
                TipoMensagem.Aviso);

        // Fluxo de criação do cofre: pergunta, e se sim, habilita a recuperação e
        // mostra a chave. Silencioso se o usuário recusar.
        public static async Task OferecerNaCriacaoAsync(Window dono, byte[] chaveMestra, string? pastaApp = null)
        {
            var quer = await CaixaMensagem.ConfirmarAsync(dono,
                Idioma.Texto("Recovery.OfferAtCreation"),
                Idioma.Texto("Recovery.SettingsRow"));
            if (!quer)
                return;

            var segredo = new ServicoRecuperacao(pastaApp).Habilitar(chaveMestra);
            await MostrarChaveAsync(dono, segredo);
        }

        // Chamado depois de uma troca de senha mestra: se a recuperação estava
        // ativa, o envelope antigo virou lixo (guardava a chave antiga) — gera um
        // segredo novo e avisa.
        public static async Task RenovarAposTrocaDeSenhaAsync(Window dono, byte[] chaveNova, string? pastaApp = null)
        {
            var svc = new ServicoRecuperacao(pastaApp);
            if (!svc.EstaHabilitada())
                return;

            var segredo = svc.Habilitar(chaveNova);
            await CaixaMensagem.MostrarAsync(dono, Idioma.Texto("Recovery.ChangedAfterMaster"),
                Idioma.Texto("Recovery.ShowTitle"), TipoMensagem.Aviso);
            await MostrarChaveAsync(dono, segredo);
        }
    }
}
