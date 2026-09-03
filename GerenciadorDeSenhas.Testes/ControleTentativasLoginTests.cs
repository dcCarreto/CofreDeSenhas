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
    public void RegistrarFalha_RodadasMaisAvancadas_EscalamADuracaoDoBloqueio()
    {
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            var controle = new ControleTentativasLogin(pasta);

            var d2 = DuracaoDoProximoBloqueio(controle, pasta, rodadasJaCumpridas: 1);
            var d5 = DuracaoDoProximoBloqueio(controle, pasta, rodadasJaCumpridas: 4);

            Assert.True(d2 >= ControleTentativasLogin.Escalada[1] - Folga, $"rodada 2 = {d2}");
            Assert.True(d5 > d2, $"rodada 5 ({d5}) deveria ser mais longa que a rodada 2 ({d2})");
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }

    [Fact]
    public void RegistrarFalha_MuitasRodadas_NaoPassaDoTetoDaEscalada()
    {
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            var d = DuracaoDoProximoBloqueio(new ControleTentativasLogin(pasta), pasta, rodadasJaCumpridas: 999);

            var teto = ControleTentativasLogin.Escalada[^1];
            Assert.True(d <= teto + Folga && d >= teto - Folga, $"{d}");
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }

    [Fact]
    public void RegistrarSucesso_ZeraAEscalada_ProximoBloqueioVoltaAoInicio()
    {
        var pasta = PastaTemporariaTeste.Criar("ControleTentativas");
        try
        {
            var controle = new ControleTentativasLogin(pasta);
            EscreverEstado(pasta, tentativas: 0, rodadas: 5, bloqueadoAteUtc: DateTime.UtcNow.AddMinutes(-1));

            controle.RegistrarSucesso();

            var d = DuracaoDoProximoBloqueio(controle, pasta, rodadasJaCumpridas: 0);
            Assert.True(d <= ControleTentativasLogin.Escalada[0] + Folga, $"{d}");
        }
        finally
        {
            PastaTemporariaTeste.Apagar(pasta);
        }
    }

    private static readonly TimeSpan Folga = TimeSpan.FromSeconds(3);

    private static TimeSpan DuracaoDoProximoBloqueio(ControleTentativasLogin controle, string pasta, int rodadasJaCumpridas)
    {
        EscreverEstado(pasta, tentativas: 0, rodadas: rodadasJaCumpridas, bloqueadoAteUtc: DateTime.UtcNow.AddMinutes(-1));

        DateTime? bloqueio = null;
        for (var i = 0; i < ControleTentativasLogin.LimiteTentativas; i++)
            bloqueio = controle.RegistrarFalha().BloqueioAteUtc;

        return bloqueio!.Value - DateTime.UtcNow;
    }

    private static void EscreverEstado(string pasta, int tentativas, int rodadas, DateTime? bloqueadoAteUtc)
    {
        var bloqueio = bloqueadoAteUtc is { } d ? $"\"{d:o}\"" : "null";
        File.WriteAllText(Path.Combine(pasta, "tentativas.dat"),
            $"{{\"Tentativas\":{tentativas},\"Rodadas\":{rodadas},\"BloqueadoAteUtc\":{bloqueio}}}");
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
