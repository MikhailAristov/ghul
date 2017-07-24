$processName = "Ghul_EXPO"
$executablePath = "C:\Program Files\GameLab\Ghul - Expo Mode\Ghul_EXPO.exe"
$waitBeforeRestart = 10

while(Test-Path -Path $executablePath) {
    if(!(Get-Process -Name $processName -EA 0)) {
        # Wait for a few seconds and double-check
        Write-Warning "$executablePath is not runnning! Waiting for $waitBeforeRestart seconds before restarting it..."
        Start-Sleep -Seconds $waitBeforeRestart
        if(!(Get-Process -Name $processName -EA 0)) {
            Write-Host "Restarting $executablePath..."
            # If it's still not running, re-launch it
            Start-Process -FilePath $executablePath
            # Let the process start before checking again
            Start-Sleep -Seconds 3
        }
    } else {
        Write-Debug "$executablePath is running, everything is good"
    }
    Start-Sleep -Seconds 1
}