using CofreDeSenhas;

namespace App.Testes
{
    public class DiagnosticoTests
    {
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
            // Sobrecarga usada por eventos que não são exceções (ex.: conflito de
            // sincronização) — precisa gravar igual à sobrecarga de Exception: acrescenta
            // no arquivo em vez de sobrescrever, e mantém o mesmo formato "[contexto]".
            // Um marcador único (guid) identifica a linha desta chamada sem depender da
            // ordem em que outros testes do processo tenham escrito no mesmo log.
            var caminho = Path.Combine(CaminhosApp.PastaDados, "logs", "erros.log");
            var marcador = "marcador-" + Guid.NewGuid();

            Diagnostico.Registrar(marcador, "ContextoDeTeste");

            var conteudo = File.ReadAllText(caminho);
            Assert.Contains($"[ContextoDeTeste] {marcador}", conteudo);
        }
    }
}
