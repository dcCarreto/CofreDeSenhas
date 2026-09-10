# Publica o Cofre de Senhas para Windows x64 e compila o instalador com o Inno Setup.
# Uso: .\App\distribuicao\gerar-instalador.ps1
param(
    [string]$Versao
)

$ErrorActionPreference = "Stop"
$raiz = Resolve-Path "$PSScriptRoot\..\.."

if (-not $Versao) {
    $Versao = [DateTime]::UtcNow.ToString('yyyy.M.d')
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

Write-Host "Publicando o aplicativo (win-x64, autocontido, atualização $Versao)..."
dotnet publish "$raiz\App\App.csproj" `
    -f net10.0-windows10.0.19041.0 -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:Version=$Versao `
    -o "$raiz\publish"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou." }

# Assina antes do Inno Setup para o executável ficar assinado também dentro do
# instalador. Sem os secrets de assinatura, não faz nada.
& "$PSScriptRoot\assinar-windows.ps1" "$raiz\publish\CofreDeSenhas.exe"

Write-Host "Compilando o instalador com o Inno Setup..."
& $iscc "/DMyAppVersion=$Versao" "$raiz\App\distribuicao\cofre-de-senhas.iss"
if ($LASTEXITCODE -ne 0) { throw "ISCC falhou." }

& "$PSScriptRoot\assinar-windows.ps1" "$raiz\dist\CofreDeSenhas-Setup-$Versao.exe"

Write-Host ""
Write-Host "Pronto! Instalador gerado em: $raiz\dist\CofreDeSenhas-Setup-$Versao.exe"
