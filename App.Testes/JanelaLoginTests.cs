using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class JanelaLoginTests
    {
        [AvaloniaFact]
        public async Task Desbloqueio_ComSenhaCorreta_DisparaCallbackComAChave()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            var chaveOriginal = auth.CriarSenhaMestra("SenhaDeTeste123!");

            byte[]? chaveRecebida = null;
            var login = new JanelaLogin(auth, (chave, senha) => chaveRecebida = chave);
            login.Show();

            login.Encontrar<TextBox>("TxtSenha").Text = "SenhaDeTeste123!";
            login.Encontrar<Button>("BtnPrincipal").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() => chaveRecebida != null);

            Assert.NotNull(chaveRecebida);
            Assert.Equal(chaveOriginal, chaveRecebida);
        }

        [AvaloniaFact]
        public async Task Desbloqueio_ComSenhaErrada_MostraErroENaoDisparaCallback()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            auth.CriarSenhaMestra("SenhaDeTeste123!");

            byte[]? chaveRecebida = null;
            var login = new JanelaLogin(auth, (chave, senha) => chaveRecebida = chave);
            login.Show();

            login.Encontrar<TextBox>("TxtSenha").Text = "SenhaErrada!";
            login.Encontrar<Button>("BtnPrincipal").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var lblErro = login.Encontrar<TextBlock>("LblErro");
            await TesteUtil.AguardarAsync(() => !string.IsNullOrEmpty(lblErro.Text));

            Assert.Null(chaveRecebida);
            Assert.False(string.IsNullOrEmpty(lblErro.Text));
        }

        [AvaloniaFact]
        public async Task Desbloqueio_ComSenhaErrada_ReabilitaBotaoPrincipalDepoisDaFalha()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            auth.CriarSenhaMestra("SenhaDeTeste123!");

            var login = new JanelaLogin(auth, (chave, senha) => { });
            login.Show();

            var btnPrincipal = login.Encontrar<Button>("BtnPrincipal");
            login.Encontrar<TextBox>("TxtSenha").Text = "SenhaErrada!";
            btnPrincipal.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var lblErro = login.Encontrar<TextBlock>("LblErro");
            await TesteUtil.AguardarAsync(() => !string.IsNullOrEmpty(lblErro.Text));

            // ConfirmarAsync desabilita o botão como primeira ação (trava de
            // reentrância) e reabilita no finally quando o caminho não vai autenticar
            // — uma senha errada não pode deixar o botão travado pro resto da sessão.
            Assert.True(btnPrincipal.IsEnabled);
        }

        [AvaloniaFact]
        public async Task Desbloqueio_ComCincoSenhasErradas_BloqueiaEMostraErro()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            auth.CriarSenhaMestra("SenhaDeTeste123!");

            var login = new JanelaLogin(auth, (chave, senha) => { });
            login.Show();

            var btnPrincipal = login.Encontrar<Button>("BtnPrincipal");
            var txtSenha = login.Encontrar<TextBox>("TxtSenha");
            var lblErro = login.Encontrar<TextBlock>("LblErro");

            for (var i = 0; i < 5; i++)
            {
                await TesteUtil.AguardarAsync(() => btnPrincipal.IsEnabled);
                txtSenha.Text = "SenhaErrada!";
                btnPrincipal.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await TesteUtil.AguardarAsync(() => !string.IsNullOrEmpty(lblErro.Text));
            }

            Assert.False(btnPrincipal.IsEnabled);
            Assert.Equal(Idioma.Texto("Login.Error.TooManyAttempts"), lblErro.Text);
        }

        [AvaloniaFact]
        public async Task Desbloqueio_ComBloqueioDeUmaInstanciaAnterior_AbreJaBloqueado()
        {
            // Simula reabrir o app depois de bater no limite de tentativas — uma nova
            // JanelaLogin (processo reiniciado leria do mesmo tentativas.dat) precisa
            // abrir já bloqueada, não com o contador zerado de novo.
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            auth.CriarSenhaMestra("SenhaDeTeste123!");

            var controle = new ControleTentativasLogin(pasta);
            for (var i = 0; i < ControleTentativasLogin.LimiteTentativas; i++)
                controle.RegistrarFalha();

            var login = new JanelaLogin(auth, (chave, senha) => { });
            login.Show();

            var btnPrincipal = login.Encontrar<Button>("BtnPrincipal");
            var lblErro = login.Encontrar<TextBlock>("LblErro");
            await TesteUtil.AguardarAsync(() => !string.IsNullOrEmpty(lblErro.Text));

            Assert.False(btnPrincipal.IsEnabled);
            Assert.Equal(Idioma.Texto("Login.Error.TooManyAttempts"), lblErro.Text);
        }

        [AvaloniaFact]
        public async Task Confirmar_ComBotaoJaDesabilitado_IgnoraChamadaReentrante()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            auth.CriarSenhaMestra("SenhaDeTeste123!");

            var chamadas = 0;
            var login = new JanelaLogin(auth, (chave, senha) => chamadas++);
            login.Show();

            var btnPrincipal = login.Encontrar<Button>("BtnPrincipal");
            login.Encontrar<TextBox>("TxtSenha").Text = "SenhaDeTeste123!";

            // Simula uma segunda entrega de clique/Enter chegando enquanto a primeira
            // chamada a ConfirmarAsync ainda está em voo (o botão já foi desabilitado
            // por ela) — RaiseEvent não respeita IsEnabled sozinho, mas a checagem no
            // início de ConfirmarAsync deve fazer essa segunda chamada não fazer nada.
            btnPrincipal.IsEnabled = false;
            btnPrincipal.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.Equal(0, chamadas);
            Assert.True(string.IsNullOrEmpty(login.Encontrar<TextBlock>("LblErro").Text));
        }

        [AvaloniaFact]
        public async Task Desbloqueio_ComSenhaCorreta_BotaoContinuaDesabilitadoAposDispararCallback()
        {
            var pasta = TesteUtil.CriarPastaTemporaria();
            var auth = new AutenticacaoMestra(pasta);
            auth.CriarSenhaMestra("SenhaDeTeste123!");

            var chamadas = 0;
            var login = new JanelaLogin(auth, (chave, senha) => chamadas++);
            login.Show();

            var btnPrincipal = login.Encontrar<Button>("BtnPrincipal");
            login.Encontrar<TextBox>("TxtSenha").Text = "SenhaDeTeste123!";
            btnPrincipal.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() => chamadas > 0);

            // Continua desabilitado de propósito: a janela de login está prestes a
            // fechar (o app troca pra JanelaPrincipal) — reabilitar aqui só abriria uma
            // fresta pra um clique repetido chamar _aoAutenticar de novo antes disso.
            Assert.Equal(1, chamadas);
            Assert.False(btnPrincipal.IsEnabled);
        }

        [AvaloniaFact]
        public async Task RestaurarComChaveDerivadaAsync_ComBancoValido_RestauraOCofreLocalEDisparaCallback()
        {
            var pastaOrigem = TesteUtil.CriarPastaTemporaria();
            var authOrigem = new AutenticacaoMestra(pastaOrigem);
            var chaveOrigem = authOrigem.CriarSenhaMestra("SenhaDeTeste123!");
            authOrigem.TentarLerParametros(out var salt, out var verificador, out var kdf, out var custo, out var memoriaKb, out var paralelismo);

            var arquivoBanco = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GS_Restaurar_" + Guid.NewGuid().ToString("N") + ".db");
            var cfg = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = arquivoBanco };
            var bd = new ServicoBancoDados();
            await bd.CriarTabelaAsync(cfg);
            await bd.GarantirColunasAsync(cfg);
            await bd.CriarTabelaAuthAsync(cfg);
            await bd.PublicarAuthAsync(cfg, new AuthBanco(salt, verificador, kdf, custo, memoriaKb, paralelismo));

            var criptografiaOrigem = new ServicoCriptografia(chaveOrigem);
            await new RepositorioSenhaBanco(cfg, criptografiaOrigem).AdicionarAsync(new Senha
            {
                NomeServico = "ServicoX",
                Usuario = "user",
                SenhaHash = criptografiaOrigem.Criptografar("SenhaOriginal@1"),
                Categoria = Categoria.Personal
            });

            var pastaNova = TesteUtil.CriarPastaTemporaria();
            var authNova = new AutenticacaoMestra(pastaNova);

            byte[]? chaveRecebida = null;
            string? senhaRecebida = null;
            var login = new JanelaLogin(authNova, (chave, senha) => { chaveRecebida = chave; senhaRecebida = senha; });
            login.Show();

            // RestaurarComChaveDerivadaAsync grava Preferencias.UltimoBanco (estático,
            // global) com Conectado=true no caminho de sucesso — sem salvar/restaurar,
            // isso vaza pra QUALQUER teste que depois construa uma JanelaPrincipal
            // "limpa": o construtor dela chama IniciarAsync, que vê UltimoBanco.Conectado
            // e tenta reconectar sozinho ao banco (já apagado) deste teste, trocando
            // _servicoSenha por um repositório de banco vazio por baixo do pano.
            var perfilBancoOriginal = Preferencias.UltimoBanco;
            try
            {
                var auth = await bd.LerAuthAsync(cfg);
                Assert.NotNull(auth);

                await login.RestaurarComChaveDerivadaAsync(cfg, auth!, "SenhaDeTeste123!");

                Assert.NotNull(chaveRecebida);
                Assert.Equal("SenhaDeTeste123!", senhaRecebida);

                var chaveVerificada = authNova.Autenticar("SenhaDeTeste123!");
                Assert.NotNull(chaveVerificada);

                var persist = new PersistenciaLocal(new ServicoCriptografia(chaveVerificada!), pastaNova);
                var senhas = await persist.CarregarSenhasAsync(chaveVerificada!);
                var item = Assert.Single(senhas);
                Assert.Equal("ServicoX", item.NomeServico);
            }
            finally
            {
                // Preferencias.Salvar() grava em %APPDATA% de verdade (Preferencias.cs
                // não tem pasta injetável pra teste) — RestaurarComChaveDerivadaAsync já
                // chamou Salvar() com o valor poluído, então restaurar só em memória não
                // basta: sem chamar Salvar() de novo aqui, o config.json real do usuário
                // ficaria com UltimoBanco apontando pra um banco de teste já apagado.
                Preferencias.UltimoBanco = perfilBancoOriginal;
                Preferencias.Salvar();
                try { if (File.Exists(arquivoBanco)) File.Delete(arquivoBanco); } catch { }
            }
        }

        [AvaloniaFact]
        public async Task RestaurarComChaveDerivadaAsync_ComFalhaAoLerDoBanco_RevertePraOEstadoAnteriorSemAutenticar()
        {
            var pastaNova = TesteUtil.CriarPastaTemporaria();
            var authNova = new AutenticacaoMestra(pastaNova);
            var chaveAnterior = authNova.CriarSenhaMestra("SenhaAntiga@123");
            var criptografiaAnterior = new ServicoCriptografia(chaveAnterior);
            await new PersistenciaLocal(criptografiaAnterior, pastaNova).SalvarSenhasAsync(new List<Senha>
            {
                new() { NomeServico = "JaLocal", Usuario = "u", SenhaHash = criptografiaAnterior.Criptografar("p") }
            }, chaveAnterior);

            // Auth "publicado" com parâmetros quaisquer, mas o cfg aponta pra um banco
            // que não existe — simula uma falha entre ler o auth publicado (que já
            // aconteceu, fora deste método) e efetivamente ler os dados do banco.
            var authFalso = new AuthBanco(new byte[16], new byte[32], 1, 3, 65536, 1);
            var cfgInexistente = new ConexaoBanco
            {
                Tipo = TipoBanco.SQLite,
                Banco = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "GS_Restaurar_Inexistente_" + Guid.NewGuid().ToString("N"), "sub", "banco.db")
            };

            var chamadas = 0;
            var login = new JanelaLogin(authNova, (chave, senha) => chamadas++);
            login.Show();

            await login.RestaurarComChaveDerivadaAsync(cfgInexistente, authFalso, "QualquerSenha@1");

            Assert.Equal(0, chamadas);
            Assert.False(string.IsNullOrEmpty(login.Encontrar<TextBlock>("LblErro").Text));

            // auth.dat continua autenticando com a senha ANTERIOR, não com a que se
            // tentou restaurar — o rollback do backup funcionou.
            Assert.NotNull(authNova.Autenticar("SenhaAntiga@123"));

            var persistVerificacao = new PersistenciaLocal(criptografiaAnterior, pastaNova);
            var senhas = await persistVerificacao.CarregarSenhasAsync(chaveAnterior);
            Assert.Contains(senhas, s => s.NomeServico == "JaLocal");
        }
    }
}
