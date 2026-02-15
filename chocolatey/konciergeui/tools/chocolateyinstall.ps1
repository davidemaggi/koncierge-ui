$ErrorActionPreference = 'Stop'

$packageName = 'konciergeui'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$url64 = '$url64$'
$checksum64 = '$checksum64$'

$packageArgs = @{
  packageName    = $packageName
  unzipLocation  = $toolsDir
  url64bit       = $url64
  checksum64     = $checksum64
  checksumType64 = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs

# Create shim for the executable
$exePath = Join-Path $toolsDir 'KonciergeUI.exe'

# Create desktop shortcut
$desktopPath = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath 'Koncierge UI.lnk'

Install-ChocolateyShortcut -ShortcutFilePath $shortcutPath -TargetPath $exePath -Description 'Koncierge UI - Kubernetes Port-Forward Manager'

Write-Host "Koncierge UI has been installed. You can run it using 'konciergeui' or from the desktop shortcut." -ForegroundColor Green

