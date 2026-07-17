# Publica o Cofre de Senhas para Windows x64 e compila o instalador com o Inno Setup.
# Uso: .\App\distribuicao\gerar-instalador.ps1
param(
    [string]$Versao
)

$ErrorActionPreference = "Stop"
$raiz = Resolve-Path "$PSScriptRoot\..\.."

if (-not $Versao) {
    $csproj = Get-Content "$raiz\App\App.csproj" -Raw
    if ($csproj -match "<Version>([^<]+)</Version>") {
        $Versao = $Matches[1]
    } else {
        throw "Não foi possível determinar a versão em App\App.csproj. Use -Versao X.Y.Z."
    }
}

$candidatosIscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$iscc = $candidatosIscc | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    $comando = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($comando) { $iscc = $comando.Source }
}
if (-not $iscc) {
    throw "Inno Setup não encontrado. Instale com: winget install JRSoftware.InnoSetup"
}

Write-Host "Publicando o aplicativo (win-x64, autocontido, versão $Versao)..."
dotnet publish "$raiz\App\App.csproj" `
    -f net10.0-windows10.0.19041.0 -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -o "$raiz\publish"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou." }

Write-Host "Compilando o instalador com o Inno Setup..."
& $iscc "/DMyAppVersion=$Versao" "$raiz\App\distribuicao\cofre-de-senhas.iss"
if ($LASTEXITCODE -ne 0) { throw "ISCC falhou." }

Write-Host ""
Write-Host "Pronto! Instalador gerado em: $raiz\dist\CofreDeSenhas-Setup-$Versao.exe"
