# Find the vertex bounds of BoostOn_AsphaltRoad.asset
$lines = Get-Content "Assets\Terrain\BoostOn_AsphaltRoad.asset"
$boundsLine = $lines | Select-String "m_Bounds:" -Context 0, 5
Write-Output $boundsLine
