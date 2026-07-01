$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("cofre-compat-" + [Guid]::NewGuid().ToString("N"))

New-Item -ItemType Directory -Path $temp | Out-Null

try {
    $proj = Join-Path $temp "Compatibilidade.csproj"
    $programa = Join-Path $temp "Program.cs"
    $gerenciador = Join-Path $raiz "GerenciadorDeSenhas\GerenciadorDeSenhas.csproj"
    $nucleo = Join-Path $raiz "CofreDeSenhas.Nucleo\CofreDeSenhas.Nucleo.csproj"

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$gerenciador" />
    <ProjectReference Include="$nucleo" />
  </ItemGroup>
</Project>
"@ | Set-Content -Path $proj -Encoding UTF8

    @'
using System.Security.Cryptography;
using CofreDeSenhas.Nucleo;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

static void Exigir(bool condicao, string mensagem)
{
    if (!condicao)
        throw new InvalidOperationException(mensagem);
}

var pasta = Path.Combine(Path.GetTempPath(), "cofre-compat-run-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(pasta);

byte[]? chave = null;
byte[]? chaveLeitura = null;

try
{
    var senhaMestra = "Compatibilidade#2026";
    var auth = new AutenticacaoMestra(pasta);
    chave = auth.CriarSenhaMestra(senhaMestra);
    var cripto = new ServicoCriptografia(chave);
    var persistencia = new PersistenciaLocal(cripto, pasta);
    var repo = new RepositorioSenha(persistencia, chave);

    var inicial = new Senha
    {
        NomeServico = "Compat Origem",
        Usuario = "usuario@compat",
        SenhaHash = cripto.Criptografar("senha-inicial"),
        Url = "https://compat.example.com",
        Categoria = Categoria.Work,
        Notas = "origem",
        Favorito = true
    };

    await repo.AdicionarAsync(inicial);
    await repo.SalvarAsync();

    using var sessao = new SessaoCofre(pasta, TimeSpan.FromMinutes(5));
    Exigir(await sessao.DestrancarAsync(senhaMestra), "Falha ao destrancar cofre criado pela biblioteca base.");

    var consulta = sessao.Consultar("login.compat.example.com");
    Exigir(consulta.Any(i => i.Id == inicial.Id), "A extensão não encontrou a credencial criada pela biblioteca base.");

    var credencial = sessao.ObterCredencial(inicial.Id);
    Exigir(credencial?.Senha == "senha-inicial", "A extensão não descriptografou a senha criada pela biblioteca base.");

    var adicao = await sessao.AdicionarCredencialAsync(new NovaCredencial(
        "Compat Ext",
        "ext@compat",
        "senha-ext",
        "https://ext.compat.example.com"));
    Exigir(adicao.Ok && adicao.Item.HasValue, adicao.Erro ?? "Falha ao adicionar pela extensão.");
    var idExtensao = adicao.Item.GetValueOrDefault().Id;

    var edicao = await sessao.EditarCredencialAsync(new EdicaoCredencial(
        idExtensao,
        "Compat Ext Edit",
        "ext2@compat",
        "senha-edit",
        "https://edit.compat.example.com",
        "Finance",
        "editado",
        true));
    Exigir(edicao.Ok, edicao.Erro ?? "Falha ao editar pela extensão.");

    chaveLeitura = auth.Autenticar(senhaMestra);
    Exigir(chaveLeitura != null, "Falha ao autenticar novamente pela biblioteca base.");

    var criptoLeitura = new ServicoCriptografia(chaveLeitura!);
    var repoLeitura = new RepositorioSenha(new PersistenciaLocal(criptoLeitura, pasta), chaveLeitura!);
    var todos = await repoLeitura.ListarTodosAsync();
    var itemExtensao = todos.FirstOrDefault(s => s.Id == idExtensao);
    Exigir(itemExtensao != null, "A biblioteca base não encontrou o item criado pela extensão.");
    Exigir(itemExtensao!.NomeServico == "Compat Ext Edit", "A biblioteca base não leu o serviço editado pela extensão.");
    Exigir(itemExtensao.Usuario == "ext2@compat", "A biblioteca base não leu o usuário editado pela extensão.");
    Exigir(itemExtensao.Url == "https://edit.compat.example.com", "A biblioteca base não leu a URL editada pela extensão.");
    Exigir(itemExtensao.Categoria == Categoria.Finance, "A biblioteca base não leu a categoria editada pela extensão.");
    Exigir(itemExtensao.Notas == "editado", "A biblioteca base não leu as notas editadas pela extensão.");
    Exigir(itemExtensao.Favorito, "A biblioteca base não leu o favorito editado pela extensão.");
    Exigir(criptoLeitura.Descriptografar(itemExtensao.SenhaHash) == "senha-edit", "A biblioteca base não descriptografou a senha editada pela extensão.");

    Console.WriteLine($"{EspecificacaoCriptografica.Kdf};{EspecificacaoCriptografica.Criptografia};{EspecificacaoCriptografica.FormatoCofre};OK");
}
finally
{
    if (chave != null)
        CryptographicOperations.ZeroMemory(chave);
    if (chaveLeitura != null)
        CryptographicOperations.ZeroMemory(chaveLeitura);
    if (Directory.Exists(pasta))
        Directory.Delete(pasta, true);
}
'@ | Set-Content -Path $programa -Encoding UTF8

    dotnet run --project $proj -c Release --nologo
}
finally {
    if (Test-Path $temp) {
        Remove-Item -LiteralPath $temp -Recurse -Force
    }
}
