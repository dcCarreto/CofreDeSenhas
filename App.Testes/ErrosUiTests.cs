using CofreDeSenhas;
using GerenciadorDeSenhas.Excecoes;
using Xunit;

namespace App.Testes
{
    public class ErrosUiTests
    {
        [Fact]
        public void MensagemAmigavel_ComErroLocalizavel_UsaATraducaoDaChave()
        {
            var ex = new ErroLocalizavel("Auth.Error.PasswordTooShort", 8);

            var mensagem = ErrosUi.MensagemAmigavel(ex);

            Assert.Equal(Idioma.Formatar("Auth.Error.PasswordTooShort", 8), mensagem);
        }

        [Fact]
        public void MensagemAmigavel_ComExcecaoGenerica_UsaPrefixoTraduzidoEmVezDeMostrarSoOTextoCru()
        {
            // Antes da correção, uma exceção que não segue o padrão ILocalizavel deste
            // app (IOException, erro de driver de banco, timeout de rede etc.) aparecia
            // pro usuário como texto cru em inglês, sem passar por nenhuma tradução —
            // quebrando a promessa de mensagens de erro traduzidas pra quem usa o app em
            // qualquer outro idioma.
            var ex = new InvalidOperationException("something broke");

            var mensagem = ErrosUi.MensagemAmigavel(ex);

            Assert.Equal(Idioma.Formatar("Common.UnexpectedError", "something broke"), mensagem);
        }

        [Fact]
        public void MensagemAmigavel_ComExcecaoGenerica_UsaSoAPrimeiraLinhaDaMensagem()
        {
            var ex = new InvalidOperationException("linha um\nlinha dois");

            var mensagem = ErrosUi.MensagemAmigavel(ex);

            Assert.Equal(Idioma.Formatar("Common.UnexpectedError", "linha um"), mensagem);
        }

        [Fact]
        public void MensagemAmigavel_ComCaminhoDoPerfilNaMensagem_RedigeAntesDeMostrar()
        {
            var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var ex = new InvalidOperationException($"Could not find file '{perfil}\\GerenciadorSenhas\\senhas.json.enc'.");

            var mensagem = ErrosUi.MensagemAmigavel(ex);

            Assert.DoesNotContain(perfil, mensagem);
            Assert.Contains("%USERPROFILE%", mensagem);
        }
    }
}
