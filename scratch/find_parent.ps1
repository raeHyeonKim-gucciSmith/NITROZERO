$sceneFile = 'Assets\02YUJEONG\boostOn_Yujeong_1.unity'
$lines = Get-Content $sceneFile
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '&8413464805995866985') {
        Write-Host "Found parent 8413464805995866985 at line $i"
        $lines[$i..($i+25)] | ForEach-Object { Write-Host $_ }
        break
    }
}
