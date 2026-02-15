$ErrorActionPreference = 'Stop'

$packageName = 'koncierge'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

# Install to user-writable location
$installDir = Join-Path $env:LOCALAPPDATA 'Koncierge\CLI'

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

$exePath = Join-Path $installDir 'Koncierge.exe'

# Create shim (Chocolatey will make this available in PATH)
Install-BinFile -Name 'koncierge' -Path $exePath

# Save install location for uninstall
$installInfoPath = Join-Path $toolsDir 'installinfo.txt'
Set-Content -Path $installInfoPath -Value $installDir

Write-Host "Koncierge CLI has been installed to: $installDir" -ForegroundColor Green
Write-Host "You can run it using 'koncierge' command." -ForegroundColor Green

