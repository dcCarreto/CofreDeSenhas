using System.Text.Json;
using CofreDeSenhas;

namespace App.Testes
{
    [Collection("Preferencias")]
    public class DiagnosticoTests : IDisposable
    {
        private readonly string _log;

        public DiagnosticoTests()
        {
            _log = Path.Combine(TesteUtil.CriarPastaTemporaria(), "erros.log");
            Diagnostico.CaminhoLogTestes = _log;
        }

        public void Dispose() => Diagnostico.CaminhoLogTestes = null;

        [Fact]
        public void Redigir_ComCaminhoDoPerfilDoUsuario_SubstituiPeloPlaceholder()
        {
            var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var mensagem = $@"Could not find file '{perfil}\AppData\Roaming\GerenciadorSenhas\senhas.json.enc'.";

            var redigida = Diagnostico.Redigir(mensagem);

            Assert.DoesNotContain(perfil, redigida, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("%USERPROFILE%", redigida);
        }

        [Fact]
        public void Redigir_SemCaminhoDoPerfil_RetornaMensagemInalterada()
        {
            const string mensagem = "The computed authentication tag did not match the input authentication tag.";

            Assert.Equal(mensagem, Diagnostico.Redigir(mensagem));
        }

        [Fact]
        public void Registrar_ComMensagemDeTexto_AcrescentaLinhaComContextoNoFormatoEsperado()
        {
            var marcador = "marcador-" + Guid.NewGuid();

            Diagnostico.Registrar(marcador, "ContextoDeTeste");

            Assert.Contains($"[ContextoDeTeste] {marcador}", File.ReadAllText(_log));
        }

        [Fact]
        public void Registrar_ComJsonException_NaoGravaAMensagemQuePodeEcoarOJsonDescifrado()
        {
            var segredoNoJson = "conta-secreta-" + Guid.NewGuid().ToString("N");
            var jsonEx = new JsonException(
                message: $"'{segredoNoJson}' is an invalid start of a value.",
                path: "$[0].Notas", lineNumber: 3, bytePositionInLine: 12);

            Diagnostico.Registrar(jsonEx, "CarregarSenhas");
            Diagnostico.Registrar(new InvalidOperationException("falha ao ler o cofre", jsonEx), "CarregarSenhas");

            var conteudo = File.ReadAllText(_log);
            Assert.DoesNotContain(segredoNoJson, conteudo);
            Assert.Contains("JsonException em $[0].Notas", conteudo);
            Assert.Contains("linha 3", conteudo);
        }

        [Fact]
        public void Registrar_ComExcecaoLancada_GravaAPilhaDeChamadas()
        {
            try { MetodoQueFalha(); }
            catch (Exception ex) { Diagnostico.Registrar(ex, "Pilha"); }

            var conteudo = File.ReadAllText(_log);
            Assert.Contains("[Pilha] InvalidOperationException: falhou", conteudo);
            Assert.Contains(nameof(MetodoQueFalha), conteudo);
        }

        private static void MetodoQueFalha() => throw new InvalidOperationException("falhou");

        [Fact]
        public void Registrar_ComLogCheio_GuardaOAnteriorEmPontoUm()
        {
            File.WriteAllText(_log, new string('x', 1_000_001));

            Diagnostico.Registrar("depois-da-virada");

            Assert.True(File.Exists(_log + ".1"));
            Assert.DoesNotContain("xxx", File.ReadAllText(_log));
        }

        [Fact]
        public void Redigir_ComSenhaDeStringDeConexao_MascaraOValor()
        {
            var redigida = Diagnostico.Redigir("Host=db;Username=app;Password=Segr3do!;Port=5432");

            Assert.DoesNotContain("Segr3do!", redigida);
            Assert.Contains("Password=***", redigida);
        }

        [Fact]
        public void Redigir_ComNomeDoUsuarioDoSistema_SubstituiPeloPlaceholder()
        {
            if (Environment.UserName.Length < 3)
                return;

            var redigida = Diagnostico.Redigir($"acesso negado para {Environment.UserName}.");

            Assert.Contains("%USERNAME%", redigida);
        }

        [Fact]
        public void MontarRelatorio_IncluiAmbienteELogSemONomeDoServicoEmConflito()
        {
            var marcador = "erro-" + Guid.NewGuid();
            Diagnostico.Registrar(marcador, "Teste");
            Diagnostico.Registrar("IntegridadeViolada em \"Banco Secreto\" (id 42)", "ConflitoSincronizacao");

            var relatorio = Diagnostico.MontarRelatorio();

            Assert.Contains("== Ambiente ==", relatorio);
            Assert.Contains(Diagnostico.VersaoApp(), relatorio);
            Assert.Contains(marcador, relatorio);
            Assert.DoesNotContain("Banco Secreto", relatorio);
            Assert.Contains("IntegridadeViolada em \"***\" (id 42)", relatorio);
        }

        [Fact]
        public void MontarRelatorio_ComLogGrande_MantemSoOFim()
        {
            File.WriteAllText(_log, new string('a', 5_000) + "FIM-DO-LOG");

            var relatorio = Diagnostico.MontarRelatorio(maxCaracteresLog: 100);

            Assert.Contains("FIM-DO-LOG", relatorio);
            Assert.DoesNotContain(new string('a', 200), relatorio);
        }
    }
}
