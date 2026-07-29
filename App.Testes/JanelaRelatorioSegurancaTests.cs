using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    public class JanelaRelatorioSegurancaTests
    {
        [AvaloniaFact]
        public async Task Abrir_MostraAPontuacaoDoRelatorio()
        {
            var caminhoOriginal = HistoricoPontuacaoSeguranca.CaminhoOverride;
            try
            {
                HistoricoPontuacaoSeguranca.CaminhoOverride =
                    Path.Combine(TesteUtil.CriarPastaTemporaria(), "pontuacao-historico.json");

                var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
                var criptografia = new ServicoCriptografia(chave);
                var persistencia = new PersistenciaLocal(criptografia, TesteUtil.CriarPastaTemporaria());
                var repositorio = new RepositorioSenha(persistencia, chave);
                var servico = new ServicoSenha(repositorio, criptografia);

                await servico.CriarSenhaAsync("Servico Forte", "usuario", "SenhaMuitoForte!987#xyz", Categoria.Personal,
                    url: "https://servico.com", totpSegredo: "JBSWY3DPEHPK3PXP");
                var senhas = await servico.ListarTodosAsync();

                var auditoria = new ServicoAuditoriaSenha().Auditar(senhas, s => criptografia.Descriptografar(s.SenhaHash));
                var relatorio = ServicoRelatorioSeguranca.Gerar(senhas, auditoria);

                var janela = new JanelaRelatorioSeguranca(relatorio, vazamentosVerificados: false,
                    reverificarVazamentos: () => Task.FromResult(relatorio));
                janela.Show();

                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                var lblPontuacao = janela.Encontrar<TextBlock>("LblPontuacao");
                Assert.Equal(relatorio.Pontuacao.ToString(), lblPontuacao.Text);
            }
            finally
            {
                HistoricoPontuacaoSeguranca.CaminhoOverride = caminhoOriginal;
            }
        }

        [AvaloniaFact]
        public async Task Abrir_ComHistoricoDeDuasOuMaisPontuacoes_MostraATendencia()
        {
            var caminhoOriginal = HistoricoPontuacaoSeguranca.CaminhoOverride;
            try
            {
                HistoricoPontuacaoSeguranca.CaminhoOverride =
                    Path.Combine(TesteUtil.CriarPastaTemporaria(), "pontuacao-historico.json");

                var chave = new AutenticacaoMestra(TesteUtil.CriarPastaTemporaria()).CriarSenhaMestra("SenhaDeTeste123!");
                var criptografia = new ServicoCriptografia(chave);
                var persistencia = new PersistenciaLocal(criptografia, TesteUtil.CriarPastaTemporaria());
                var repositorio = new RepositorioSenha(persistencia, chave);
                var servico = new ServicoSenha(repositorio, criptografia);
                var senhas = await servico.ListarTodosAsync();
                var auditoria = new ServicoAuditoriaSenha().Auditar(senhas, s => criptografia.Descriptografar(s.SenhaHash));
                var relatorio = ServicoRelatorioSeguranca.Gerar(senhas, auditoria);

                HistoricoPontuacaoSeguranca.RegistrarPontuacao(70);
                var pontos = HistoricoPontuacaoSeguranca.Carregar();
                pontos[0].DataUtc = DateTime.UtcNow.AddDays(-1);
                File.WriteAllText(HistoricoPontuacaoSeguranca.CaminhoOverride!,
                    System.Text.Json.JsonSerializer.Serialize(pontos));

                var janela = new JanelaRelatorioSeguranca(relatorio, vazamentosVerificados: false,
                    reverificarVazamentos: () => Task.FromResult(relatorio));
                janela.Show();

                await TesteUtil.AguardarAsync(() => false, tentativas: 5);

                Assert.True(janela.Encontrar<StackPanel>("PainelTendencia").IsVisible);
                Assert.Equal(2, janela.Encontrar<StackPanel>("PainelBarrasTendencia").Children.Count);
            }
            finally
            {
                HistoricoPontuacaoSeguranca.CaminhoOverride = caminhoOriginal;
            }
        }
    }
}
