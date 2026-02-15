$ErrorActionPreference = 'Stop'

$packageName = 'konciergeui'

# Remove desktop shortcut
$desktopPath = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath 'Koncierge UI.lnk'

if (Test-Path $shortcutPath) {
  Remove-Item $shortcutPath -Force
  Write-Host "Desktop shortcut removed." -ForegroundColor Green
}

Write-Host "Koncierge UI has been uninstalled." -ForegroundColor Green

