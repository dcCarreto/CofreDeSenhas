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
    }
}
