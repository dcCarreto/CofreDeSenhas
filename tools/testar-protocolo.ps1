param([string]$Dominio = "github.com")

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot

Write-Host "Compilando o Agent..." -ForegroundColor Cyan
dotnet build "$raiz\CofreDeSenhas.Agent\CofreDeSenhas.Agent.csproj" -c Debug --nologo -v q | Out-Null

$agentExe = "$raiz\CofreDeSenhas.Agent\bin\Debug\net10.0-windows\CofreDeSenhas.Agent.exe"
Write-Host "Iniciando o Agent..." -ForegroundColor Cyan
Start-Process -FilePath $agentExe
Start-Sleep -Milliseconds 800

$pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "CofreDeSenhas.Agent", [System.IO.Pipes.PipeDirection]::InOut)
$pipe.Connect(3000)
$leitor = New-Object System.IO.StreamReader($pipe)
$escritor = New-Object System.IO.StreamWriter($pipe)
$escritor.AutoFlush = $true

function Enviar($json) {
    $escritor.WriteLine($json)
    $resposta = $leitor.ReadLine()
    Write-Host ">> $json" -ForegroundColor DarkGray
    Write-Host "<< $resposta" -ForegroundColor Green
    return $resposta
}

Enviar '{"tipo":"status"}' | Out-Null

Write-Host "Solicitando unlock — digite a senha mestra no diálogo que abrir..." -ForegroundColor Yellow
Enviar '{"tipo":"unlock"}' | Out-Null

$respConsulta = Enviar "{`"tipo`":`"query`",`"dominio`":`"$Dominio`"}"
$consulta = $respConsulta | ConvertFrom-Json

if ($consulta.itens -and $consulta.itens.Count -gt 0) {
    $id = $consulta.itens[0].id
    Write-Host "Recuperando credencial $id ..." -ForegroundColor Yellow
    Enviar "{`"tipo`":`"getCredential`",`"id`":`"$id`"}" | Out-Null
}
else {
    Write-Host "Nenhuma credencial salva casa com '$Dominio'." -ForegroundColor Yellow
}

Enviar '{"tipo":"lock"}' | Out-Null
$pipe.Dispose()
Write-Host "Concluído." -ForegroundColor Cyan
