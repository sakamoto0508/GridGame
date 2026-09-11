# Original geometric UI PNGs. No external images or fonts are used.
# Run explicitly to regenerate; existing PNG files will be overwritten.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$spriteDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../UI/Art/Neon'))
[System.IO.Directory]::CreateDirectory($spriteDirectory) | Out-Null
$definitions = @(
    @('PanelCyan', '#70DFFF', '#0B1118', 240),
    @('PanelGreen', '#6EF59A', '#0B1118', 240),
    @('ButtonNeutral', '#90AAB8', '#0B1118', 240),
    @('ButtonSelected', '#6EF59A', '#10241E', 245),
    @('ButtonPrimary', '#6EF59A', '#6EF59A', 255),
    @('FrameCyan', '#70DFFF', '#0B1118', 0),
    @('KeycapCyan', '#70DFFF', '#0B1118', 230),
    @('PanelWarning', '#FFE45C', '#0B1118', 240)
)
foreach ($definition in $definitions) {
    # Render at 4x resolution, then downsample for antialiased corners.
    $bitmap = [System.Drawing.Bitmap]::new(1024, 512)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform(4, 4)
    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(20,2), [System.Drawing.PointF]::new(236,2),
        [System.Drawing.PointF]::new(254,20), [System.Drawing.PointF]::new(254,108),
        [System.Drawing.PointF]::new(236,126), [System.Drawing.PointF]::new(20,126),
        [System.Drawing.PointF]::new(2,108), [System.Drawing.PointF]::new(2,20)
    )
    $baseColor = [System.Drawing.ColorTranslator]::FromHtml($definition[2])
    $fill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb([int]$definition[3], $baseColor))
    $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml($definition[1]), 2)
    $graphics.FillPolygon($fill, $points)
    $graphics.DrawPolygon($pen, $points)
    $output = [System.Drawing.Bitmap]::new(256,128)
    $outputGraphics = [System.Drawing.Graphics]::FromImage($output)
    $outputGraphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $outputGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $outputGraphics.DrawImage($bitmap, 0, 0, 256, 128)
    $output.Save((Join-Path $spriteDirectory ($definition[0] + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
    $outputGraphics.Dispose(); $output.Dispose(); $pen.Dispose(); $fill.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
Write-Output ('Created 8 PNG sprites: ' + $spriteDirectory)
