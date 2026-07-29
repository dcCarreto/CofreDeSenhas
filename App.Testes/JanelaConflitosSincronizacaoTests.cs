using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using CofreDeSenhas;
using CofreDeSenhas.Janelas;
using GerenciadorDeSenhas.Modelos;

namespace App.Testes
{
    public class JanelaConflitosSincronizacaoTests
    {
        [AvaloniaFact]
        public async Task Abrir_ComConflitos_MostraNomeDoServicoDeCadaUm()
        {
            var conflitos = new List<ConflitoSincronizacao>
            {
                new()
                {
                    SenhaId = Guid.NewGuid(),
                    NomeServico = "Servico Concorrente",
                    Tipo = TipoConflitoSincronizacao.EdicaoConcorrente,
                    DetectadoEmUtc = DateTime.UtcNow
                },
                new()
                {
                    SenhaId = Guid.NewGuid(),
                    NomeServico = "Servico Adulterado",
                    Tipo = TipoConflitoSincronizacao.IntegridadeViolada,
                    DetectadoEmUtc = DateTime.UtcNow
                }
            };

            var janela = new JanelaConflitosSincronizacao(conflitos);
            janela.Show();
            await TesteUtil.AguardarAsync(() => false, tentativas: 5);

            var textos = janela.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains("Servico Concorrente", textos);
            Assert.Contains("Servico Adulterado", textos);
        }
    }
}
