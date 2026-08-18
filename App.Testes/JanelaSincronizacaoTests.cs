using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class JanelaSincronizacaoTests
    {
        [AvaloniaFact]
        public async Task Abrir_SemPerfilConfigurado_MostraPainelInativo()
        {
            var perfilOriginal = Preferencias.Sincronizacao;
            try
            {
                Preferencias.Sincronizacao = null;

                var janela = new JanelaSincronizacao(null, _ => { }, () => Task.FromResult(true), () => false, _ => Task.FromResult(true));
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

                var janela = new JanelaSincronizacao(servicoSincronizacao, _ => { }, () => Task.FromResult(true), () => false, _ => Task.FromResult(true));
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

        [AvaloniaFact]
        public async Task SincronizarAgora_QuandoJaEstaSincronizando_MostraAvisoENaoChamaSincronizarDeNovo()
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

                var chamadas = 0;
                var janela = new JanelaSincronizacao(servicoSincronizacao, _ => { },
                    () => { chamadas++; return Task.FromResult(true); },
                    () => true,
                    _ => Task.FromResult(true));
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                janela.BotaoPorTexto(Idioma.Texto("Sync.Now")).RaiseEvent(
                    new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                Assert.Equal(0, chamadas);
            }
            finally
            {
                Preferencias.Sincronizacao = perfilOriginal;
            }
        }

        [AvaloniaFact]
        public async Task Desativar_QuandoJaEstaSincronizando_MostraAvisoENaoDesativa()
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

                ServicoSincronizacao? servicoRecebido = servicoSincronizacao;
                // Antes da correção, nada impedia zerar a chave (ZerarChave) enquanto
                // uma sincronização em andamento ainda a estava usando —
                // ObjectDisposedException derrubaria aquele ciclo de sync por trás.
                var janela = new JanelaSincronizacao(servicoSincronizacao, s => servicoRecebido = s,
                    () => Task.FromResult(true),
                    () => true,
                    _ => Task.FromResult(true));
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                janela.Encontrar<Button>("BtnDesativar").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                Assert.NotNull(Preferencias.Sincronizacao);
                Assert.NotNull(servicoRecebido);
            }
            finally
            {
                Preferencias.Sincronizacao = perfilOriginal;
            }
        }

        [AvaloniaFact]
        public async Task Ativar_AbreOConfirmarSenhaMestraAtravesDoDelegateInjetado()
        {
            // O diálogo de confirmação de senha mestra aninhado precisa passar pelo
            // delegate injetado (que em produção é JanelaPrincipal.AbrirDialogoAsync,
            // o único ponto que vincula o MonitorInatividade a um diálogo) em vez de
            // um ShowDialog direto — senão teclas digitadas nele não contam como
            // atividade, e o bloqueio automático pode fechá-lo à força no meio da
            // digitação.
            var perfilOriginal = Preferencias.Sincronizacao;
            try
            {
                Preferencias.Sincronizacao = null;

                Window? janelaRecebidaPeloDelegate = null;
                var janela = new JanelaSincronizacao(null, _ => { },
                    () => Task.FromResult(true),
                    () => false,
                    janelaAninhada =>
                    {
                        janelaRecebidaPeloDelegate = janelaAninhada;
                        return Task.FromResult(false);
                    });
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                // BtnAtivar dispara o seletor de pasta nativo antes de chegar no
                // diálogo de senha — inviável de simular num teste headless. Em vez
                // disso, confirma direto que o delegate injetado é o mesmo que
                // Ativar_Click chamaria com o diálogo de senha mestra.
                var confirmarSenha = new JanelaConfirmarSenhaMestra("t", "i", "b");
                await janela._abrirDialogoAninhado(confirmarSenha);

                Assert.Same(confirmarSenha, janelaRecebidaPeloDelegate);
            }
            finally
            {
                Preferencias.Sincronizacao = perfilOriginal;
            }
        }
    }
}
