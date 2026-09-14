Add-Type -AssemblyName System.Drawing
$filePath = 'C:\Users\PC\.gemini\antigravity-ide\brain\61be171a-b862-4b3d-887b-beb204579c99\.user_uploaded\media_1789376113809.jpg'
$img = [System.Drawing.Image]::FromFile($filePath)

$cropX = 450
$cropY = 20
$cropW = 120
$cropH = 100

$cropRect = New-Object System.Drawing.Rectangle $cropX, $cropY, $cropW, $cropH
$bmp = New-Object System.Drawing.Bitmap $cropRect.Width, $cropRect.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $cropW, $cropH), $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
$bmp.Save('C:\Users\PC\.gemini\antigravity-ide\brain\61be171a-b862-4b3d-887b-beb204579c99\scratch\scene_horizon.png', [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
$img.Dispose()
Write-Output "Saved scene horizon"
