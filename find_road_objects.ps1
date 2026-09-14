$lines = Get-Content "Assets\02YUJEONG\boostOn_Yujeong.unity"
$objects = @()
$currentName = ""
$currentPos = ""
$currentScale = ""
$currentRot = ""

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match "m_Name:\s*(.+)") {
        $currentName = $matches[1]
    }
    if ($line -match "m_LocalPosition:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}") {
        $posX = [float]$matches[1]
        $posY = [float]$matches[2]
        $posZ = [float]$matches[3]
        if ($currentName -ne "") {
            $objects += [PSCustomObject]@{
                Name = $currentName
                X = $posX
                Y = $posY
                Z = $posZ
                Line = $i
            }
        }
    }
}

Write-Output "Total objects with pos: $($objects.Count)"
# Filter objects near the road corridor: Y between -30 and 10, Z between -50 and 50
$roadObjects = $objects | Where-Object { $_.Y -ge -35 -and $_.Y -le 20 -and $_.Z -ge -60 -and $_.Z -le 60 }
Write-Output "Road corridor objects: $($roadObjects.Count)"
$roadObjects | Sort-Object X | Select-Object -First 30 Name, X, Y, Z | Format-Table -AutoSize
