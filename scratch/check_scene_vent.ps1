$sceneFile = 'Assets\02YUJEONG\boostOn_Yujeong_1.unity'
$lines = Get-Content $sceneFile
Write-Host "Total lines: $($lines.Length)"

for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -match '\\uBD80\\uD488_\\uBCF4\\uC870\\uBCA4\\uD2B8\\uD3102') {
    $goIdx = $i
    while ($goIdx -ge 0 -and $lines[$goIdx] -notmatch '--- !u!1 &(\d+)') { $goIdx-- }
    $goId = if ($lines[$goIdx] -match '--- !u!1 &(\d+)') { $Matches[1] } else { 'unknown' }
    Write-Host "Found in Scene! GameObject ID: $goId (line $goIdx)"
    for ($j = 0; $j -lt $lines.Length; $j++) {
      if ($lines[$j] -match '--- !u!4 &(\d+)') {
        $tId = $Matches[1]
        $end = [Math]::Min($j+20, $lines.Length-1)
        $chunk = $lines[$j..$end] -join "`n"
        if ($chunk -match "m_GameObject: \{fileID: $goId\}") {
          Write-Host "Scene Transform ID: $tId"
          Write-Host $chunk
          break
        }
      }
    }
  }
}

# Also check PrefabInstance modifications on Blue_Car_Final_Booster if it is a prefab instance
for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -match '부품_보조벤트판2' -or $lines[$i] -match '1662931201178478260' -or $lines[$i] -match '4946557352313361335') {
    Write-Host "Found reference at line $i : $($lines[$i])"
    $start = [Math]::Max(0, $i - 10)
    $end = [Math]::Min($lines.Length - 1, $i + 15)
    $lines[$start..$end] | ForEach-Object { Write-Host $_ }
  }
}
