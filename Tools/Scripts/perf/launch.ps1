
$beg_tp = Get-Date -UFormat %s
start shell:AppsFolder\Microsoft.WindowsCalculator.Dev_8wekyb3d8bbwe!App
$end_tp = Get-Date -UFormat %s

$logs = "`n----------------------------------------`n"
$logs += "$beg_tp | start launching`n"
$logs += "$end_tp | finished launching`n"

Add-Content -Path .\logs -Value $logs