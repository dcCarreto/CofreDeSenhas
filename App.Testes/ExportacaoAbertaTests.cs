using System.Text.Json;
using CofreDeSenhas;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace App.Testes
{
    public class ExportacaoAbertaTests
    {
        private static SenhaExportada Item(string nome, string senha) => new()
        {
            NomeServico = nome,
            Usuario = "pessoa@exemplo.com",
            Senha = senha,
            Categoria = Categoria.Personal,
            DataCriacao = DateTime.UtcNow,
            DataAtualizacao = DateTime.UtcNow
        };

        [Fact]
        public void CsvBitwarden_VoltaIgualPeloProprioImportador()
        {
            var comTudo = Item("Banco, Agência \"Centro\"", "s3nh@,com\"aspas\"");
            comTudo.Url = "https://banco.exemplo.com";
            comTudo.Notas = "linha 1\nlinha 2, com vírgula";
            comTudo.TotpSegredo = "JBSWY3DPEHPK3PXP";
            comTudo.Favorito = true;
            var simples = Item("Email", "OutraSenha@1");
            simples.Categoria = Categoria.Work;

            var csv = ExportacaoAberta.CsvBitwarden(new[] { comTudo, simples });
            var importado = new ServicoImportacaoCsv().Importar(csv);

            Assert.Equal("Bitwarden", importado.FormatoDetectado);
            Assert.Equal(2, importado.Itens.Count);
            var lido = importado.Itens.Single(i => i.NomeServico == comTudo.NomeServico);
            Assert.Equal(comTudo.Usuario, lido.Usuario);
            Assert.Equal(comTudo.Senha, lido.Senha);
            Assert.Equal(comTudo.Url, lido.Url);
            Assert.Equal(comTudo.Notas, lido.Notas);
            Assert.Equal(comTudo.TotpSegredo, lido.TotpSegredo);
            Assert.True(lido.Favorito);
            var lidoSimples = importado.Itens.Single(i => i.NomeServico == "Email");
            Assert.False(lidoSimples.Favorito);
            Assert.Equal(Categoria.Work, lidoSimples.Categoria);
        }

        [Fact]
        public void JsonBitwarden_LevaHistoricoCamposOcultosEPastas()
        {
            var cartao = Item("Cartão", "1234 5678 9012 3456");
            cartao.Tipo = TipoCredencial.Cartao;
            cartao.CamposExtras["cvv"] = "123";
            cartao.CamposExtras["validade"] = "12/30";
            cartao.Etiquetas = new List<string> { "viagem", "pessoal" };
            cartao.CodigosRecuperacao = new List<CodigoRecuperacaoExportado> { new() { Codigo = "AAAA-BBBB", Usado = true } };
            cartao.Historico = new List<HistoricoSenhaExportada> { new() { Senha = "antiga", DataAlteracao = DateTime.UtcNow.AddDays(-3) } };
            var trabalho = Item("Intranet", "Senha@Trabalho1");
            trabalho.Categoria = Categoria.Work;
            trabalho.Url = "https://intranet.exemplo.com";

            using var doc = JsonDocument.Parse(ExportacaoAberta.JsonBitwarden(new[] { cartao, trabalho }));
            var raiz = doc.RootElement;

            Assert.False(raiz.GetProperty("encrypted").GetBoolean());
            Assert.Equal(2, raiz.GetProperty("folders").GetArrayLength());
            var itens = raiz.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal(2, itens.Count);

            var c = itens.Single(i => i.GetProperty("name").GetString() == "Cartão");
            Assert.Equal("1234 5678 9012 3456", c.GetProperty("login").GetProperty("password").GetString());
            Assert.Equal("antiga", c.GetProperty("passwordHistory")[0].GetProperty("password").GetString());
            var campos = c.GetProperty("fields").EnumerateArray()
                .ToDictionary(f => f.GetProperty("value").GetString()!, f => f.GetProperty("type").GetInt32());
            Assert.Equal(1, campos["123"]);
            Assert.Equal(0, campos["12/30"]);
            Assert.Equal(0, campos["viagem, pessoal"]);
            Assert.Equal(1, campos["AAAA-BBBB"]);

            var t = itens.Single(i => i.GetProperty("name").GetString() == "Intranet");
            Assert.Equal("https://intranet.exemplo.com", t.GetProperty("login").GetProperty("uris")[0].GetProperty("uri").GetString());
            Assert.NotEqual(c.GetProperty("folderId").GetString(), t.GetProperty("folderId").GetString());
        }
    }
}
