$content = Get-Content 'Assets/02YUJEONG/boostOn_Yujeong.unity'
for ($i = 0; $i -lt $content.Length; $i++) {
    if ($content[$i] -match '&892663354226777002$') {
        $end = [Math]::Min($content.Length - 1, $i + 20)
        for ($j = $i; $j -le $end; $j++) {
            Write-Host $content[$j]
        }
        break
    }
}
