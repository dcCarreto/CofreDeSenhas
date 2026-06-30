$ErrorActionPreference = "Stop"
$nomeHost = "com.dccarreto.cofredesenhas"
$instalacao = Join-Path $env:LOCALAPPDATA "CofreDeSenhas\host"

Get-Process "CofreDeSenhas.Agent" -ErrorAction SilentlyContinue | Stop-Process -Force

$chaves = @(
    "HKCU:\Software\Google\Chrome\NativeMessagingHosts\$nomeHost",
    "HKCU:\Software\Microsoft\Edge\NativeMessagingHosts\$nomeHost",
    "HKCU:\Software\BraveSoftware\Brave-Browser\NativeMessagingHosts\$nomeHost"
)
foreach ($k in $chaves) {
    if (Test-Path $k) { Remove-Item -Path $k -Recurse -Force }
}

if (Test-Path $instalacao) { Remove-Item -Path $instalacao -Recurse -Force }

Write-Host "Host nativo removido." -ForegroundColor Green
