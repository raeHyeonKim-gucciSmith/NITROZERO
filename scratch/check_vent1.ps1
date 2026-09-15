$files = @(
  'Assets\02YUJEONG\boostOn_Yujeong_1.unity',
  'Assets\Prefabs\Final_Car_Booster\Blue_Car_Final_Booster.prefab'
)

foreach ($f in $files) {
  Write-Host "=== $f ==="
  $lines = Get-Content $f
  for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '\\uBD80\\uD488_\\uBCF4\\uC870\\uBCA4\\uD2B8\\uD3101' -or $lines[$i] -match '부품_보조벤트판1') {
      $goIdx = $i
      while ($goIdx -ge 0 -and $lines[$goIdx] -notmatch '--- !u!1 &(\d+)') { $goIdx-- }
      $goId = if ($lines[$goIdx] -match '--- !u!1 &(\d+)') { $Matches[1] } else { 'unknown' }
      Write-Host "Vent 1 GameObject ID: $goId"
      for ($j = 0; $j -lt $lines.Length; $j++) {
        if ($lines[$j] -match '--- !u!4 &(\d+)') {
          $tId = $Matches[1]
          $end = [Math]::Min($j+20, $lines.Length-1)
          $chunk = $lines[$j..$end] -join "`n"
          if ($chunk -match "m_GameObject: \{fileID: $goId\}") {
            Write-Host "Vent 1 Transform ID: $tId"
            Write-Host $chunk
            break
          }
        }
      }
    }
  }
}
