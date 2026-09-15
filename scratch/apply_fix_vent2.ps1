$scenePath = 'Assets\02YUJEONG\boostOn_Yujeong_1.unity'
$prefabPath = 'Assets\Prefabs\Final_Car_Booster\Blue_Car_Final_Booster.prefab'

function Fix-Vent2($filePath, $tId) {
    $content = [System.IO.File]::ReadAllText($filePath)
    $pattern = "(--- !u!4 &$tId\r?\nTransform:\r?\n[\s\S]*?m_LocalRotation: )\{[^}]+\}"
    $replacement = "`${1}{x: -0.5147691, y: 0.4847812, z: 0.5147691, w: 0.4847812}"
    if ($content -match $pattern) {
        $content = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)
        [System.IO.File]::WriteAllText($filePath, $content)
        Write-Host "Successfully fixed rotation in $filePath for Transform $tId"
    } else {
        Write-Warning "Could not match pattern in $filePath for Transform $tId"
    }
}

# Scene Transform ID for 부품_보조벤트판2 is 5718442549163180711
Fix-Vent2 $scenePath '5718442549163180711'

# Prefab Transform ID for 부품_보조벤트판2 is 1662931201178478260
Fix-Vent2 $prefabPath '1662931201178478260'
