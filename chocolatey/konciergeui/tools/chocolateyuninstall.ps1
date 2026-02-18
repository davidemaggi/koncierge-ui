$ErrorActionPreference = 'Stop'

$packageName = 'konciergeui'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

# Get install location from saved info or use default
$installInfoPath = Join-Path $toolsDir 'installinfo.txt'
if (Test-Path $installInfoPath) {
  $installDir = Get-Content $installInfoPath
} else {
  $installDir = Join-Path $env:LOCALAPPDATA 'Koncierge\UI'
}

# Remove shim
Uninstall-BinFile -Name 'konciergeui'

# Remove desktop shortcut
$desktopPath = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath 'Koncierge UI.lnk'

if (Test-Path $shortcutPath) {
  Remove-Item $shortcutPath -Force
  Write-Host "Desktop shortcut removed." -ForegroundColor Green
}

# Remove Start Menu shortcut
$startMenuPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$startMenuShortcut = Join-Path $startMenuPath 'Koncierge UI.lnk'

if (Test-Path $startMenuShortcut) {
  Remove-Item $startMenuShortcut -Force
  Write-Host "Start Menu shortcut removed." -ForegroundColor Green
}

# Remove installed files
if (Test-Path $installDir) {
  Remove-Item $installDir -Recurse -Force
  Write-Host "Application files removed from: $installDir" -ForegroundColor Green
}

Write-Host "Koncierge UI has been uninstalled." -ForegroundColor Green

