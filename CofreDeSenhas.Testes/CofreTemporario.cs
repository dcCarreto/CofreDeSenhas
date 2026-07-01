using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Testes;

internal sealed class CofreTemporario : IDisposable
{
    public string Pasta { get; }
    public string SenhaMestra { get; }
    public byte[] Chave { get; }

    public CofreTemporario(string senhaMestra = "SenhaMestra#2026")
    {
        Pasta = Path.Combine(Path.GetTempPath(), "cofre-teste-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Pasta);
        SenhaMestra = senhaMestra;
        Chave = new AutenticacaoMestra(Pasta).CriarSenhaMestra(senhaMestra);
    }

    public async Task<Senha> AdicionarPelaBaseAsync(
        string servico,
        string usuario,
        string senha,
        string? url = null,
        Categoria categoria = Categoria.Other,
        string? notas = null,
        bool favorito = false)
    {
        var cripto = new ServicoCriptografia(Chave);
        var repo = new RepositorioSenha(new PersistenciaLocal(cripto, Pasta), Chave);
        var item = new Senha
        {
            NomeServico = servico,
            Usuario = usuario,
            SenhaHash = cripto.Criptografar(senha),
            Url = url,
            Categoria = categoria,
            Notas = notas,
            Favorito = favorito,
        };
        await repo.AdicionarAsync(item);
        await repo.SalvarAsync();
        return item;
    }

    public async Task<List<Senha>> LerPelaBaseAsync()
    {
        var chave = new AutenticacaoMestra(Pasta).Autenticar(SenhaMestra)!;
        var repo = new RepositorioSenha(new PersistenciaLocal(new ServicoCriptografia(chave), Pasta), chave);
        return await repo.ListarTodosAsync();
    }

    public string SenhaEmClaroPelaBase(Senha senha)
    {
        var chave = new AutenticacaoMestra(Pasta).Autenticar(SenhaMestra)!;
        return new ServicoCriptografia(chave).Descriptografar(senha.SenhaHash);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Pasta))
                Directory.Delete(Pasta, true);
        }
        catch
        {
        }
    }
}
