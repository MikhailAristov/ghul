#####################################################################################
#
# This script is installed along with the exposition/musium version of the Ghul 
# video game for Windows. It is supposed to run in a background command line window,
# and automatically restarts the game executable after 10 seconds if the player quits 
# the game. The installer automatically places a shortcut to this script on the
# current user's Windows Desktop.
#
# To LAUNCH the auto-restart script, right-click on its Desktop shortcut and select 
# "Execute in Powershell" from the popup menu. This will open a Powershell console
# window that will then launch the game. The console window will remain open, so just
# minimize it for now.
#
# To STOP the auto-restart script, simply close the Powershell console window with 
# the close window button (usually a red X in the top-right corner).
#
#####################################################################################

$processName = "Ghul_EXPO"
$executablePath = "Ghul_EXPO.exe"
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