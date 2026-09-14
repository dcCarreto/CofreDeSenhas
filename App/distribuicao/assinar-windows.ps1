# Assina executáveis do Windows com Authenticode, se os secrets de assinatura
# estiverem configurados. Sem eles, não faz nada — mesma ideia da assinatura GPG
# opcional do pipeline: os binários saem sem Authenticode (o SmartScreen mostra
# "editor desconhecido"), enquanto o CHECKSUMS assinado e a attestation seguem
# como a forma de conferir a procedência.
#
# Espera dois secrets/variáveis de ambiente:
#   WINDOWS_CERT_BASE64   - o .pfx do certificado de assinatura, em base64
#   WINDOWS_CERT_PASSWORD - a senha do .pfx (vazia se não houver)
#
# Uso: .\assinar-windows.ps1 caminho1.exe [caminho2.exe ...]
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arquivos)

$ErrorActionPreference = "Stop"

if (-not $env:WINDOWS_CERT_BASE64) {
    Write-Host "WINDOWS_CERT_BASE64 não definido - assinatura Authenticode ignorada."
    return
}
if (-not $Arquivos) { return }

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $signtool) {
    $signtool = (Get-Command signtool.exe -ErrorAction SilentlyContinue).Source
}
if (-not $signtool) {
    throw "signtool.exe não encontrado. Instale o Windows SDK (componente 'Windows SDK Signing Tools')."
}

$pfx = Join-Path ([IO.Path]::GetTempPath()) ("cofre-signing-" + [Guid]::NewGuid().ToString("N") + ".pfx")
[IO.File]::WriteAllBytes($pfx, [Convert]::FromBase64String($env:WINDOWS_CERT_BASE64))
$senha = $env:WINDOWS_CERT_PASSWORD
try {
    foreach ($arquivo in $Arquivos) {
        if (-not (Test-Path $arquivo)) {
            throw "Arquivo para assinar não encontrado: $arquivo"
        }
        $argumentos = @('sign', '/f', $pfx)
        if ($senha) { $argumentos += @('/p', $senha) }
        $argumentos += @('/fd', 'SHA256', '/tr', 'http://timestamp.digicert.com', '/td', 'SHA256',
                         '/d', 'Cofre de Senhas', $arquivo)
        & $signtool @argumentos
        if ($LASTEXITCODE -ne 0) {
            throw "signtool falhou ao assinar $arquivo"
        }
        Write-Host "Assinado: $arquivo"
    }
}
finally {
    Remove-Item $pfx -Force -ErrorAction SilentlyContinue
}
