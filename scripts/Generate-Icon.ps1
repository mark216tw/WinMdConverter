[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root "assets\WinMdConverter.ico"
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

function New-RoundedRectanglePath {
    param(
        [single]$X,
        [single]$Y,
        [single]$Width,
        [single]$Height,
        [single]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconFrame {
    param([int]$Size)

    $renderSize = $Size * 4
    $bitmap = [System.Drawing.Bitmap]::new($renderSize, $renderSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.ScaleTransform([single]($renderSize / 128), [single]($renderSize / 128))

    $backgroundPath = New-RoundedRectanglePath 4 4 120 120 23
    $backgroundBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml("#176b63"))
    $backgroundPen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml("#0e514b"), 3)
    $graphics.FillPath($backgroundBrush, $backgroundPath)
    $graphics.DrawPath($backgroundPen, $backgroundPath)

    $documentPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $documentPath.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(32, 24),
        [System.Drawing.PointF]::new(74, 24),
        [System.Drawing.PointF]::new(98, 48),
        [System.Drawing.PointF]::new(98, 108),
        [System.Drawing.PointF]::new(32, 108)
    ))
    $paperBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml("#f8f5ed"))
    $paperPen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml("#d7d2c5"), 2)
    $paperPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.FillPath($paperBrush, $documentPath)
    $graphics.DrawPath($paperPen, $documentPath)

    $foldPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $foldPath.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(74, 24),
        [System.Drawing.PointF]::new(74, 48),
        [System.Drawing.PointF]::new(98, 48)
    ))
    $foldBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml("#bfd9d3"))
    $graphics.FillPath($foldBrush, $foldPath)
    $graphics.DrawPath($paperPen, $foldPath)

    $inkPen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml("#17212b"), 6)
    $inkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $inkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $inkPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawLines($inkPen, [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(43, 78),
        [System.Drawing.PointF]::new(43, 56),
        [System.Drawing.PointF]::new(53, 68),
        [System.Drawing.PointF]::new(63, 56),
        [System.Drawing.PointF]::new(63, 78)
    ))
    $graphics.DrawLine($inkPen, 79, 56, 79, 79)
    $graphics.DrawLines($inkPen, [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(70, 71),
        [System.Drawing.PointF]::new(79, 80),
        [System.Drawing.PointF]::new(88, 71)
    ))

    $graphics.Dispose()
    $backgroundPath.Dispose()
    $backgroundBrush.Dispose()
    $backgroundPen.Dispose()
    $documentPath.Dispose()
    $paperBrush.Dispose()
    $paperPen.Dispose()
    $foldPath.Dispose()
    $foldBrush.Dispose()
    $inkPen.Dispose()

    $frame = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $frameGraphics = [System.Drawing.Graphics]::FromImage($frame)
    $frameGraphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $frameGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $frameGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $frameGraphics.DrawImage($bitmap, [System.Drawing.Rectangle]::new(0, 0, $Size, $Size))
    $frameGraphics.Dispose()
    $bitmap.Dispose()

    $stream = [System.IO.MemoryStream]::new()
    $frame.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($Size -eq 256) {
        $frame.Save((Join-Path $outputDirectory "WinMdConverter.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $frame.Dispose()
    $bytes = $stream.ToArray()
    $stream.Dispose()
    return $bytes
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$frames = @()
foreach ($size in $sizes) {
    $frames += ,(New-IconFrame $size)
}

$fileStream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create)
$writer = [System.IO.BinaryWriter]::new($fileStream)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$frames.Count)

$offset = 6 + (16 * $frames.Count)
for ($index = 0; $index -lt $frames.Count; $index++) {
    $size = $sizes[$index]
    $frame = $frames[$index]
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$frame.Length)
    $writer.Write([uint32]$offset)
    $offset += $frame.Length
}

foreach ($frame in $frames) {
    $writer.Write([byte[]]$frame)
}

$writer.Dispose()
$fileStream.Dispose()
Write-Host "Icon created: $OutputPath"
