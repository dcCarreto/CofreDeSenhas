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
    }
}
