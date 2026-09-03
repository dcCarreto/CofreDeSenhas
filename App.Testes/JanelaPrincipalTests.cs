using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
        public async Task AtalhoBloquearAgora_ComAltJunto_NaoDisparaOCallback()
        {
            // AltGr (teclado PT-BR, entre outros) chega como Ctrl+Alt sintético — digitar
            // um símbolo com AltGr+L em qualquer campo não pode travar o cofre sozinho.
            var (servico, chave) = CriarServico();
            var bloqueado = false;
            var janela = new JanelaPrincipal(servico, chave, aoBloquear: () => bloqueado = true);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.L,
                KeyModifiers = KeyModifiers.Control | KeyModifiers.Alt
            });

            Assert.False(bloqueado);
        }

        [AvaloniaFact]
        public async Task Construtor_MantemOTimerDeBackupAgendadoAtivoParaSessoesLongas()
        {
            // Antes da correção, o agendamento de backup automático só era avaliado uma
            // vez, na abertura da janela — numa sessão longa (o app suporta ficar
            // minimizado na bandeja por dias), "diário"/"semanal" nunca disparava de
            // novo depois disso. O timer precisa continuar rodando (não só existir uma
            // vez) pra reavaliar o agendamento periodicamente.
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.True(janela._timerBackupAgendado.IsEnabled);
            Assert.Equal(TimeSpan.FromHours(1), janela._timerBackupAgendado.Interval);

            janela.Close();

            Assert.False(janela._timerBackupAgendado.IsEnabled);
        }

        [AvaloniaFact]
        public async Task RestaurarBackupAsync_TiraFotoDeSegurancaDoEstadoAtualAntesDeSobrescrever()
        {
            // Restaurar um backup mais antigo descarta tudo que mudou depois — sem uma
            // foto de segurança do estado atual antes de sobrescrever, essas mudanças
            // ficam sem nenhum jeito de recuperar caso o usuário se arrependa ou tenha
            // escolhido o backup errado.
            var bancoOriginal = Preferencias.UltimoBanco;
            try
            {
                Preferencias.UltimoBanco = null;

                var pastaApp = TesteUtil.CriarPastaTemporaria();
                var chave = new AutenticacaoMestra(pastaApp).CriarSenhaMestra("SenhaDeTeste123!");
                var criptografia = new ServicoCriptografia(chave);
                var persistencia = new PersistenciaLocal(criptografia, pastaApp);
                var repositorio = new RepositorioSenha(persistencia, chave);
                var servico = new ServicoSenha(repositorio, criptografia);

                await servico.CriarSenhaAsync("ServicoA", "userA", "SenhaA@123", Categoria.Personal);
                await servico.PersistirAsync();

                var paraBackup1 = await servico.ListarTodosAsync();
                await persistencia.BackupAutomaticoAsync(paraBackup1, chave);
                var backup1 = persistencia.ListarBackups().Single().Caminho;

                // Só existe no cofre atual, depois do backup1 — não deveria sobreviver
                // à restauração, mas precisa sobreviver na foto de segurança.
                await servico.CriarSenhaAsync("ServicoB", "userB", "SenhaB@123", Categoria.Personal);
                await servico.PersistirAsync();

                var janela = new JanelaPrincipal(servico, chave, criptografia, repositorio);
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                // Sem aguardar aqui: RestaurarBackupAsync termina com um CaixaMensagem de
                // sucesso que fica esperando um clique — a restauração em si (o que este
                // teste verifica) já aconteceu bem antes disso. E a verificação abaixo lê
                // direto do disco por uma PersistenciaLocal nova — o "servico" original
                // já tem seu próprio RepositorioSenha com cache em memória, que
                // RestaurarBackupAsync não tem como atualizar (ele troca a referência
                // interna da JanelaPrincipal, não o objeto que este teste já segurava).
                _ = janela.RestaurarBackupAsync(persistencia, backup1);

                List<Senha>? atuais = null;
                for (var i = 0; i < 250 && (atuais == null || atuais.Count != 1); i++)
                {
                    // A troca atômica de arquivo (escrever num .tmp e mover por cima) pode
                    // deixar uma leitura concorrente esbarrar num instante de transição —
                    // trata como "ainda não pronto" em vez de deixar a exceção subir.
                    try { atuais = await persistencia.CarregarSenhasAsync(chave); }
                    catch { }

                    if (atuais == null || atuais.Count != 1)
                        await Task.Delay(20);
                }

                var restaurada = Assert.Single(atuais!);
                Assert.Equal("ServicoA", restaurada.NomeServico);

                var backups = persistencia.ListarBackups();
                Assert.Equal(2, backups.Count);
                var fotoSeguranca = backups.Single(b => b.Caminho != backup1);
                var conteudoFoto = await persistencia.CarregarBackupAsync(fotoSeguranca.Caminho);

                Assert.Equal(2, conteudoFoto.Count);
                Assert.Contains(conteudoFoto, s => s.NomeServico == "ServicoA");
                Assert.Contains(conteudoFoto, s => s.NomeServico == "ServicoB");
            }
            finally
            {
                Preferencias.UltimoBanco = bancoOriginal;
            }
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
        public async Task AtalhoModoPrivacidade_MascaraListaDaLixeira()
        {
            var (servico, chave) = CriarServico();
            var criada = await servico.CriarSenhaAsync("Servico Excluido Sensivel", "usuario.excluido.sensivel", "SenhaForte123!", Categoria.Personal);
            await servico.RemoverSenhaAsync(criada.Id);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.Encontrar<Button>("BtnNavLixeira").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Excluido Sensivel"));

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.H,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "••••••••"));

            Assert.DoesNotContain(janela.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Servico Excluido Sensivel");
            Assert.DoesNotContain(janela.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "usuario.excluido.sensivel");

            foreach (var descendente in janela.GetVisualDescendants().OfType<Control>())
            {
                var nome = AutomationProperties.GetName(descendente);
                Assert.DoesNotContain("Servico Excluido Sensivel", nome ?? "");

                var dica = ToolTip.GetTip(descendente) as string;
                Assert.DoesNotContain("Servico Excluido Sensivel", dica ?? "");
            }
        }

        [AvaloniaFact]
        public async Task EditarSenha_ComModoPrivacidadeAtivo_NaoAbreAJanelaDeEdicao()
        {
            // Antes da correção, "Editar" ignorava o modo privacidade por completo:
            // JanelaEditarSenha abria com usuário, URL, notas e campos extras em texto
            // puro — um clique bastava pra ver tudo que a lista tinha acabado de
            // mascarar, sem precisar desligar o modo privacidade.
            var (servico, chave) = CriarServico();
            var criada = await servico.CriarSenhaAsync("Servico Sensivel", "usuario.sensivel", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.H,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "••••••••"));

            janela.EditarSenha(criada);
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.DoesNotContain(janela.OwnedWindows, w => w is JanelaEditarSenha);
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
        public async Task AplicarImportacaoAsync_ComServicoEUsuarioQueColidemNaConcatenacao_NaoTrataComoDuplicata()
        {
            // Antes da correção, a chave de duplicata era "nomeServico + ' ' + usuario"
            // concatenados — "Banco X" + "Contas Correntes" e "Banco X Contas" + "Correntes"
            // geravam a mesma string "Banco X Contas Correntes" e o segundo item, mesmo
            // sendo uma credencial completamente diferente, era descartado como duplicata.
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var itens = new List<SenhaExportada>
            {
                new()
                {
                    NomeServico = "Banco X",
                    Usuario = "Contas Correntes",
                    Senha = "SenhaForte123!",
                    Categoria = Categoria.Finance
                },
                new()
                {
                    NomeServico = "Banco X Contas",
                    Usuario = "Correntes",
                    Senha = "SenhaForte456!",
                    Categoria = Categoria.Finance
                }
            };

            var (adicionadas, invalidas, duplicadas) = await janela.AplicarImportacaoAsync(itens);

            Assert.Equal(2, adicionadas);
            Assert.Equal(0, invalidas);
            Assert.Equal(0, duplicadas);
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
        public async Task TrocarIdiomaNaLixeira_MantemAListaMostrandoAsTumbas()
        {
            // IdiomaGlobal_Alterado chamava FiltrarSenhas() direto, sem checar _naLixeira —
            // trocar o idioma dentro da Lixeira trocava a lista pelo cofre inteiro,
            // contradizendo a barra de ferramentas e o item de menu, que continuam "Lixeira".
            var (servico, chave) = CriarServico();
            var excluida = await servico.CriarSenhaAsync("Servico Na Lixeira", "usuario.lixeira", "SenhaForte123!", Categoria.Personal);
            await servico.RemoverSenhaAsync(excluida.Id);
            await servico.CriarSenhaAsync("Servico No Cofre", "usuario.cofre", "SenhaForte123!", Categoria.Personal);
            await servico.PersistirAsync();

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.Encontrar<Button>("BtnNavLixeira").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Na Lixeira"));

            try
            {
                Idioma.Definir("en");
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                var textos = janela.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
                Assert.Contains("Servico Na Lixeira", textos);
                Assert.DoesNotContain("Servico No Cofre", textos);
                Assert.Empty(janela.GetVisualDescendants().OfType<LinhaSenha>());
            }
            finally
            {
                Idioma.Definir("pt-BR");
            }
        }

        [AvaloniaFact]
        public async Task RestaurarDaLixeira_AnunciaRestauradoEmVezDeCopiadoParaAreaDeTransferencia()
        {
            // O anúncio reaproveitava A11y.Copied ("{0} copiado para a área de
            // transferência.") ao restaurar — frase sem sentido e nada foi copiado.
            Acessibilidade.DefinirLeitorTela(true);
            try
            {
                var (servico, chave) = CriarServico();
                var criada = await servico.CriarSenhaAsync("Servico Restaurado", "u", "SenhaForte123!", Categoria.Personal);
                await servico.RemoverSenhaAsync(criada.Id);
                await servico.PersistirAsync();

                var janela = new JanelaPrincipal(servico, chave);
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                janela.Encontrar<Button>("BtnNavLixeira").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await TesteUtil.AguardarAsync(() =>
                    janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Restaurado"));

                janela.BotaoPorTexto(Idioma.Texto("Trash.Restore")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                var anunciador = janela.Encontrar<TextBlock>("LblAnuncioLeitorTela");
                var esperado = Idioma.Formatar("A11y.Restored", "Servico Restaurado");
                await TesteUtil.AguardarAsync(() => anunciador.Text == esperado);

                Assert.Equal(esperado, anunciador.Text);
                Assert.NotEqual(Idioma.Formatar("A11y.Copied", Idioma.Texto("Trash.Restore")), anunciador.Text);
            }
            finally
            {
                Acessibilidade.DefinirLeitorTela(false);
            }
        }

        [AvaloniaFact]
        public async Task LixeiraBotaoExcluirDefinitivamente_NomeAcessivelUsaRotuloCurtoComNomeDoItem()
        {
            // O nome acessível vinha de Trash.DeleteForeverConfirm — o texto do diálogo
            // de confirmação, com quebra de linha — em vez de um rótulo curto como o
            // botão de restaurar ao lado.
            var (servico, chave) = CriarServico();
            var criada = await servico.CriarSenhaAsync("Servico Lixo", "u", "SenhaForte123!", Categoria.Personal);
            await servico.RemoverSenhaAsync(criada.Id);
            await servico.PersistirAsync();

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.Encontrar<Button>("BtnNavLixeira").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Servico Lixo"));

            var btnExcluir = janela.GetVisualDescendants().OfType<Button>()
                .Single(b => (ToolTip.GetTip(b) as string) == Idioma.Texto("Trash.DeleteForever"));

            Assert.Equal(Idioma.Texto("Trash.DeleteForever") + " Servico Lixo", AutomationProperties.GetName(btnExcluir));
        }

        [AvaloniaFact]
        public async Task BadgeContador_AcompanhaABuscaEVoltaAoTotalQuandoLimpa()
        {
            // AtualizarContador sempre usava _senhasAtuais.Count e FiltrarSenhas nunca o
            // chamava — o badge "N Itens" ignorava busca e filtros.
            var (servico, chave) = CriarServico();
            await servico.CriarSenhaAsync("GitHub QA", "user", "SenhaForte123!", Categoria.Personal);
            await servico.CriarSenhaAsync("Site Fraco", "user2", "SenhaForte123!", Categoria.Personal);

            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => janela.GetVisualDescendants().OfType<LinhaSenha>().Count() == 2);

            var badge = janela.Encontrar<TextBlock>("LblContadorHeader");
            Assert.Equal(Idioma.Plural(2, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural"), badge.Text);

            var busca = janela.Encontrar<TextBox>("TxtBusca");
            busca.Text = "github";
            await TesteUtil.AguardarAsync(() => janela.GetVisualDescendants().OfType<LinhaSenha>().Count() == 1);
            Assert.Equal(Idioma.Plural(1, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural"), badge.Text);

            busca.Text = "";
            await TesteUtil.AguardarAsync(() => janela.GetVisualDescendants().OfType<LinhaSenha>().Count() == 2);
            Assert.Equal(Idioma.Plural(2, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural"), badge.Text);
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
        public async Task EdicaoInlineDoServico_EmAndamentoQuandoOutraLinhaFavoritaRecarregaALista_PreservaOTextoDigitado()
        {
            // Antes da correção, favoritar/fixar OUTRA linha disparava
            // CarregarSenhasAsync -> AtualizarLista, que reconstruía a lista inteira do
            // zero e descartava em silêncio uma edição de nome de serviço ainda não
            // confirmada (Enter/clicar fora) na linha A — o texto digitado sumia e o
            // nome voltava pro original sem nenhum aviso.
            var bancoOriginal = Preferencias.UltimoBanco;
            try
            {
                // Sem isto, um perfil de banco "conectado" deixado por outro teste
                // (estado estático compartilhado) faz IniciarAsync tentar reconectar em
                // vez de CarregarSenhasAsync — a lista nunca carrega e o teste não
                // encontra nenhuma linha.
                Preferencias.UltimoBanco = null;

                var (servico, chave, criptografia) = CriarServicoComCriptografia();
                await servico.CriarSenhaAsync("Servico A", "usuario.a", "SenhaForte123!", Categoria.Personal);
                await servico.CriarSenhaAsync("Servico B", "usuario.b", "SenhaForte123!", Categoria.Personal);

                var janela = new JanelaPrincipal(servico, chave, criptografia);
                janela.Show();
                await TesteUtil.AguardarAsync(() =>
                    janela.GetVisualDescendants().OfType<LinhaSenha>().Count(l => l.Senha.NomeServico is "Servico A" or "Servico B") == 2);

                var linhaA = janela.GetVisualDescendants().OfType<LinhaSenha>().First(l => l.Senha.NomeServico == "Servico A");
                var rotuloA = linhaA.TextoPorConteudo("Servico A");
                rotuloA.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

                var txtServico = linhaA.GetVisualDescendants().OfType<TextBox>()
                    .First(t => AutomationProperties.GetName(t) == Idioma.Texto("Entry.ServiceName"));
                Assert.True(txtServico.IsVisible);
                txtServico.Text = "Servico A Renomeado Sem Confirmar";

                var linhaB = janela.GetVisualDescendants().OfType<LinhaSenha>().First(l => l.Senha.NomeServico == "Servico B");
                var botaoFixarB = linhaB.BotaoPorNomeAutomacao(Idioma.Texto("Row.PinEntry"));
                botaoFixarB.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                await TesteUtil.AguardarAsync(() =>
                    servico.ListarTodosAsync().GetAwaiter().GetResult().Any(s => s.Fixado));

                var linhaARecriada = janela.GetVisualDescendants().OfType<LinhaSenha>()
                    .Single(l => l.Senha.Usuario == "usuario.a");

                Assert.True(linhaARecriada.EmEdicaoDeServico);
                Assert.Equal("Servico A Renomeado Sem Confirmar", linhaARecriada.TextoServicoEmEdicao);

                // E confirmar a edição retomada ainda funciona normalmente.
                var txtServicoRecriado = linhaARecriada.GetVisualDescendants().OfType<TextBox>()
                    .First(t => AutomationProperties.GetName(t) == Idioma.Texto("Entry.ServiceName"));
                Assert.Equal("Servico A Renomeado Sem Confirmar", txtServicoRecriado.Text);
                txtServicoRecriado.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

                Senha? itemA = null;
                await TesteUtil.AguardarAsync(() =>
                {
                    itemA = servico.ListarTodosAsync().GetAwaiter().GetResult().SingleOrDefault(s => s.Usuario == "usuario.a");
                    return itemA?.NomeServico == "Servico A Renomeado Sem Confirmar";
                });

                Assert.Equal("Servico A Renomeado Sem Confirmar", itemA?.NomeServico);
            }
            finally
            {
                Preferencias.UltimoBanco = bancoOriginal;
            }
        }

        [AvaloniaFact]
        public async Task DigitarNaBuscaComSelecaoEmLote_PreservaSelecaoDosItensAindaVisiveis()
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

            // Sem a correção, digitar na busca (Busca_Alterada -> FiltrarSenhas ->
            // AtualizarLista) derrubava a seleção inteira mesmo os dois itens
            // continuando visíveis com o termo digitado.
            janela.Encontrar<TextBox>("TxtBusca").Text = "Servico Lote";
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.True(janela.Encontrar<Border>("PainelAcoesLote").IsVisible);
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
        public async Task ConectarAsync_ComIntegridadeViolada_RegistraOConflitoNoLogDeDiagnostico()
        {
            // Antes da correção, um conflito de integridade (possível adulteração do
            // banco compartilhado — o tipo mais crítico) só existia na lista em memória
            // de RepositorioSenhaEspelhado.UltimosConflitos: se o usuário não abrisse a
            // tela de conflitos antes de reconectar ou fechar o app, o registro sumia
            // sem deixar nenhum rastro pra investigar depois.
            var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
            var criptografia = new ServicoCriptografia(chave);
            var persistenciaLocal = new PersistenciaLocal(criptografia, TesteUtil.CriarPastaTemporaria());
            var repositorioLocal = new RepositorioSenha(persistenciaLocal, chave);
            var servico = new ServicoSenha(repositorioLocal, criptografia);

            var janela = new JanelaPrincipal(servico, chave, criptografia, repositorioLocal);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var arquivoBanco = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GS_ConflitoLog_" + Guid.NewGuid().ToString("N") + ".db");
            var cfg = new ConexaoBanco { Tipo = TipoBanco.SQLite, Banco = arquivoBanco };
            var bd = new ServicoBancoDados();
            await bd.CriarTabelaAsync(cfg);
            await bd.GarantirColunasAsync(cfg);

            try
            {
                var repoBanco = new RepositorioSenhaBanco(cfg, criptografia);
                await repoBanco.AdicionarAsync(new Senha
                {
                    Id = Guid.NewGuid(),
                    NomeServico = "ServicoComIntegridadeViolada",
                    Usuario = "u",
                    SenhaHash = criptografia.Criptografar("SenhaQualquer@1"),
                    Categoria = Categoria.Personal,
                    DataCriacao = DateTime.UtcNow,
                    DataAtualizacao = DateTime.UtcNow
                });

                await using (var con = bd.CriarConexao(cfg))
                {
                    await con.OpenAsync();
                    await using var cmd = con.CreateCommand();
                    cmd.CommandText = "UPDATE CofreDeSenhas SET hmac = 'adulterado'";
                    await cmd.ExecuteNonQueryAsync();
                }

                var caminhoLog = System.IO.Path.Combine(CaminhosApp.PastaDados, "logs", "erros.log");

                await janela.ConectarAsync(cfg, persistir: false, silencioso: true);

                // Não compara com um snapshot "antes": outros testes do mesmo processo
                // também escrevem nesse log, então outro pode acrescentar uma linha
                // entre a leitura "antes" e esta gravação — o nome do serviço usado
                // aqui já é específico o bastante pra não colidir com outra coisa.
                var conteudo = File.ReadAllText(caminhoLog);
                Assert.Contains("ConflitoSincronizacao", conteudo);
                Assert.Contains("ServicoComIntegridadeViolada", conteudo);
            }
            finally
            {
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
        public async Task BarraLateral_CadaBotaoDeNavegacaoTemNomeAcessivelProprio()
        {
            // Sem AutomationProperties.Name no Button, o peer de automação cai no nome do
            // tipo do primeiro filho ("Avalonia.Controls.Grid") — os 9 botões da barra
            // lateral ficam indistinguíveis para um leitor de tela.
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var esperado = new (string Nome, string Chave)[]
            {
                ("BtnNavCofre", "Nav.Vault"),
                ("BtnNavFavoritas", "Nav.Favorites"),
                ("BtnNavRecentes", "Nav.Recent"),
                ("BtnNavLixeira", "Nav.Trash"),
                ("BtnCatPessoal", "Category.Personal"),
                ("BtnCatSocial", "Category.Social"),
                ("BtnCatTrabalho", "Category.Work"),
                ("BtnCatFinancas", "Category.Finance"),
                ("BtnCatOutro", "Category.Other"),
            };

            var botoes = janela.GetVisualDescendants().OfType<Button>().ToList();
            foreach (var (nome, chaveTexto) in esperado)
            {
                var botao = botoes.Single(b => b.Name == nome);
                Assert.Equal(Idioma.Texto(chaveTexto), AutomationProperties.GetName(botao));
            }

            janela.Close();
        }

        [AvaloniaFact]
        public async Task DesabilitarInteracaoAteReiniciar_CongelaAJanelaEBloqueiaOsAtalhosQueGravamNoCofre()
        {
            // Depois que a troca de senha mestra grava auth.dat/vault na chave nova, a
            // janela continua com _servicoSenha/_criptografia na chave ANTIGA até o
            // processo reiniciar — qualquer gravação nesse intervalo regrava o cofre com
            // a chave errada e o deixa ilegível depois do restart. A janela precisa
            // ficar sem nenhuma interação capaz de gravar até lá.
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();
            var totalAntes = (await servico.ListarTodosAsync()).Count;

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.DesabilitarInteracaoAteReiniciar();

            Assert.False(janela.IsEnabled);

            janela.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.N,
                KeyModifiers = KeyModifiers.Control
            });
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.DoesNotContain(janela.OwnedWindows, w => w is JanelaCriarSenha);
            Assert.Equal(totalAntes, (await servico.ListarTodosAsync()).Count);

            janela.Close();
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
        public async Task PainelDetalhes_ComOperacaoJaEmAndamento_CliqueEmLixeiraNaoFechaOPainel()
        {
            var (servico, chave, criptografia) = CriarServicoComCriptografia();
            await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
            await servico.PersistirAsync();
            var item = (await servico.ListarTodosAsync()).Single();

            var janela = new JanelaPrincipal(servico, chave, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.AbrirDetalhes(item);

            // Mesmo cenário do Fechar/Excluir acima, mas pelo botão "Lixeira" da barra
            // lateral — antes da correção, nada impedia ele de fechar o painel de
            // detalhes enquanto um Salvar/Excluir ainda estava em voo, e quando a
            // operação terminasse ela reabria o painel por cima da lixeira.
            janela._detalhesOperacaoEmAndamento = true;

            janela.Encontrar<Button>("BtnNavLixeira").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.True(janela.Encontrar<Border>("PainelDetalhes").IsVisible);
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
        public async Task CopiarUsuarioDetalhes_ComLimpezaAutomaticaAtiva_AgendaLimpezaComoACopiaDeSenha()
        {
            // Antes da correção, o botão de copiar usuário do painel de detalhes ia
            // direto pro clipboard sem nunca agendar limpeza — só senha e TOTP eram
            // apagados sozinhos do clipboard depois de alguns segundos.
            var segundosOriginal = Preferencias.SegundosLimpezaClipboard;
            try
            {
                Preferencias.SegundosLimpezaClipboard = 5;

                var (servico, chave, criptografia) = CriarServicoComCriptografia();
                await servico.CriarSenhaAsync("ServicoX", "usuario.detalhes", "SenhaOriginal@1", Categoria.Personal);
                await servico.PersistirAsync();
                var item = (await servico.ListarTodosAsync()).Single();

                var janela = new JanelaPrincipal(servico, chave, criptografia);
                janela.Show();
                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                janela.AbrirDetalhes(item);
                janela.Encontrar<Button>("BtnCopiarUsuarioDetalhes").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                string? textoClipboard = null;
                await TesteUtil.AguardarAsync(() =>
                {
                    textoClipboard = janela.Clipboard?.TryGetTextAsync().GetAwaiter().GetResult();
                    return !string.IsNullOrEmpty(textoClipboard);
                });
                Assert.Equal("usuario.detalhes", textoClipboard);

                var botao = janela.Encontrar<Button>("BtnCopiarUsuarioDetalhes");
                var mensagemEsperada = Idioma.Formatar("Row.UserCopiedClearing", 5);
                Assert.Equal(mensagemEsperada, ToolTip.GetTip(botao));
            }
            finally
            {
                Preferencias.SegundosLimpezaClipboard = segundosOriginal;
            }
        }

        [AvaloniaFact]
        public async Task LimparCofre_Click_ExigeReautenticacaoAntesDeMexerNoCofre()
        {
            // Antes da correção, "Limpar cofre" bastava um clique de confirmação —
            // sem senha nenhuma — pra mover todo o cofre pra lixeira numa sessão já
            // desbloqueada, ao contrário de "Excluir cofre" (que sempre exigiu
            // reautenticação). O clique agora precisa abrir o mesmo diálogo de senha
            // mestra antes de chegar em qualquer confirmação — e o cofre não pode ter
            // sido tocado enquanto esse diálogo ainda está esperando.
            var bancoOriginal = Preferencias.UltimoBanco;
            try
            {
                // Sem isto, um perfil de banco "conectado" deixado por outro teste faz
                // IniciarAsync tentar reconectar em vez de CarregarSenhasAsync — ver a
                // mesma correção no teste de edição inline do item 27.
                Preferencias.UltimoBanco = null;

                var (servico, chave, criptografia) = CriarServicoComCriptografia();
                await servico.CriarSenhaAsync("ServicoX", "user", "SenhaOriginal@1", Categoria.Personal);
                await servico.PersistirAsync();

                var janela = new JanelaPrincipal(servico, chave, criptografia);
                janela.Show();
                await TesteUtil.AguardarAsync(() =>
                    janela.GetVisualDescendants().OfType<LinhaSenha>().Any());

                janela.LimparCofre_Click(janela, new RoutedEventArgs());
                await TesteUtil.AguardarAsync(() =>
                    janela.OwnedWindows.OfType<JanelaConfirmarSenhaMestra>().Any());

                Assert.Contains(janela.OwnedWindows, w => w is JanelaConfirmarSenhaMestra);

                var ativas = await servico.ListarTodosAsync();
                Assert.Single(ativas);
                Assert.Empty(await servico.ListarLixeiraAsync());
            }
            finally
            {
                Preferencias.UltimoBanco = bancoOriginal;
            }
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

        [AvaloniaFact]
        public async Task AtualizarAgora_Click_PedeConfirmacaoComVersaoENotasAntesDeIniciarAAtualizacao()
        {
            // Antes da correção, "Atualizar agora" já disparava o download e a
            // instalação silenciosa direto — sem mostrar qual versão ia entrar nem o
            // que mudava nela, o app podia se fechar sozinho pra aplicar a atualização
            // sem o usuário nunca ter visto do que se tratava.
            var (servico, chave) = CriarServico();
            var janela = new JanelaPrincipal(servico, chave);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.ExibirAtualizacaoDisponivel(new AtualizacaoDisponivel("v9.9.9", "- Item um\n- Item dois"));

            janela.Encontrar<Button>("BtnAtualizarAgora").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => janela.OwnedWindows.OfType<CaixaMensagem>().Any());

            var dialogo = janela.OwnedWindows.OfType<CaixaMensagem>().Single();
            Assert.Equal(Idioma.Formatar("Update.ConfirmTitle", "v9.9.9"), dialogo.Title);

            // Cancelar aqui não pode chegar a chamar ServicoAtualizacao.AtualizarAgoraAsync
            // (rede de verdade) — o texto do botão só muda pra "Baixando..." se o fluxo
            // de download/instalação for iniciado.
            dialogo.BotaoPorTexto(Idioma.Texto("Common.No")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.Equal(Idioma.Texto("Update.Now"), janela.Encontrar<TextBlock>("LblBtnAtualizarAgora").Text);
        }
    }
}
