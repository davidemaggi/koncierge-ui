$ErrorActionPreference = 'Stop'

$packageName = 'konciergeui'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

# Install to user-writable location
$installDir = Join-Path $env:LOCALAPPDATA 'Koncierge\UI'

$url64 = '$url64$'
$checksum64 = '$checksum64$'

$packageArgs = @{
  packageName    = $packageName
  unzipLocation  = $installDir
  url64bit       = $url64
  checksum64     = $checksum64
  checksumType64 = 'sha256'
}

# Clean previous installation if exists
if (Test-Path $installDir) {
  Remove-Item $installDir -Recurse -Force
}

# Create install directory
New-Item -ItemType Directory -Path $installDir -Force | Out-Null

Install-ChocolateyZipPackage @packageArgs

$exePath = Join-Path $installDir 'KonciergeUI.exe'

# Create shim in tools directory (Chocolatey will add this to PATH)
$shimPath = Join-Path $toolsDir 'konciergeui.exe'
Install-BinFile -Name 'konciergeui' -Path $exePath

# Create desktop shortcut
$desktopPath = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath 'Koncierge UI.lnk'

Install-ChocolateyShortcut -ShortcutFilePath $shortcutPath -TargetPath $exePath -Description 'Koncierge UI - Kubernetes Port-Forward Manager'

# Create Start Menu shortcut
$startMenuPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$startMenuShortcut = Join-Path $startMenuPath 'Koncierge UI.lnk'

Install-ChocolateyShortcut -ShortcutFilePath $startMenuShortcut -TargetPath $exePath -Description 'Koncierge UI - Kubernetes Port-Forward Manager'

# Save install location for uninstall
$installInfoPath = Join-Path $toolsDir 'installinfo.txt'
Set-Content -Path $installInfoPath -Value $installDir

Write-Host "Koncierge UI has been installed to: $installDir" -ForegroundColor Green
Write-Host "You can run it using 'konciergeui' command or from the desktop/Start Menu shortcut." -ForegroundColor Green

