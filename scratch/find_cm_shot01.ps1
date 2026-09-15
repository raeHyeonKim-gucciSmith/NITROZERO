$sceneFile = 'Assets\02YUJEONG\boostOn_Yujeong_1.unity'
$lines = Get-Content $sceneFile
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match 'm_Name: CM_Shot01') {
        Write-Host "Found CM_Shot01 at line $i"
        $start = [Math]::Max(0, $i - 5)
        $end = [Math]::Min($lines.Length - 1, $i + 40)
        $lines[$start..$end] | ForEach-Object { Write-Host $_ }
        break
    }
}
