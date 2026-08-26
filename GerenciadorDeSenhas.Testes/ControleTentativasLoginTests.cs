using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class ControleTentativasLoginTests
{
    [Fact]
    public void RegistrarFalha_AntesDoLimite_NaoRetornaBloqueio()
    {
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            var controle = new ControleTentativasLogin(pasta);

            for (var i = 0; i < ControleTentativasLogin.LimiteTentativas - 1; i++)
            {
                var (tentativas, bloqueioAte) = controle.RegistrarFalha();
                Assert.Equal(i + 1, tentativas);
                Assert.Null(bloqueioAte);
            }
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }

    [Fact]
    public void RegistrarFalha_AoAtingirOLimite_RetornaBloqueio()
    {
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            var controle = new ControleTentativasLogin(pasta);
            (int Tentativas, DateTime? BloqueioAteUtc) resultado = default;

            for (var i = 0; i < ControleTentativasLogin.LimiteTentativas; i++)
                resultado = controle.RegistrarFalha();

            Assert.Equal(ControleTentativasLogin.LimiteTentativas, resultado.Tentativas);
            Assert.NotNull(resultado.BloqueioAteUtc);
            Assert.True(resultado.BloqueioAteUtc > DateTime.UtcNow);
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }

    [Fact]
    public void ObterBloqueioAtivo_ComInstanciaNova_ContinuaVendoOBloqueioDeUmaInstanciaAnterior()
    {
        // Simula reiniciar o app: uma segunda instância de ControleTentativasLogin
        // apontando pra mesma pasta (novo processo leria do mesmo arquivo) precisa
        // enxergar o bloqueio que a primeira instância já tinha registrado — sem
        // isto, reiniciar o app a cada poucas tentativas dribla o limite por completo.
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            var primeiraInstancia = new ControleTentativasLogin(pasta);
            for (var i = 0; i < ControleTentativasLogin.LimiteTentativas; i++)
                primeiraInstancia.RegistrarFalha();

            var segundaInstancia = new ControleTentativasLogin(pasta);
            var bloqueio = segundaInstancia.ObterBloqueioAtivo();

            Assert.NotNull(bloqueio);
            Assert.True(bloqueio > DateTime.UtcNow);
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }

    [Fact]
    public void RegistrarSucesso_LimpaOBloqueioEATentativasAcumuladas()
    {
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            var controle = new ControleTentativasLogin(pasta);
            for (var i = 0; i < ControleTentativasLogin.LimiteTentativas; i++)
                controle.RegistrarFalha();

            controle.RegistrarSucesso();

            Assert.Null(controle.ObterBloqueioAtivo());
            var (tentativas, bloqueioAte) = controle.RegistrarFalha();
            Assert.Equal(1, tentativas);
            Assert.Null(bloqueioAte);
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }

    [Fact]
    public void Limpar_SemArquivoExistente_NaoLancaExcecao()
    {
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            new ControleTentativasLogin(pasta).Limpar();
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }

    [Fact]
    public void ObterBloqueioAtivo_SemNenhumaTentativaRegistrada_RetornaNulo()
    {
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            var controle = new ControleTentativasLogin(pasta);

            Assert.Null(controle.ObterBloqueioAtivo());
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }
}
