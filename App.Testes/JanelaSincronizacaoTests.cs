using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    public class JanelaSincronizacaoTests
    {
        [AvaloniaFact]
        public async Task Abrir_SemPerfilConfigurado_MostraPainelInativo()
        {
            var perfilOriginal = Preferencias.Sincronizacao;
            try
            {
                Preferencias.Sincronizacao = null;

                var janela = new JanelaSincronizacao(null, _ => { }, () => Task.FromResult(true));
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                Assert.True(janela.Encontrar<StackPanel>("PainelInativo").IsVisible);
                Assert.False(janela.Encontrar<StackPanel>("PainelAtivo").IsVisible);
            }
            finally
            {
                Preferencias.Sincronizacao = perfilOriginal;
            }
        }

        [AvaloniaFact]
        public async Task Abrir_ComPerfilConfigurado_MostraPainelAtivoComAPasta()
        {
            var perfilOriginal = Preferencias.Sincronizacao;
            try
            {
                var pastaSincronizada = TesteUtil.CriarPastaTemporaria();
                Preferencias.Sincronizacao = new PerfilSincronizacao
                {
                    Pasta = pastaSincronizada,
                    Salt = Convert.ToBase64String(ServicoSincronizacao.GerarSalt()),
                    FrequenciaMinutos = 15
                };

                var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
                var servicoSincronizacao = new ServicoSincronizacao(new ServicoCriptografia(chave));

                var janela = new JanelaSincronizacao(servicoSincronizacao, _ => { }, () => Task.FromResult(true));
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                Assert.False(janela.Encontrar<StackPanel>("PainelInativo").IsVisible);
                Assert.True(janela.Encontrar<StackPanel>("PainelAtivo").IsVisible);
                Assert.Equal(pastaSincronizada, janela.Encontrar<TextBlock>("LblPasta").Text);
            }
            finally
            {
                Preferencias.Sincronizacao = perfilOriginal;
            }
        }
    }
}
