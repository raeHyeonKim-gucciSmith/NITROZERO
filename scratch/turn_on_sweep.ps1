$file = 'Assets\02YUJEONG\boostOn_Yujeong_1.unity'
$content = [System.IO.File]::ReadAllText($file)
$pattern = '(&6810238012108680525[\s\S]*?enableBoosterSweepCam: )0'
if ($content -match $pattern) {
    $content = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, '${1}1')
    [System.IO.File]::WriteAllText($file, $content)
    Write-Host 'Successfully set enableBoosterSweepCam to 1 in scene file!'
} else {
    Write-Warning 'Pattern not found!'
}
