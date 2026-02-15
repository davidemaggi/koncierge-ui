$ErrorActionPreference = 'Stop'

$packageName = 'koncierge'
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

Write-Host "Koncierge CLI has been installed. You can run it using 'koncierge' command." -ForegroundColor Green

