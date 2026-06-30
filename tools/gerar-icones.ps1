Add-Type -AssemblyName System.Drawing

$destino = Join-Path (Split-Path -Parent $PSScriptRoot) "extensao-navegador\icones"
New-Item -ItemType Directory -Force -Path $destino | Out-Null

function New-Icone([int]$S, [string]$arquivo) {
    $bmp = New-Object System.Drawing.Bitmap($S, $S)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $fundo = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 18, 110, 130))
    $g.FillEllipse($fundo, 0, 0, ($S - 1), ($S - 1))

    $branco = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $caneta = New-Object System.Drawing.Pen ([System.Drawing.Color]::White, [single]($S * 0.10))

    $aw = $S * 0.30; $ah = $S * 0.34
    $ax = ($S - $aw) / 2; $ay = $S * 0.24
    $g.DrawArc($caneta, [single]$ax, [single]$ay, [single]$aw, [single]$ah, 180, 180)

    $bw = $S * 0.46; $bh = $S * 0.32
    $bx = ($S - $bw) / 2; $by = $S * 0.46
    $g.FillRectangle($branco, [single]$bx, [single]$by, [single]$bw, [single]$bh)

    $bmp.Save($arquivo, [System.Drawing.Imaging.ImageFormat]::Png)
    $caneta.Dispose(); $branco.Dispose(); $fundo.Dispose()
    $g.Dispose(); $bmp.Dispose()
}

foreach ($t in 16, 32, 48, 128) {
    New-Icone $t (Join-Path $destino "icone$t.png")
}

Write-Host "Ícones gerados em $destino" -ForegroundColor Green
