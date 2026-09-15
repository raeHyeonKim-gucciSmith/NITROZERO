$sceneFile = 'Assets\02YUJEONG\boostOn_Yujeong_1.unity'
$lines = Get-Content $sceneFile
$comps = @('5912635396630743869', '6810238012108680524', '6810238012108680525')

foreach ($c in $comps) {
    Write-Host "=== Component ID $c ==="
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match "&$c") {
            $end = [Math]::Min($lines.Length - 1, $i + 25)
            $lines[$i..$end] | ForEach-Object { Write-Host $_ }
            break
        }
    }
}
