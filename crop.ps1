Add-Type -AssemblyName System.Drawing
$filePath = 'C:\Users\PC\.gemini\antigravity-ide\brain\61be171a-b862-4b3d-887b-beb204579c99\.user_uploaded\media_1789376113809.jpg'
$img = [System.Drawing.Image]::FromFile($filePath)
Write-Output "Size: $($img.Width) x $($img.Height)"

# Crop the horizon of the game view
$cropX = [int]($img.Width * 0.42)
$cropY = [int]($img.Height * 0.72)
$cropW = [int]($img.Width * 0.16)
$cropH = [int]($img.Height * 0.10)

$cropRect = New-Object System.Drawing.Rectangle $cropX, $cropY, $cropW, $cropH
$bmp = New-Object System.Drawing.Bitmap $cropRect.Width, $cropRect.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $cropW, $cropH), $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
$bmp.Save('C:\Users\PC\.gemini\antigravity-ide\brain\61be171a-b862-4b3d-887b-beb204579c99\scratch\game_crop.png', [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
$img.Dispose()
Write-Output "Saved crop successfully"
