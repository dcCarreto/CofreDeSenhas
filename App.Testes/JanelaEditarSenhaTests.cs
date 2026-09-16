using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    // Duas destas classes mexem em Preferencias.Sincronizacao (estático global) —
    // mesmo cuidado de JanelaPrincipalTests/JanelaSincronizacaoTests, senão testes
    // rodando em paralelo em outras classes veem o valor mudar por baixo.
    [Collection("Preferencias")]
    public class JanelaEditarSenhaTests
    {
        private static async Task<(IServicoSenha servico, IServicoCriptografia criptografia, Senha senha)> CriarServicoComCredencialAsync()
        {
            var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
            var criptografia = new ServicoCriptografia(chave);
            var persistencia = new PersistenciaLocal(criptografia, TesteUtil.CriarPastaTemporaria());
            var repositorio = new RepositorioSenha(persistencia, chave);
            var servico = new ServicoSenha(repositorio, criptografia);

            await servico.CriarSenhaAsync("Servico Original", "usuario.original", "SenhaOriginal123!", Categoria.Personal);
            var senha = (await servico.ListarTodosAsync())[0];
            return (servico, criptografia, senha);
        }

        [AvaloniaFact]
        public async Task Construtor_RegistraAnunciadorParaLeitorDeTela()
        {
            var (servico, criptografia, senha) = await CriarServicoComCredencialAsync();
            var janela = new JanelaEditarSenha(servico, senha, criptografia);
            janela.Show();

            Acessibilidade.Anunciar(janela, "mensagem-de-teste-leitor-de-tela", forcar: true);

            var anunciador = janela.Encontrar<TextBlock>("LblAnuncioLeitorTela");
            await TesteUtil.AguardarAsync(() => anunciador.Text == "mensagem-de-teste-leitor-de-tela");

            Assert.Equal("mensagem-de-teste-leitor-de-tela", anunciador.Text);
        }

        [AvaloniaFact]
        public async Task Salvar_AtualizaNomeDoServico()
        {
            var (servico, criptografia, senha) = await CriarServicoComCredencialAsync();
            var janela = new JanelaEditarSenha(servico, senha, criptografia);
            janela.Show();

            janela.Encontrar<TextBox>("TxtNomeServico").Text = "Servico Renomeado";

            janela.BotaoPorTexto(Idioma.Texto("Common.Save")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            List<Senha> lista = new();
            await TesteUtil.AguardarAsync(() =>
            {
                lista = servico.ListarTodosAsync().GetAwaiter().GetResult();
                return lista.Count > 0 && lista[0].NomeServico == "Servico Renomeado";
            });

            var atualizada = Assert.Single(lista);
            Assert.Equal("Servico Renomeado", atualizada.NomeServico);
            Assert.Equal("usuario.original", atualizada.Usuario);
        }

        [AvaloniaFact]
        public async Task TrocarTipoEVoltar_PreservaCampoExtraJaDigitado()
        {
            var (servico, criptografia, senha) = await CriarServicoComCredencialAsync();
            var janela = new JanelaEditarSenha(servico, senha, criptografia);
            janela.Show();

            var cmbTipo = janela.Encontrar<ComboBox>("CmbTipo");
            cmbTipo.SelectedIndex = TemplatesCredencial.ObterIndice(TipoCredencial.Cartao);
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            janela.GetVisualDescendants().OfType<TextBox>().First(t => (string?)t.Tag == "cvv").Text = "123";

            // Troca pra um tipo que não tem "cvv" e volta pro Cartão, sem salvar —
            // antes desta correção, isso apagava o valor recém-digitado porque só os
            // campos do tipo anterior (não os de todos os tipos já visitados na sessão)
            // ficavam disponíveis pra repovoar o painel.
            cmbTipo.SelectedIndex = TemplatesCredencial.ObterIndice(TipoCredencial.Login);
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);
            cmbTipo.SelectedIndex = TemplatesCredencial.ObterIndice(TipoCredencial.Cartao);
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var cvvDepois = janela.GetVisualDescendants().OfType<TextBox>().First(t => (string?)t.Tag == "cvv");
            Assert.Equal("123", cvvDepois.Text);
        }

        [AvaloniaFact]
        public async Task CodigoRecuperacao_CampoNaoFicaEsmagadoContraOsBotoesDeAcao()
        {
            var (servico, criptografia, senha) = await CriarServicoComCredencialAsync();
            senha.CodigosRecuperacao.Add(new CodigoRecuperacao { Codigo = criptografia.Criptografar("CODE-AAAA-1111") });
            await servico.PersistirAsync();

            var janela = new JanelaEditarSenha(servico, senha, criptografia);
            janela.Show();
            await TesteUtil.AguardarAsync(() =>
                janela.GetVisualDescendants().OfType<TextBox>()
                    .Any(t => AutomationProperties.GetName(t) == Idioma.Texto("A11y.RecoveryCode")));

            var campo = janela.GetVisualDescendants().OfType<TextBox>()
                .First(t => AutomationProperties.GetName(t) == Idioma.Texto("A11y.RecoveryCode"));

            // Antes da correção o campo dividia a linha da Grid com três botões de
            // rótulo longo e sobrava com ~64px de largura; agora fica sozinho na
            // própria linha, com os botões abaixo.
            Assert.IsNotType<Grid>(campo.Parent);
            Assert.True(campo.Bounds.Width > 200, $"largura do campo do código de recuperação: {campo.Bounds.Width}");
        }

        [AvaloniaFact]
        public async Task Abrir_ComAnexoExistente_MostraOAnexoNaLista()
        {
            var (servico, criptografia, senha) = await CriarServicoComCredencialAsync();
            var servicoAnexos = new ServicoAnexos(criptografia, TesteUtil.CriarPastaTemporaria());
            await servicoAnexos.AdicionarAsync(senha, "documento.pdf", new byte[] { 1, 2, 3, 4 });
            await servico.PersistirAsync();

            var janela = new JanelaEditarSenha(servico, senha, criptografia, servicoAnexos);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.Contains(janela.GetVisualDescendants().OfType<TextBlock>(),
                tb => AutomationProperties.GetName(tb) == "documento.pdf");
        }

        [AvaloniaFact]
        public async Task Abrir_ComAnexoAindaNaoBaixado_MostraIndicadorNaLinha()
        {
            // Metadado chegado por sincronização (fase B/C) sem o arquivo físico local
            // ainda — mesmo estado de um anexo publicado por outro dispositivo antes
            // de alguém clicar em "Baixar" aqui.
            var (servico, criptografia, senha) = await CriarServicoComCredencialAsync();
            senha.Anexos.Add(new AnexoSenha { NomeArquivo = "remoto.pdf", TamanhoBytes = 500 });
            await servico.PersistirAsync();

            var servicoAnexos = new ServicoAnexos(criptografia, TesteUtil.CriarPastaTemporaria());
            var janela = new JanelaEditarSenha(servico, senha, criptografia, servicoAnexos);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            Assert.Contains(janela.GetVisualDescendants().OfType<TextBlock>(),
                tb => tb.Text != null && tb.Text.Contains("remoto.pdf") && tb.Text.Contains(Idioma.Texto("Entry.AttachmentNotDownloaded")));
        }

        [AvaloniaFact]
        public async Task ObterConteudoAnexoAsync_ComArquivoLocalAusenteEAnexoPublicado_BaixaDaPastaEGravaCopiaLocal()
        {
            var (servico, criptografia, senha) = await CriarServicoComCredencialAsync();
            var anexo = new AnexoSenha { NomeArquivo = "remoto.pdf", TamanhoBytes = 4 };
            senha.Anexos.Add(anexo);
            await servico.PersistirAsync();

            var pastaSync = TesteUtil.CriarPastaTemporaria();
            var chaveSync = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeSync123!");
            var servicoSincronizacao = new ServicoSincronizacao(new ServicoCriptografia(chaveSync));
            var conteudo = new byte[] { 9, 8, 7, 6 };
            await servicoSincronizacao.PublicarAnexoAsync(pastaSync, anexo.Id, conteudo);

            var perfilOriginal = Preferencias.Sincronizacao;
            Preferencias.Sincronizacao = new PerfilSincronizacao
            {
                Pasta = pastaSync,
                Salt = Convert.ToBase64String(ServicoSincronizacao.GerarSalt())
            };

            try
            {
                var servicoAnexos = new ServicoAnexos(criptografia, TesteUtil.CriarPastaTemporaria());
                var janela = new JanelaEditarSenha(servico, senha, criptografia, servicoAnexos, servicoSincronizacao);

                Assert.False(servicoAnexos.TemArquivoLocal(anexo.Id));

                var baixado = await janela.ObterConteudoAnexoAsync(anexo);

                Assert.Equal(conteudo, baixado);
                Assert.True(servicoAnexos.TemArquivoLocal(anexo.Id));
            }
            finally
            {
                Preferencias.Sincronizacao = perfilOriginal;
            }
        }

        [AvaloniaFact]
        public async Task ObterConteudoAnexoAsync_SemArquivoLocalNemSincronizacaoConfigurada_LancaErroLocalizavel()
        {
            var (servico, criptografia, senha) = await CriarServicoComCredencialAsync();
            var anexo = new AnexoSenha { NomeArquivo = "remoto.pdf", TamanhoBytes = 4 };
            senha.Anexos.Add(anexo);
            await servico.PersistirAsync();

            var perfilOriginal = Preferencias.Sincronizacao;
            Preferencias.Sincronizacao = null;

            try
            {
                var servicoAnexos = new ServicoAnexos(criptografia, TesteUtil.CriarPastaTemporaria());
                var janela = new JanelaEditarSenha(servico, senha, criptografia, servicoAnexos);

                var erro = await Assert.ThrowsAsync<GerenciadorDeSenhas.Excecoes.ErroLocalizavel>(
                    () => janela.ObterConteudoAnexoAsync(anexo));

                Assert.Equal("Attachment.Error.NotAvailable", erro.Chave);
            }
            finally
            {
                Preferencias.Sincronizacao = perfilOriginal;
            }
        }
    }
}
