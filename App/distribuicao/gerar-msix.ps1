# Gera o pacote MSIX do Cofre de Senhas para envio à Microsoft Store.
#
# O pacote sai SEM assinatura de propósito: a Loja assina com o certificado dela
# na certificação. Para instalar localmente antes disso, veja docs/LOJA-MICROSOFT.md.
#
# A identidade do pacote tem que bater com a página "Identidade do produto" do
# Partner Center, senão a Loja recusa o envio. Vem dos parâmetros ou das variáveis
# de ambiente LOJA_IDENTIDADE_NOME, LOJA_EDITOR e LOJA_NOME_EDITOR; sem nenhum dos
# dois, usa valores locais — o pacote é gerado e validado, mas não serve para a Loja.
#
# Uso: .\App\distribuicao\gerar-msix.ps1 [-Versao 2026.11.12]
param(
    [string]$Versao,
    [string]$NomeIdentidade = $env:LOJA_IDENTIDADE_NOME,
    [string]$Editor = $env:LOJA_EDITOR,
    [string]$NomeEditor = $env:LOJA_NOME_EDITOR
)

$ErrorActionPreference = "Stop"
$raiz = (Resolve-Path "$PSScriptRoot\..\..").Path
$pastaMsix = Join-Path $PSScriptRoot "msix"

if (-not $Versao) {
    $Versao = [DateTime]::UtcNow.ToString('yyyy.M.d')
}

$identidadeLocal = -not ($NomeIdentidade -and $Editor -and $NomeEditor)
if ($identidadeLocal) {
    Write-Warning "Identidade da Loja não informada - gerando pacote com identidade local (não serve para envio)."
    $NomeIdentidade = "CofreDeSenhas.Local"
    $Editor = "CN=CofreDeSenhas-Local"
    $NomeEditor = "Cofre de Senhas (build local)"
}

# A versão de um MSIX tem quatro partes de 0 a 65535, e a Loja exige a última
# igual a zero. O carimbo do produto é aaaa.M.d[.N], então mês e dia viram uma
# parte só (M*100+d) e o sufixo do dia vai para a terceira:
#   2026.11.12   -> 2026.1112.0.0
#   2026.11.12.1 -> 2026.1112.1.0
# Continua crescente entre atualizações, que é o que a Loja confere.
function Converter-VersaoMsix([string]$carimbo) {
    $partes = $carimbo.Split('.')
    if ($partes.Count -lt 3 -or $partes.Count -gt 4 -or ($partes | Where-Object { $_ -notmatch '^\d+$' })) {
        throw "Carimbo de versão inválido: '$carimbo' (esperado aaaa.M.d ou aaaa.M.d.N)."
    }
    $mes = [int]$partes[1]; $dia = [int]$partes[2]
    if ($mes -lt 1 -or $mes -gt 12 -or $dia -lt 1 -or $dia -gt 31) {
        throw "Carimbo de versão inválido: '$carimbo' (mês ou dia fora do intervalo)."
    }
    $sufixo = if ($partes.Count -eq 4) { [int]$partes[3] } else { 0 }
    return "{0}.{1}.{2}.0" -f [int]$partes[0], ($mes * 100 + $dia), $sufixo
}
$versaoMsix = Converter-VersaoMsix $Versao

# makeappx e makepri vêm do Windows SDK. Sem ele instalado (máquina de
# desenvolvimento comum), baixa o pacote NuGet oficial que traz só essas
# ferramentas, com versão e hash fixos.
$versaoBuildTools = "10.0.28000.2957"
$hashBuildTools = "3e17cbe9fd5df5cd825c20f67f2424c0104547afdcb205715835838fff6e218dcf539694477beda00267ec73f577f70cac719086e487d455c7bda6488ce21a8f"

function Encontrar-PastaFerramentas {
    $sdk = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
        Where-Object { Test-Path (Join-Path $_.DirectoryName "makepri.exe") } |
        Sort-Object { [version]$_.Directory.Parent.Name } -Descending |
        Select-Object -First 1
    if ($sdk) { return $sdk.DirectoryName }

    $destino = Join-Path $raiz "publish-msix\ferramentas\$versaoBuildTools"
    $pastaBin = Join-Path $destino "bin\10.0.28000.0\x64"
    if (Test-Path (Join-Path $pastaBin "makeappx.exe")) { return $pastaBin }

    Write-Host "Windows SDK não encontrado - baixando Microsoft.Windows.SDK.BuildTools $versaoBuildTools..."
    New-Item -ItemType Directory -Force $destino | Out-Null
    $zip = Join-Path $destino "buildtools.zip"
    $url = "https://api.nuget.org/v3-flatcontainer/microsoft.windows.sdk.buildtools/$versaoBuildTools/microsoft.windows.sdk.buildtools.$versaoBuildTools.nupkg"
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    $hash = (Get-FileHash $zip -Algorithm SHA512).Hash.ToLowerInvariant()
    if ($hash -ne $hashBuildTools) {
        Remove-Item $zip -Force
        throw "Hash do Microsoft.Windows.SDK.BuildTools não confere (esperado $hashBuildTools, veio $hash)."
    }
    Expand-Archive $zip -DestinationPath $destino -Force
    Remove-Item $zip -Force
    if (-not (Test-Path (Join-Path $pastaBin "makeappx.exe"))) {
        throw "makeappx.exe não encontrado em $pastaBin depois de extrair o pacote."
    }
    return $pastaBin
}

$ferramentas = Encontrar-PastaFerramentas
$makeappx = Join-Path $ferramentas "makeappx.exe"
$makepri = Join-Path $ferramentas "makepri.exe"

$publicacao = Join-Path $raiz "publish-msix\pacote"
if (Test-Path $publicacao) { Remove-Item $publicacao -Recurse -Force }

# Pasta normal em vez de arquivo único: dentro do pacote o .exe de arquivo único
# só perderia tempo extraindo as DLLs nativas na primeira execução — a pasta de
# instalação já é dele e é somente leitura.
Write-Host "Publicando o aplicativo (win-x64, autocontido, atualização $Versao)..."
dotnet publish "$raiz\App\App.csproj" `
    -f net10.0-windows10.0.19041.0 -c Release -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -p:Version=$Versao `
    -o $publicacao
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou." }

# Sem PDBs no pacote: não ajudam o usuário e só aumentam o download.
Get-ChildItem $publicacao -Filter *.pdb -Recurse | Remove-Item -Force

Copy-Item (Join-Path $pastaMsix "Imagens") (Join-Path $publicacao "Imagens") -Recurse

function Escapar-Xml([string]$texto) { [System.Security.SecurityElement]::Escape($texto) }
$manifesto = Get-Content (Join-Path $pastaMsix "AppxManifest.xml") -Raw -Encoding UTF8
$manifesto = $manifesto.
    Replace("__NOME_IDENTIDADE__", (Escapar-Xml $NomeIdentidade)).
    Replace("__EDITOR__", (Escapar-Xml $Editor)).
    Replace("__NOME_EDITOR__", (Escapar-Xml $NomeEditor)).
    Replace("__VERSAO__", $versaoMsix)
$caminhoManifesto = Join-Path $publicacao "AppxManifest.xml"
[System.IO.File]::WriteAllText($caminhoManifesto, $manifesto, (New-Object System.Text.UTF8Encoding $false))

Write-Host "Indexando os recursos (resources.pri)..."
$logMakepri = & $makepri new /pr $publicacao /cf (Join-Path $pastaMsix "priconfig.xml") /mn $caminhoManifesto /of (Join-Path $publicacao "resources.pri") /o 2>&1
if ($LASTEXITCODE -ne 0) {
    $logMakepri | Write-Host
    throw "makepri falhou."
}

$saida = Join-Path $raiz "dist-loja"
New-Item -ItemType Directory -Force $saida | Out-Null
$pacote = Join-Path $saida "CofreDeSenhas-$Versao-win-x64.msix"

# O makeappx valida o manifesto contra o esquema antes de empacotar.
Write-Host "Empacotando o MSIX (versão do pacote $versaoMsix)..."
# Ele lista cada arquivo do pacote (centenas); a saída só aparece se der erro.
$logMakeappx = & $makeappx pack /d $publicacao /p $pacote /o 2>&1
if ($LASTEXITCODE -ne 0) {
    $logMakeappx | Write-Host
    throw "makeappx falhou."
}

Write-Host ""
Write-Host "Pronto! Pacote gerado em: $pacote"
if ($identidadeLocal) {
    Write-Host "Atenção: identidade local - para a Loja, informe a identidade do Partner Center."
}
