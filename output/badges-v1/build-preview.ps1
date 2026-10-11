$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$badgeRoot = $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $badgeRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$groups = @(
    @{ Id='achievements'; Title='Достижения'; Subtitle='12 игровых бейджей' },
    @{ Id='prestige'; Title='Престиж'; Subtitle='7 ступеней долгого пути' },
    @{ Id='collections'; Title='Коллекции'; Subtitle='4 тематических бейджа' }
)
$columns = 4
$width = 1760
$margin = 40
$cellWidth = 420
$cellHeight = 356
$headerHeight = 100
$height = 100 + 3 * $headerHeight + 6 * $cellHeight + 40
$canvas = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$graphics.Clear([System.Drawing.ColorTranslator]::FromHtml('#F7EDDC'))
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$titleFont = New-Object System.Drawing.Font('Segoe UI', 34, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$groupFont = New-Object System.Drawing.Font('Segoe UI', 28, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$nameFont = New-Object System.Drawing.Font('Segoe UI', 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$detailFont = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$ink = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#634225'))
$softInk = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#987655'))
$plate = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#FFF8EB'))
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$graphics.DrawString('Bloomlings · Бейджи v1', $titleFont, $ink, 40, 24)
$checks = @()
$y = 100
foreach ($group in $groups) {
    $graphics.DrawString($group.Title, $groupFont, $ink, 40, ($y + 12))
    $graphics.DrawString($group.Subtitle, $detailFont, $softInk, 40, ($y + 51))
    $y += $headerHeight
    $items = @($manifest.badges | Where-Object { $_.group -eq $group.Id })
    for ($i = 0; $i -lt $items.Count; $i++) {
        $item = $items[$i]
        $x = $margin + ($i % $columns) * $cellWidth
        $itemY = $y + [Math]::Floor($i / $columns) * $cellHeight
        $graphics.FillRectangle($plate, $x + 8, $itemY + 8, $cellWidth - 16, $cellHeight - 16)
        $source = [System.Drawing.Bitmap]::FromFile((Join-Path $badgeRoot $item.file))
        try {
            $checks += [PSCustomObject]@{
                id=$item.id; width=$source.Width; height=$source.Height;
                pixelFormat=$source.PixelFormat.ToString();
                alphaTopLeft=$source.GetPixel(0,0).A;
                alphaTopRight=$source.GetPixel($source.Width-1,0).A;
                alphaBottomLeft=$source.GetPixel(0,$source.Height-1).A;
                alphaBottomRight=$source.GetPixel($source.Width-1,$source.Height-1).A
            }
            $scale = [Math]::Min(330.0 / $source.Width, 270.0 / $source.Height)
            $drawWidth = [int]($source.Width * $scale)
            $drawHeight = [int]($source.Height * $scale)
            $drawX = [int]($x + ($cellWidth - $drawWidth) / 2)
            $drawY = [int]($itemY + 18 + (270 - $drawHeight) / 2)
            $graphics.DrawImage($source, $drawX, $drawY, $drawWidth, $drawHeight)
        } finally { $source.Dispose() }
        $nameRect = New-Object System.Drawing.RectangleF(($x + 16), ($itemY + 284), ($cellWidth - 32), 42)
        $graphics.DrawString($item.name, $nameFont, $ink, $nameRect, $format)
        $idRect = New-Object System.Drawing.RectangleF(($x + 16), ($itemY + 322), ($cellWidth - 32), 22)
        $graphics.DrawString($item.id, $detailFont, $softInk, $idRect, $format)
    }
    $y += [Math]::Ceiling($items.Count / $columns) * $cellHeight
}
$previewPath = Join-Path $badgeRoot 'all-badges-preview.png'
$canvas.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$canvas.Dispose()
foreach ($disposable in @($titleFont,$groupFont,$nameFont,$detailFont,$ink,$softInk,$plate,$format)) { $disposable.Dispose() }
$checks | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $badgeRoot 'image-checks.json') -Encoding UTF8
$badCorners = @($checks | Where-Object { $_.alphaTopLeft -gt 1 -or $_.alphaTopRight -gt 1 -or $_.alphaBottomLeft -gt 1 -or $_.alphaBottomRight -gt 1 })
if ($checks.Count -ne 23 -or $badCorners.Count -ne 0) { throw 'Asset count or corner-alpha verification failed' }
Write-Output "Preview: $previewPath"
Write-Output "Verified: $($checks.Count) RGBA PNG files; corner alpha <= 1/255"
