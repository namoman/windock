Add-Type -AssemblyName System.Drawing
$pngPath = Join-Path $PSScriptRoot "app-icon.png"
$icoPath = Join-Path $PSScriptRoot "app-icon.ico"
$bitmap = [System.Drawing.Bitmap]::FromFile($pngPath)
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream = [System.IO.File]::Create($icoPath)
$icon.Save($stream)
$stream.Close()
$icon.Dispose()
$bitmap.Dispose()
Write-Host "Created $icoPath"
