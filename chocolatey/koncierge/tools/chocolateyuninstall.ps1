$ErrorActionPreference = 'Stop'

$packageName = 'koncierge'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

# Get install location from saved info or use default
$installInfoPath = Join-Path $toolsDir 'installinfo.txt'
if (Test-Path $installInfoPath) {
  $installDir = Get-Content $installInfoPath
} else {
  $installDir = Join-Path $env:LOCALAPPDATA 'Koncierge\CLI'
}

# Remove shim
Uninstall-BinFile -Name 'koncierge'

# Remove installed files
if (Test-Path $installDir) {
  Remove-Item $installDir -Recurse -Force
  Write-Host "Application files removed from: $installDir" -ForegroundColor Green
}

Write-Host "Koncierge CLI has been uninstalled." -ForegroundColor Green

