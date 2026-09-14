$lines = Get-Content "Assets\02YUJEONG\boostOn_Yujeong.unity"
$objects = @()
$currentName = ""

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
            }
        }
    }
}

# Filter objects where X > -1900 and X < 5000 and Y is between -40 and 20 and Z is between -100 and 100
$aheadObjects = $objects | Where-Object { $_.X -gt -1900 -and $_.X -lt 5000 -and $_.Y -ge -40 -and $_.Y -le 20 -and [Math]::Abs($_.Z) -le 100 }
$aheadObjects | Sort-Object X | Format-Table -AutoSize
