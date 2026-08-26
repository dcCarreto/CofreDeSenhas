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
            //
            // Não compara o conteúdo do arquivo antes/depois byte a byte: as duas TFMs
            // deste projeto de teste rodam em paralelo como processos separados contra
            // o mesmo erros.log real (%APPDATA%), então outro processo pode acrescentar
            // uma linha entre a leitura "antes" e a gravação daqui — um marcador único
            // (guid) já garante que a linha é desta chamada, sem depender de ordem.
            var caminho = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GerenciadorSenhas", "logs", "erros.log");
            var marcador = "marcador-" + Guid.NewGuid();

            Diagnostico.Registrar(marcador, "ContextoDeTeste");

            var conteudo = File.ReadAllText(caminho);
            Assert.Contains($"[ContextoDeTeste] {marcador}", conteudo);
        }
    }
}
