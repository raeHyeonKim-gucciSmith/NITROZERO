Add-Type -AssemblyName System.Drawing
$filePath = 'C:\Users\PC\.gemini\antigravity-ide\brain\61be171a-b862-4b3d-887b-beb204579c99\.user_uploaded\media_1789376113809.jpg'
$img = [System.Drawing.Image]::FromFile($filePath)

# In Scene view (top), let's crop right where the road converges at the horizon
# In the original image (1024x829):
# The road tip is around X=512, Y=110 ~ 130
$cropX = 470
$cropY = 90
$cropW = 85
$cropH = 65

$cropRect = New-Object System.Drawing.Rectangle $cropX, $cropY, $cropW, $cropH
$bmp = New-Object System.Drawing.Bitmap $cropRect.Width, $cropRect.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $cropW, $cropH), $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
$bmp.Save('C:\Users\PC\.gemini\antigravity-ide\brain\61be171a-b862-4b3d-887b-beb204579c99\scratch\scene_road_tip.png', [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
$img.Dispose()
Write-Output "Saved road tip"
