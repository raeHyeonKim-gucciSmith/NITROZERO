Add-Type -AssemblyName System.Drawing
$filePath = 'C:\Users\PC\.gemini\antigravity-ide\brain\61be171a-b862-4b3d-887b-beb204579c99\.user_uploaded\media_1789376113809.jpg'
$img = [System.Drawing.Image]::FromFile($filePath)

# Crop the full game view (bottom half of image, roughly Y: 430 to 829, X: 100 to 950)
$cropX = [int]($img.Width * 0.1)
$cropY = [int]($img.Height * 0.52)
$cropW = [int]($img.Width * 0.8)
$cropH = [int]($img.Height * 0.46)

$cropRect = New-Object System.Drawing.Rectangle $cropX, $cropY, $cropW, $cropH
$bmp = New-Object System.Drawing.Bitmap $cropRect.Width, $cropRect.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $cropW, $cropH), $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
$bmp.Save('C:\Users\PC\.gemini\antigravity-ide\brain\61be171a-b862-4b3d-887b-beb204579c99\scratch\full_game_view.png', [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
$img.Dispose()
Write-Output "Saved full game view"
