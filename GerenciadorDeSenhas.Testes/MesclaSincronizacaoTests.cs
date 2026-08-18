using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;
using Xunit;

namespace GerenciadorDeSenhas.Testes;

public class MesclaSincronizacaoTests
{
    private static Senha NovaSenha(Guid id, string dominio, DateTime dataAtualizacao) => new()
    {
        Id = id,
        NomeServico = dominio,
        Usuario = "u",
        SenhaHash = "cifrado",
        Categoria = Categoria.Other,
        DataAtualizacao = dataAtualizacao
    };

    [Fact]
    public void MesclarSenhas_ItemSoLocal_Sobrevive()
    {
        var id = Guid.NewGuid();
        var locais = new List<Senha> { NovaSenha(id, "local.com", DateTime.UtcNow) };

        var resultado = MesclaSincronizacao.MesclarSenhas(locais, new List<Senha>());

        Assert.Single(resultado);
        Assert.Equal("local.com", resultado[0].NomeServico);
    }

    [Fact]
    public void MesclarSenhas_ItemSoRemoto_EhAdicionado()
    {
        var id = Guid.NewGuid();
        var remotos = new List<Senha> { NovaSenha(id, "remoto.com", DateTime.UtcNow) };

        var resultado = MesclaSincronizacao.MesclarSenhas(new List<Senha>(), remotos);

        Assert.Single(resultado);
        Assert.Equal("remoto.com", resultado[0].NomeServico);
    }

    [Fact]
    public void MesclarSenhas_MesmoIdComLocalMaisRecente_LocalVenceMasUneEtiquetasEHistorico()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = NovaSenha(id, "servico.com", agora);
        local.Etiquetas.Add("local-tag");
        local.Historico.Add(new HistoricoSenha { SenhaHash = "senha-local-antiga", DataAlteracao = agora.AddDays(-1) });

        var remoto = NovaSenha(id, "servico.com", agora.AddMinutes(-10));
        remoto.Etiquetas.Add("remoto-tag");
        remoto.Historico.Add(new HistoricoSenha { SenhaHash = "senha-remota-antiga", DataAlteracao = agora.AddDays(-2) });

        var resultado = MesclaSincronizacao.MesclarSenhas(new List<Senha> { local }, new List<Senha> { remoto });

        var item = Assert.Single(resultado);
        Assert.Equal("cifrado", item.SenhaHash); // conteúdo do vencedor (local)
        Assert.Contains("local-tag", item.Etiquetas);
        Assert.Contains("remoto-tag", item.Etiquetas);
        Assert.Contains(item.Historico, h => h.SenhaHash == "senha-local-antiga");
        Assert.Contains(item.Historico, h => h.SenhaHash == "senha-remota-antiga");
    }

    [Fact]
    public void MesclarSenhas_MesmoIdComRemotoMaisRecente_PreservaAnexosLocais()
    {
        // Anexos nunca sincronizam pro banco (decisão de produto) — o objeto remoto
        // sempre chega com Anexos vazio. Sem tratamento especial, deixar o remoto
        // vencer apagaria os anexos que o lado local já tinha.
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = NovaSenha(id, "servico.com", agora.AddMinutes(-10));
        local.Anexos.Add(new AnexoSenha { NomeArquivo = "arquivo.txt", TamanhoBytes = 10 });

        var remoto = NovaSenha(id, "servico.com", agora);

        var resultado = MesclaSincronizacao.MesclarSenhas(new List<Senha> { local }, new List<Senha> { remoto });

        var item = Assert.Single(resultado);
        var anexo = Assert.Single(item.Anexos);
        Assert.Equal("arquivo.txt", anexo.NomeArquivo);
    }

    [Fact]
    public void MesclarSenhas_ComTumbaDeExclusaoDefinitivaVencendo_NaoResgataEtiquetasNemHistoricoDoLadoPerdedor()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = NovaSenha(id, "sera-esquecido.com", agora.AddMinutes(-30));
        local.Etiquetas.Add("nao-pode-voltar");
        local.Historico.Add(new HistoricoSenha { SenhaHash = "senha-antiga-sensivel", DataAlteracao = agora.AddDays(-5) });

        var tumba = new Senha
        {
            Id = id,
            NomeServico = "",
            Usuario = "",
            SenhaHash = "",
            NaLixeira = true,
            DataAtualizacao = agora
        };

        var resultado = MesclaSincronizacao.MesclarSenhas(new List<Senha> { local }, new List<Senha> { tumba });

        var item = Assert.Single(resultado);
        Assert.Equal("", item.NomeServico);
        Assert.Empty(item.Etiquetas);
        Assert.Empty(item.Historico);
    }

    [Fact]
    public void EhTumbaDeExclusaoDefinitiva_ComLinhaNormalNaLixeira_RetornaFalse()
    {
        var normal = new Senha { NomeServico = "servico.com", Usuario = "u", SenhaHash = "x", NaLixeira = true };

        Assert.False(MesclaSincronizacao.EhTumbaDeExclusaoDefinitiva(normal));
    }

    [Fact]
    public void MesclarSenhasExportadas_EtiquetasDivergentes_Une()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = new SenhaExportada { Id = id, NomeServico = "s", Usuario = "u", Senha = "p", DataAtualizacao = agora };
        local.Etiquetas.Add("local-tag");

        var remoto = new SenhaExportada { Id = id, NomeServico = "s", Usuario = "u", Senha = "p", DataAtualizacao = agora.AddMinutes(-5) };
        remoto.Etiquetas.Add("remoto-tag");

        var resultado = MesclaSincronizacao.MesclarSenhasExportadas(new List<SenhaExportada> { local }, new List<SenhaExportada> { remoto });

        var item = Assert.Single(resultado);
        Assert.Contains("local-tag", item.Etiquetas);
        Assert.Contains("remoto-tag", item.Etiquetas);
    }

    [Fact]
    public void MesclarSenhasExportadas_HistoricoDivergente_Une()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = new SenhaExportada { Id = id, NomeServico = "s", Usuario = "u", Senha = "p", DataAtualizacao = agora };
        local.Historico.Add(new HistoricoSenhaExportada { Senha = "senha-local-antiga", DataAlteracao = agora.AddDays(-1) });

        var remoto = new SenhaExportada { Id = id, NomeServico = "s", Usuario = "u", Senha = "p", DataAtualizacao = agora.AddMinutes(-5) };
        remoto.Historico.Add(new HistoricoSenhaExportada { Senha = "senha-remota-antiga", DataAlteracao = agora.AddDays(-2) });

        var resultado = MesclaSincronizacao.MesclarSenhasExportadas(new List<SenhaExportada> { local }, new List<SenhaExportada> { remoto });

        var item = Assert.Single(resultado);
        Assert.Contains(item.Historico, h => h.Senha == "senha-local-antiga");
        Assert.Contains(item.Historico, h => h.Senha == "senha-remota-antiga");
    }

    [Fact]
    public void MesclarSenhasExportadas_CodigosRecuperacaoDivergentes_Une()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = new SenhaExportada { Id = id, NomeServico = "s", Usuario = "u", Senha = "p", DataAtualizacao = agora };
        local.CodigosRecuperacao.Add(new CodigoRecuperacaoExportado { Codigo = "CODIGO-LOCAL", Usado = false });

        var remoto = new SenhaExportada { Id = id, NomeServico = "s", Usuario = "u", Senha = "p", DataAtualizacao = agora.AddMinutes(-5) };
        remoto.CodigosRecuperacao.Add(new CodigoRecuperacaoExportado { Codigo = "CODIGO-REMOTO", Usado = false });

        var resultado = MesclaSincronizacao.MesclarSenhasExportadas(new List<SenhaExportada> { local }, new List<SenhaExportada> { remoto });

        var item = Assert.Single(resultado);
        Assert.Contains(item.CodigosRecuperacao, c => c.Codigo == "CODIGO-LOCAL");
        Assert.Contains(item.CodigosRecuperacao, c => c.Codigo == "CODIGO-REMOTO");
    }

    [Fact]
    public void MesclarSenhasExportadas_ComTumbaDeExclusaoDefinitivaVencendo_NaoResgataEtiquetasNemHistoricoDoLadoPerdedor()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = new SenhaExportada { Id = id, NomeServico = "sera-esquecido.com", Usuario = "u", Senha = "p", DataAtualizacao = agora.AddMinutes(-30) };
        local.Etiquetas.Add("nao-pode-voltar");
        local.Historico.Add(new HistoricoSenhaExportada { Senha = "senha-antiga-sensivel", DataAlteracao = agora.AddDays(-5) });

        var tumba = new SenhaExportada
        {
            Id = id,
            NomeServico = "",
            Usuario = "",
            Senha = "",
            NaLixeira = true,
            DataAtualizacao = agora
        };

        var resultado = MesclaSincronizacao.MesclarSenhasExportadas(new List<SenhaExportada> { local }, new List<SenhaExportada> { tumba });

        var item = Assert.Single(resultado);
        Assert.Equal("", item.NomeServico);
        Assert.Empty(item.Etiquetas);
        Assert.Empty(item.Historico);
    }

    [Fact]
    public void EhTumbaDeExclusaoDefinitiva_SenhaExportadaComLinhaNormalNaLixeira_RetornaFalse()
    {
        var normal = new SenhaExportada { NomeServico = "servico.com", Usuario = "u", Senha = "x", NaLixeira = true };

        Assert.False(MesclaSincronizacao.EhTumbaDeExclusaoDefinitiva(normal));
    }

    [Fact]
    public void MesclarSenhasExportadas_MesmoIdSemDivergenciaDeListas_MantemOVencedorSemReconstruir()
    {
        var id = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        var local = new SenhaExportada { Id = id, NomeServico = "s-local", Usuario = "u", Senha = "p-local", DataAtualizacao = agora };
        var remoto = new SenhaExportada { Id = id, NomeServico = "s-remoto", Usuario = "u", Senha = "p-remoto", DataAtualizacao = agora.AddMinutes(-5) };

        var resultado = MesclaSincronizacao.MesclarSenhasExportadas(new List<SenhaExportada> { local }, new List<SenhaExportada> { remoto });

        var item = Assert.Single(resultado);
        Assert.Same(local, item);
    }
}
