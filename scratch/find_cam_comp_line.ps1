$sceneFile = 'Assets\02YUJEONG\boostOn_Yujeong_1.unity'
$lines = Get-Content $sceneFile
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '&6810238012108680525') {
        Write-Host "Found 6810238012108680525 at line $i"
        $lines[$i..($i+30)] | ForEach-Object { Write-Host $_ }
        break
    }
}
