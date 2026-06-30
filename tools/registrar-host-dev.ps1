param([string]$ExtensionId = "glkkihapjkoncfobclhbcmndmpenmkhb")

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot

Write-Host "Compilando Bridge e Agent..." -ForegroundColor Cyan
dotnet build "$raiz\CofreDeSenhas.Bridge\CofreDeSenhas.Bridge.csproj" -c Debug --nologo -v q | Out-Null
dotnet build "$raiz\CofreDeSenhas.Agent\CofreDeSenhas.Agent.csproj" -c Debug --nologo -v q | Out-Null

$bridge = "$raiz\CofreDeSenhas.Bridge\bin\Debug\net10.0\CofreDeSenhas.Bridge.exe"
if (-not (Test-Path $bridge)) { throw "Bridge não encontrado em $bridge" }

$nomeHost = "com.dccarreto.cofredesenhas"
$pastaManifesto = Join-Path $env:LOCALAPPDATA "CofreDeSenhas"
New-Item -ItemType Directory -Force -Path $pastaManifesto | Out-Null
$manifesto = Join-Path $pastaManifesto "$nomeHost.json"

$conteudo = [ordered]@{
    name            = $nomeHost
    description     = "Cofre de Senhas - host nativo (dev)"
    path            = $bridge
    type            = "stdio"
    allowed_origins = @("chrome-extension://$ExtensionId/")
}
$json = $conteudo | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($manifesto, $json, (New-Object System.Text.UTF8Encoding($false)))

$chaves = @(
    "HKCU:\Software\Google\Chrome\NativeMessagingHosts\$nomeHost",
    "HKCU:\Software\Microsoft\Edge\NativeMessagingHosts\$nomeHost",
    "HKCU:\Software\BraveSoftware\Brave-Browser\NativeMessagingHosts\$nomeHost"
)
foreach ($k in $chaves) {
    New-Item -Path $k -Force | Out-Null
    Set-ItemProperty -Path $k -Name "(default)" -Value $manifesto
}

Write-Host "Host nativo registrado para a extensão $ExtensionId." -ForegroundColor Green
Write-Host "Manifesto: $manifesto"
Write-Host "Bridge:    $bridge"
Write-Host "Reinicie o navegador para garantir que ele releia o registro." -ForegroundColor Yellow
