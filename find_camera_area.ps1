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
                Line = $i
            }
        }
    }
}

$nearCamera = $objects | Where-Object { $_.X -ge -2050 -and $_.X -le -1800 }
$nearCamera | Format-Table -AutoSize
