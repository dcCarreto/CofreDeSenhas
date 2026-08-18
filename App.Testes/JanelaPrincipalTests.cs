using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CofreDeSenhas;
using CofreDeSenhas.Controles;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class JanelaPrincipalTests
    {
        private static (IServicoSenha servico, byte[] chave) CriarServico()
        {
            var (servico, chave, _) = CriarServicoComCriptografia();
            return (servico, chave);
        }

        private static (IServicoSenha servico, byte[] chave, IServicoCriptografia criptografia) CriarServicoComCriptografia()
        {
            var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
            var criptografia = new ServicoCriptografia(chave);
            var persistencia = new PersistenciaLocal(criptografia, TesteUtil.CriarPastaTemporaria());
            var repositorio = new RepositorioSenha(persistencia, chave);
            return (new ServicoSenha(repositorio, criptografia), chave, criptografia);
        }

        [AvaloniaFact]
        public async Task AtalhoBloquearAgora_DisparaCallbackDeBloqueio()
        {
            var (servico, chave) = CriarServico();
            var bloqueado = false;
            var janela = new JanelaPrincipal(servico, chave, aoBloquear: () => bloqueado = true);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.L,
                KeyModifiers = KeyModifiers.Control
            });

            Assert.True(bloqueado);
        }

        [AvaloniaFact]
        public async Task TrocaDeIdioma_AtualizaTextoDaJanelaPrincipal()
        {
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            try
            {
                Idioma.Definir("en");

                Assert.Equal("Search by service, user, or category", janela.Encontrar<TextBox>("TxtBusca").Watermark);
            }
            finally
            {
                Idioma.Definir("pt-BR");
            }
        }

        [AvaloniaFact]
        public void Construtor_DeixaOIndicadorDeStatusDeConexaoVisivel()
        {
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);

            var ponto = janela.Encontrar<Ellipse>("PontoConexao");

            Assert.True(ponto.IsVisible);
        }

        [AvaloniaFact]
        public async Task AtalhoModoPrivacidade_MascaraListaDeCredenciais()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Servico Sensivel", "usuario.sensivel", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Sensivel"));

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.H,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "••••••••"));

            Assert.DoesNotContain(janela.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Servico Sensivel");
        }

        [AvaloniaFact]
        public async Task AtalhoModoPrivacidade_MascaraNomeAcessivelETooltipsDaLinha()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Servico Sensivel", "usuario.sensivel", "SenhaForte123!", Categoria.Work);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Sensivel"));

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.H,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "••••••••"));

            var linha = janela.GetVisualDescendants().OfType<LinhaSenha>().Single();

            var nomeLinha = AutomationProperties.GetName(linha);
            Assert.DoesNotContain("Servico Sensivel", nomeLinha ?? "");
            Assert.DoesNotContain("usuario.sensivel", nomeLinha ?? "");

            foreach (var descendente in linha.GetVisualDescendants().OfType<Control>())
            {
                var nome = AutomationProperties.GetName(descendente);
                Assert.DoesNotContain("Servico Sensivel", nome ?? "");
                Assert.DoesNotContain("usuario.sensivel", nome ?? "");

                var dica = ToolTip.GetTip(descendente) as string;
                Assert.DoesNotContain("Servico Sensivel", dica ?? "");
                Assert.DoesNotContain("usuario.sensivel", dica ?? "");
            }
        }

        [AvaloniaFact]
        public async Task AtalhoModoPrivacidade_MascaraTooltipDaEtiquetaNoChipDeCategoria()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Servico Etiquetado", "usuario.etiquetado", "SenhaForte123!",
                Categoria.Other, etiquetas: new[] { "EtiquetaSecreta" });

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Etiquetado"));

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.H,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "••••••••"));

            var linha = janela.GetVisualDescendants().OfType<LinhaSenha>().Single();

            foreach (var descendente in linha.GetVisualDescendants().OfType<Control>())
            {
                var nome = AutomationProperties.GetName(descendente);
                Assert.DoesNotContain("EtiquetaSecreta", nome ?? "");

                var dica = ToolTip.GetTip(descendente) as string;
                Assert.DoesNotContain("EtiquetaSecreta", dica ?? "");
            }
        }

        [AvaloniaFact]
        public async Task CopiarSenhaPelaLista_RegistraDataDeUltimaCopia()
        {
            var frequenciaOriginal = Preferencias.FrequenciaBackup;
            try
            {
                // "Manual" impede que JanelaPrincipal.IniciarAsync dispare um backup automático,
                // que tocaria o %APPDATA% real do desenvolvedor (fora do cofre descartável do teste).
                Preferencias.FrequenciaBackup = "Manual";

                var (servico, chave, criptografia) = CriarServicoComCriptografia();
                var criada = await servico.CriarSenhaAsync("Servico Copia", "usuario.copia", "SenhaForte123!", Categoria.Personal);

                var janela = new JanelaPrincipal(servico, chave, criptografia);
                janela.Show();
                await TesteUtil.AguardarAsync(() =>
                    janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Copia"));

                var botaoCopiar = janela.BotaoPorNomeAutomacao(Idioma.Texto("Row.CopyPassword"));
                botaoCopiar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Senha? atualizada = null;
                await TesteUtil.AguardarAsync(() =>
                {
                    atualizada = servico.ListarTodosAsync().GetAwaiter().GetResult().FirstOrDefault(s => s.Id == criada.Id);
                    return atualizada?.DataUltimaCopiaSenha != null;
                });

                Assert.NotNull(atualizada?.DataUltimaCopiaSenha);
            }
            finally
            {
                Preferencias.FrequenciaBackup = frequenciaOriginal;
            }
        }

        [AvaloniaFact]
        public async Task AplicarImportacaoAsync_ComCampoMuitoLongo_ContaComoInvalidaSemAbortarOResto()
        {
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var itens = new List<SenhaExportada>
            {
                new()
                {
                    NomeServico = new string('a', 200),
                    Usuario = "usuario.invalido",
                    Senha = "SenhaForte123!",
                    Categoria = Categoria.Personal
                },
                new()
                {
                    NomeServico = "Servico Valido",
                    Usuario = "usuario.valido",
                    Senha = "SenhaForte456!",
                    Categoria = Categoria.Personal
                }
            };

            var (adicionadas, invalidas, duplicadas) = await janela.AplicarImportacaoAsync(itens);

            Assert.Equal(1, adicionadas);
            Assert.Equal(1, invalidas);
            Assert.Equal(0, duplicadas);

            var todas = await servico.ListarTodosAsync();
            var unica = Assert.Single(todas);
            Assert.Equal("Servico Valido", unica.NomeServico);
        }

        [AvaloniaFact]
        public async Task AtalhoNovaSenha_DentroDaLixeira_NaoAbreDialogoDeCriacao()
        {
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.Encontrar<Button>("BtnNavLixeira").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.N,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.DoesNotContain(janela.OwnedWindows, w => w is JanelaCriarSenha);
            Assert.Empty(await servico.ListarTodosAsync());
        }

        [AvaloniaFact]
        public async Task EnterNoCabecalhoDeColuna_OrdenaAListaAssimComoOClique()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Zebra", "u1", "SenhaForte123!", Categoria.Personal);
            await servico.CriarSenhaAsync("Abacaxi", "u2", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<LinhaSenha>().Count() == 2);

            var nomesAntes = janela.GetVisualDescendants().OfType<LinhaSenha>().Select(l => l.Senha.NomeServico).ToList();
            Assert.Equal(new[] { "Abacaxi", "Zebra" }, nomesAntes);

            var cabecalhoServico = janela.GetVisualDescendants().OfType<StackPanel>()
                .First(s => AutomationProperties.GetName(s) == Idioma.Texto("Vault.Table.Service"));
            cabecalhoServico.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var nomesDepois = janela.GetVisualDescendants().OfType<LinhaSenha>().Select(l => l.Senha.NomeServico).ToList();
            Assert.Equal(new[] { "Zebra", "Abacaxi" }, nomesDepois);
        }

        [AvaloniaFact]
        public async Task NavegarParaLixeira_MostraItemExcluidoEPermiteRestaurar()
        {
            var (servico, chave) = CriarServico();
            var criada = await servico.CriarSenhaAsync("Servico Excluido", "usuario.excluido", "SenhaForte123!", Categoria.Personal);
            await servico.RemoverSenhaAsync(criada.Id);
            await servico.PersistirAsync();

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.Encontrar<Button>("BtnNavLixeira").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Excluido"));

            var botaoRestaurar = janela.BotaoPorTexto(Idioma.Texto("Trash.Restore"));
            botaoRestaurar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            List<Senha> ativos = new();
            await TesteUtil.AguardarAsync(() =>
            {
                ativos = servico.ListarTodosAsync().GetAwaiter().GetResult();
                return ativos.Any(s => s.Id == criada.Id);
            });

            Assert.Contains(ativos, s => s.Id == criada.Id);
        }

        [AvaloniaFact]
        public async Task CtrlClicarDuasLinhas_MostraPainelDeAcoesEmLoteEFavoritarAplicaNasDuas()
        {
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("Servico Lote A", "usuario.a", "SenhaForte123!", Categoria.Personal);
            await servico.CriarSenhaAsync("Servico Lote B", "usuario.b", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<LinhaSenha>().Count() == 2);

            foreach (var linha in janela.GetVisualDescendants().OfType<LinhaSenha>().ToList())
            {
                var ponto = linha.TranslatePoint(new Point(2, 26), janela) ?? default;
                janela.MouseDown(ponto, MouseButton.Left, RawInputModifiers.Control);
                janela.MouseUp(ponto, MouseButton.Left, RawInputModifiers.Control);
            }

            await TesteUtil.AguardarAsync(() => janela.Encontrar<Border>("PainelAcoesLote").IsVisible);
            Assert.Equal(Idioma.Plural(2, "Batch.CountSingular", "Batch.CountPlural"),
                janela.Encontrar<TextBlock>("LblContagemSelecao").Text);

            janela.Encontrar<Button>("BtnLoteFavoritar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            List<Senha> todas = new();
            await TesteUtil.AguardarAsync(() =>
            {
                todas = servico.ListarTodosAsync().GetAwaiter().GetResult();
                return todas.Count(s => s.Favorito) == 2;
            });

            Assert.Equal(2, todas.Count(s => s.Favorito));
        }

        [AvaloniaFact]
        public async Task LoteAdicionarEtiqueta_AplicaAEtiquetaNosItensSelecionados()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("Servico Etiqueta A", "usuario.a", "SenhaForte123!", Categoria.Personal);
            await servico.CriarSenhaAsync("Servico Etiqueta B", "usuario.b", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<LinhaSenha>().Count() == 2);

            foreach (var linha in janela.GetVisualDescendants().OfType<LinhaSenha>().ToList())
            {
                var ponto = linha.TranslatePoint(new Point(2, 26), janela) ?? default;
                janela.MouseDown(ponto, MouseButton.Left, RawInputModifiers.Control);
                janela.MouseUp(ponto, MouseButton.Left, RawInputModifiers.Control);
            }

            await TesteUtil.AguardarAsync(() => janela.Encontrar<Border>("PainelAcoesLote").IsVisible);

            janela.Encontrar<Button>("BtnLoteEtiqueta").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => janela.Encontrar<StackPanel>("PainelAcoesLoteEtiqueta").IsVisible);

            janela.Encontrar<TextBox>("TxtLoteEtiqueta").Text = "urgente";
            janela.Encontrar<Button>("BtnLoteEtiquetaAplicar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            List<Senha> todas = new();
            await TesteUtil.AguardarAsync(() =>
            {
                todas = servico.ListarTodosAsync().GetAwaiter().GetResult();
                return todas.Count(s => s.Etiquetas.Contains("urgente")) == 2;
            });

            Assert.Equal(2, todas.Count(s => s.Etiquetas.Contains("urgente")));
        }

        private static string DescricaoConexao(JanelaPrincipal janela) =>
            ToolTip.GetTip(janela.Encontrar<TextBlock>("LblConexao")) as string ?? "";

        [AvaloniaFact]
        public async Task ConectarAsync_ComBancoValido_AtivaConexaoEPassaAGravarLa()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var arquivo = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GS_Conectar_" + Guid.NewGuid().ToString("N") + ".db");
            var cfg = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = arquivo };
            var bd = new ServicoBancoDados();
            await bd.CriarTabelaAsync(cfg);
            await bd.GarantirColunasAsync(cfg);

            // Semeia o banco diretamente (sem passar por "servico", que continua
            // sendo o serviço local original — ConectarAposAsync troca a instância
            // interna de _servicoSenha da janela, não o objeto que o teste segura).
            var repoBanco = new RepositorioSenhaBanco(cfg, criptografia);
            await repoBanco.AdicionarAsync(new Senha
            {
                NomeServico = "JaNoBanco",
                Usuario = "u",
                SenhaHash = criptografia.Criptografar("Senha@Forte1"),
                Categoria = Categoria.Personal
            });

            try
            {
                await janela.ConectarAsync(cfg, persistir: false, silencioso: true);

                Assert.True(janela.Encontrar<MenuItem>("MenuDesconectarBanco").IsVisible);
                Assert.Equal(Idioma.Formatar("Vault.Connection.Connected", cfg.Descricao), DescricaoConexao(janela));

                // Prova que a janela passou a exibir o conteúdo do banco (não mais o
                // serviço local original, que estava vazio) — _servicoSenha realmente
                // trocou de instância dentro de ConectarAposAsync.
                await TesteUtil.AguardarAsync(() =>
                    janela.GetVisualDescendants().OfType<LinhaSenha>().Any(l => l.Senha.NomeServico == "JaNoBanco"));
                Assert.Contains(janela.GetVisualDescendants().OfType<LinhaSenha>(), l => l.Senha.NomeServico == "JaNoBanco");
            }
            finally
            {
                try { if (File.Exists(arquivo)) File.Delete(arquivo); } catch { }
            }
        }

        [AvaloniaFact]
        public async Task ConectarAsync_ComBancoInacessivel_VoltaParaOServicoLocalSemDerrubarAJanela()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("JaLocal", "u", "Senha@Forte1", Categoria.Personal);
            await servico.PersistirAsync();

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            // Pasta inexistente: SQLite falha ao criar o arquivo do banco ali dentro.
            var cfgInvalida = new ConexaoBanco
            {
                Tipo = TipoBanco.SQLite,
                Banco = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GS_Inexistente_" + Guid.NewGuid().ToString("N"), "sub", "banco.db")
            };

            // silencioso: true evita popar um diálogo de erro que ficaria esperando
            // interação num teste headless. Nesse modo, uma falha de conexão cai no
            // ramo "falha ao reconectar" de AtualizarEstadoConexao (não no "Local") —
            // MenuDesconectarBanco também fica visível nesse caso, então o sinal
            // confiável de "não conectou de verdade" é o texto do status.
            await janela.ConectarAsync(cfgInvalida, persistir: false, silencioso: true);

            Assert.Equal(Idioma.Texto("Vault.Connection.DatabaseUnavailable"), DescricaoConexao(janela));

            // O serviço local continua respondendo normalmente depois da falha.
            var locais = await servico.ListarTodosAsync();
            Assert.Contains(locais, s => s.NomeServico == "JaLocal");
        }

        [AvaloniaFact]
        public async Task ConectarAsync_ComDesconectarClicadoEnquantoAIndaEstaEmVoo_NaoReconectaDepoisQueATarefaTermina()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var arquivo = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GS_CorridaConectar_" + Guid.NewGuid().ToString("N") + ".db");
            var cfg = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = arquivo };
            var bd = new ServicoBancoDados();
            await bd.CriarTabelaAsync(cfg);
            await bd.GarantirColunasAsync(cfg);

            try
            {
                // Semeia um ponto de espera controlável como "tarefa anterior" —
                // ConectarAposAsync sempre aguarda essa tarefa como sua primeira
                // linha, então isto garante um yield real e determinístico logo no
                // início, diferente de tentar flagrar a corrida só via timing com E/S
                // local (que aqui termina rápido demais, às vezes até sincronamente,
                // pra deixar uma janela real de interleaving).
                var portao = new TaskCompletionSource();
                janela._tarefaConexaoAtual = portao.Task;

                var tarefaConectar = janela.ConectarAsync(cfg, persistir: false, silencioso: true);

                // ConectarAposAsync está parado esperando o portão — minhaGeracao já
                // foi capturada antes desta linha rodar. Simula uma desconexão (ou
                // outra tentativa de conexão) mudando a geração antes de liberar.
                janela._geracaoConexao++;
                portao.SetResult();

                await tarefaConectar;
                await TesteUtil.AguardarAsync(() => false, tentativas: 10);

                // A conexão não deveria ter conseguido aplicar seu resultado por cima
                // da geração mais recente — nem os campos de estado nem o rótulo (que
                // só é atualizado dentro do bloco protegido pela checagem de geração)
                // podem refletir "conectado".
                Assert.Equal(Idioma.Texto("Vault.Connection.Local"), DescricaoConexao(janela));
            }
            finally
            {
                try { if (File.Exists(arquivo)) File.Delete(arquivo); } catch { }
            }
        }

        [AvaloniaFact]
        public async Task RepublicarAposTrocaDeSenhaMestraAsync_ComBancoEPastaDeSincronizacaoConectados_ReencriptaAmbosComAChaveNova()
        {
            // Precisa passar repositorioLocal explicitamente (diferente do resto dos
            // testes deste arquivo): sem ele, _repositorioLocal fica nulo e
            // ConectarAposAsync não monta o RepositorioSenhaEspelhado — a janela passa
            // a enxergar só o banco (vazio), nunca o item criado abaixo. É exatamente
            // como AbrirCofre em App.axaml.cs conecta de verdade.
            var chaveAntiga = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
            var criptografiaAntiga = new ServicoCriptografia(chaveAntiga);
            var persistenciaLocal = new PersistenciaLocal(criptografiaAntiga, TesteUtil.CriarPastaTemporaria());
            var repositorioLocal = new RepositorioSenha(persistenciaLocal, chaveAntiga);
            var servico = new ServicoSenha(repositorioLocal, criptografiaAntiga);
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();

            var janela = new JanelaPrincipal(servico, chaveAntiga, criptografiaAntiga, repositorioLocal);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var arquivoBanco = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GS_TrocaSenha_" + Guid.NewGuid().ToString("N") + ".db");
            var cfg = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = arquivoBanco };
            var bd = new ServicoBancoDados();
            await bd.CriarTabelaAsync(cfg);
            await bd.GarantirColunasAsync(cfg);

            var perfilSyncOriginal = Preferencias.Sincronizacao;
            var perfilBancoOriginal = Preferencias.UltimoBanco;
            var pastaSync = TesteUtil.CriarPastaTemporaria();
            var saltSync = ServicoSincronizacao.GerarSalt();
            var (kdf, iteracoes, memoriaKb, paralelismo) = ServicoSincronizacao.ParametrosPadrao();
            Preferencias.Sincronizacao = new PerfilSincronizacao
            {
                Pasta = pastaSync,
                Salt = Convert.ToBase64String(saltSync),
                Kdf = kdf,
                Iteracoes = iteracoes,
                MemoriaKb = memoriaKb,
                Paralelismo = paralelismo
            };

            try
            {
                await janela.ConectarAsync(cfg, persistir: false, silencioso: true);
                Assert.Equal(Idioma.Formatar("Vault.Connection.Connected", cfg.Descricao), DescricaoConexao(janela));

                // ConectarAsync(persistir: false) propositalmente não grava
                // Preferencias.UltimoBanco (evita gravar em Preferencias.Salvar(), que
                // escreveria no config.json real do usuário rodando o teste) — mas
                // RepublicarAposTrocaDeSenhaMestraAsync depende dele pra reconstruir os
                // dados de conexão, já que a janela não guarda o ConexaoBanco ativo em
                // outro lugar. Seta só em memória, sem Salvar().
                Preferencias.UltimoBanco = new PerfilBanco { Tipo = cfg.Tipo, Banco = cfg.Banco };

                // Confirma que o espelhamento local-banco realmente ligou (a janela
                // passou a enxergar o item mesclado, não só o serviço local original).
                await TesteUtil.AguardarAsync(() =>
                    janela.GetVisualDescendants().OfType<LinhaSenha>().Any(l => l.Senha.NomeServico == "ServicoX"));
                Assert.Contains(janela.GetVisualDescendants().OfType<LinhaSenha>(), l => l.Senha.NomeServico == "ServicoX");

                var chaveNova = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaNova@456");

                var afetaOutrosDispositivos = await janela.RepublicarAposTrocaDeSenhaMestraAsync(chaveNova, "SenhaNova@456", null);

                Assert.True(afetaOutrosDispositivos);

                // Banco: com a chave antiga, o hmac gravado não bate mais (o item foi
                // recifrado e o hmac recalculado com a chave nova) — vira violação de
                // integridade. Com a chave nova, lê normalmente.
                var repoBancoComChaveAntiga = new RepositorioSenhaBanco(cfg, criptografiaAntiga);
                await repoBancoComChaveAntiga.ListarTodosAsync();
                Assert.Contains(repoBancoComChaveAntiga.ViolacoesIntegridade, v => v.NomeServico == "ServicoX");

                var criptografiaNova = new ServicoCriptografia(chaveNova);
                var repoBancoComChaveNova = new RepositorioSenhaBanco(cfg, criptografiaNova);
                var doBancoComChaveNova = await repoBancoComChaveNova.ListarTodosAsync();
                Assert.Empty(repoBancoComChaveNova.ViolacoesIntegridade);
                var itemBanco = Assert.Single(doBancoComChaveNova, s => s.NomeServico == "ServicoX");
                Assert.Equal("SenhaOriginal@1", criptografiaNova.Descriptografar(itemBanco.SenhaHash));

                // Pasta de sincronização: o arquivo foi reescrito com a chave nova — a
                // chave antiga não consegue mais decifrá-lo (LerAsync engole a falha e
                // retorna lista vazia, mesmo comportamento de "arquivo corrompido").
                var caminhoSync = System.IO.Path.Combine(pastaSync, ServicoSincronizacao.NomeArquivo);
                var servicoSyncAntigo = new ServicoSincronizacao(criptografiaAntiga);
                var lidoComChaveAntiga = await servicoSyncAntigo.LerAsync(caminhoSync);
                Assert.Empty(lidoComChaveAntiga);

                var chaveSyncNova = ServicoSincronizacao.DerivarChave("SenhaNova@456", saltSync, kdf, iteracoes, memoriaKb, paralelismo);
                var servicoSyncNovo = new ServicoSincronizacao(new ServicoCriptografia(chaveSyncNova));
                var lidoComChaveNova = await servicoSyncNovo.LerAsync(caminhoSync);
                Assert.Contains(lidoComChaveNova, s => s.NomeServico == "ServicoX");
            }
            finally
            {
                Preferencias.Sincronizacao = perfilSyncOriginal;
                Preferencias.UltimoBanco = perfilBancoOriginal;
                try { if (File.Exists(arquivoBanco)) File.Delete(arquivoBanco); } catch { }
            }
        }

        [AvaloniaFact]
        public async Task RepublicarAposTrocaDeSenhaMestraAsync_SemBancoNemPastaConectados_NaoFazNadaERetornaFalse()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var perfilOriginal = Preferencias.Sincronizacao;
            try
            {
                Preferencias.Sincronizacao = null;

                var chaveNova = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaNova@456");
                var afetaOutrosDispositivos = await janela.RepublicarAposTrocaDeSenhaMestraAsync(chaveNova, "SenhaNova@456", null);

                Assert.False(afetaOutrosDispositivos);
            }
            finally
            {
                Preferencias.Sincronizacao = perfilOriginal;
            }
        }

        [AvaloniaFact]
        public async Task DetalhesTemAlteracoesNaoSalvas_RevelarEditarSenhaEOcultarDeNovo_ContinuaDetectandoAEdicao()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();
            var item = (await servico.ListarTodosAsync()).Single();

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.AbrirDetalhes(item);
            Assert.False(janela.DetalhesTemAlteracoesNaoSalvas());

            var btnRevelar = janela.Encontrar<Button>("BtnDetalheRevelar");
            var txtSenha = janela.Encontrar<TextBox>("TxtDetalheSenha");

            btnRevelar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            txtSenha.Text = "SenhaEditada@2";
            btnRevelar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            // Antes da correção, _senhaDetalhePlain (usada como baseline) era
            // sobrescrita pelo próprio valor editado na hora de ocultar de novo — a
            // edição virava invisível pra DetalhesTemAlteracoesNaoSalvas assim que o
            // campo deixava de estar visível.
            Assert.True(janela.DetalhesTemAlteracoesNaoSalvas());
        }

        [AvaloniaFact]
        public async Task PainelDetalhes_ComOperacaoJaEmAndamento_CliqueEmFecharNaoFechaOPainel()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();
            var item = (await servico.ListarTodosAsync()).Single();

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.AbrirDetalhes(item);

            // Simula um Salvar (ou Excluir) já em voo — antes da correção, nada
            // impedia Fechar de rodar concorrentemente e, se o Salvar terminasse
            // depois, ele reabriria um painel que o usuário já tinha fechado.
            janela._detalhesOperacaoEmAndamento = true;

            janela.Encontrar<Button>("BtnFecharDetalhes").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.True(janela.Encontrar<Border>("PainelDetalhes").IsVisible);
        }

        [AvaloniaFact]
        public async Task PainelDetalhes_ComOperacaoJaEmAndamento_CliqueEmExcluirNaoExclui()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();
            var item = (await servico.ListarTodosAsync()).Single();

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.AbrirDetalhes(item);
            janela._detalhesOperacaoEmAndamento = true;

            janela.Encontrar<Button>("BtnExcluirDetalhes").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var restantes = await servico.ListarTodosAsync();
            Assert.Contains(restantes, s => s.Id == item.Id);
        }

        [AvaloniaFact]
        public async Task SalvarDetalhes_AoConcluir_ReabilitaFecharEExcluir()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();
            var item = (await servico.ListarTodosAsync()).Single();

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.AbrirDetalhes(item);
            janela.Encontrar<TextBox>("TxtDetalheNotas").Text = "nota nova";
            janela.Encontrar<Button>("BtnSalvarDetalhes").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await TesteUtil.AguardarAsync(() => !janela._detalhesOperacaoEmAndamento);

            Assert.True(janela.Encontrar<Button>("BtnFecharDetalhes").IsEnabled);
            Assert.True(janela.Encontrar<Button>("BtnExcluirDetalhes").IsEnabled);
            Assert.True(janela.Encontrar<Button>("BtnSalvarDetalhes").IsEnabled);
        }

        [AvaloniaFact]
        public async Task PublicarTumbasNaPastaDeSincronizacaoAsync_ComItemQueJaEstavaNoArquivoRemoto_SubstituiPorUmaTumbaQueNaoResgataOItem()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            var criado = await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();

            var perfilOriginal = Preferencias.Sincronizacao;
            var pastaSync = TesteUtil.CriarPastaTemporaria();
            var saltSync = ServicoSincronizacao.GerarSalt();
            var (kdf, iteracoes, memoriaKb, paralelismo) = ServicoSincronizacao.ParametrosPadrao();
            Preferencias.Sincronizacao = new PerfilSincronizacao
            {
                Pasta = pastaSync,
                Salt = Convert.ToBase64String(saltSync),
                Kdf = kdf,
                Iteracoes = iteracoes,
                MemoriaKb = memoriaKb,
                Paralelismo = paralelismo
            };

            try
            {
                var chaveSync = ServicoSincronizacao.DerivarChave("SenhaDeTeste123!", saltSync, kdf, iteracoes, memoriaKb, paralelismo);
                var servicoSincronizacao = new ServicoSincronizacao(new ServicoCriptografia(chaveSync));
                var caminho = System.IO.Path.Combine(pastaSync, ServicoSincronizacao.NomeArquivo);

                // Simula um ciclo de sync anterior: o item já está no arquivo
                // compartilhado, como se este dispositivo (ou outro) já tivesse
                // sincronizado antes de excluir.
                await servicoSincronizacao.EscreverAsync(caminho, saltSync, kdf, iteracoes, memoriaKb, paralelismo,
                    new List<SenhaExportada>
                    {
                        new()
                        {
                            Id = criado.Id,
                            NomeServico = "ServicoX",
                            Usuario = "user",
                            Senha = "SenhaOriginal@1",
                            DataAtualizacao = criado.DataAtualizacao
                        }
                    });

                var janela = new JanelaPrincipal(servico, chave, criptografia, servicoSincronizacao: servicoSincronizacao);
                janela.Show();

                // janela.Show() dispara o evento Opened, que inclui um
                // "_ = SincronizarAsync(silencioso: true)" em segundo plano — com
                // _servicoSincronizacao e Preferencias.Sincronizacao configurados como
                // acima, essa sincronização de fundo roda de verdade e, se terminar
                // antes de ExcluirDefinitivamenteAsync/PublicarTumbas abaixo, pode
                // mesclar e regravar o arquivo remoto por conta própria — dá tempo dela
                // completar aqui pra não competir com o resto do teste.
                await TesteUtil.AguardarAsync(() => false, tentativas: 15);

                // Exclui de verdade localmente (sem deixar rastro, como
                // RepositorioSenha.RemoverDefinitivamenteAsync realmente faz) e publica
                // a tumba, como ExcluirDefinitivamenteAsync/EsvaziarLixeira_Click fazem.
                await servico.RemoverDefinitivamenteAsync(criado.Id);
                await servico.PersistirAsync();
                await janela.PublicarTumbasNaPastaDeSincronizacaoAsync(new[] { criado.Id });

                var remotas = await servicoSincronizacao.LerAsync(caminho);
                var tumba = Assert.Single(remotas);
                Assert.Equal(criado.Id, tumba.Id);
                Assert.True(tumba.NaLixeira);
                Assert.Equal("", tumba.NomeServico);

                // Um dispositivo que nunca teve o item localmente (ou que também já o
                // excluiu) e puxa esse arquivo remoto não o ressuscita — a tumba vence
                // a mesclagem em vez de virar uma credencial nova.
                var mescladas = ServicoSincronizacao.MesclarListas(new List<SenhaExportada>(), remotas);
                var itemMesclado = Assert.Single(mescladas);
                Assert.True(GerenciadorDeSenhas.Servicos.MesclaSincronizacao.EhTumbaDeExclusaoDefinitiva(itemMesclado));
            }
            finally
            {
                // A sincronização de fundo disparada por janela.Show() (ver acima) pode
                // ter chamado Preferencias.Salvar() com o perfil deste teste —
                // restaurar só em memória não basta, porque Preferencias.Salvar() grava
                // em %APPDATA% de verdade (Preferencias.cs não tem pasta injetável pra
                // teste); sem chamar Salvar() de novo aqui, o config.json real do
                // usuário ficaria com Sincronizacao apontando pra uma pasta de teste.
                Preferencias.Sincronizacao = perfilOriginal;
                Preferencias.Salvar();
            }
        }

        [AvaloniaFact]
        public async Task DetalhesTemAlteracaoConcorrente_ComOMesmoItemAlteradoPorForaEnquantoOPainelEstaAberto_DetectaAMudanca()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();
            var item = (await servico.ListarTodosAsync()).Single();

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.AbrirDetalhes(item);
            Assert.False(janela.DetalhesTemAlteracaoConcorrente());

            // Simula uma sincronização automática silenciosa alterando este mesmo
            // item por fora enquanto o painel de detalhes continua aberto —
            // CarregarSenhasAsync (chamada ao fim de todo ciclo de sync) atualiza
            // _senhasAtuais, mas nunca toca no painel já aberto.
            await servico.AtualizarSenhaAsync(item.Id, "ServicoX", "user", "SenhaMudadaPorFora@2", Categoria.Personal);
            await servico.PersistirAsync();
            await janela.CarregarSenhasAsync(silencioso: true);

            Assert.True(janela.DetalhesTemAlteracaoConcorrente());
        }

        [AvaloniaFact]
        public async Task DetalhesTemAlteracaoConcorrente_SemNenhumaMudancaExterna_ContinuaFalse()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();
            var item = (await servico.ListarTodosAsync()).Single();

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.AbrirDetalhes(item);

            // Um ciclo de sync/refresh que não mudou nada não pode disparar falso
            // positivo de conflito.
            await janela.CarregarSenhasAsync(silencioso: true);

            Assert.False(janela.DetalhesTemAlteracaoConcorrente());
        }
    }
}
