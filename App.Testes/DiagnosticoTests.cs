using System.Text.Json;
using CofreDeSenhas;

namespace App.Testes
{
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
    }
}
