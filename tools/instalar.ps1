param(
    [switch]$SelfContained,
    [string]$ExtensionId = "glkkihapjkoncfobclhbcmndmpenmkhb"
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$nomeHost = "com.dccarreto.cofredesenhas"
$instalacao = Join-Path $env:LOCALAPPDATA "CofreDeSenhas\host"

Get-Process "CofreDeSenhas.Agent" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Publicando Bridge e Agent (Release)..." -ForegroundColor Cyan
$pubArgs = @("-c", "Release", "--nologo", "-v", "q", "-o", $instalacao)
if ($SelfContained) {
    $pubArgs += @("-r", "win-x64", "--self-contained", "true", "-p:PublishSingleFile=true")
}

dotnet publish "$raiz\CofreDeSenhas.Bridge\CofreDeSenhas.Bridge.csproj" @pubArgs | Out-Null
dotnet publish "$raiz\CofreDeSenhas.Agent\CofreDeSenhas.Agent.csproj"   @pubArgs | Out-Null

$bridge = Join-Path $instalacao "CofreDeSenhas.Bridge.exe"
if (-not (Test-Path $bridge)) { throw "Falha: Bridge não publicado em $bridge" }

$manifesto = Join-Path $instalacao "$nomeHost.json"
$conteudo = [ordered]@{
    name            = $nomeHost
    description     = "Cofre de Senhas - host nativo"
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

Write-Host "Instalado em: $instalacao" -ForegroundColor Green
Write-Host "Extensão (ID fixo): $ExtensionId"
Write-Host "Carregue a pasta 'extensao-navegador' em chrome://extensions e reinicie o navegador." -ForegroundColor Yellow
