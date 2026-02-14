<#
.SYNOPSIS
    Generates a self-signed code signing certificate for Koncierge UI.

.DESCRIPTION
    This script creates a self-signed certificate suitable for code signing Windows applications.
    It exports the certificate in two formats:
    - PFX file (with private key, password protected) - for signing
    - CER file (public key only) - for distribution to users

    The certificate is valid for 3 years and uses SHA256 hashing algorithm.

.PARAMETER CertName
    The subject name for the certificate. Default: "Koncierge UI"

.PARAMETER Password
    The password to protect the PFX file. If not provided, a strong random password will be generated.
    IMPORTANT: Use a strong password in production!

.PARAMETER OutputPath
    The directory where certificate files will be saved. Default: current directory

.EXAMPLE
    .\New-SelfSignedCodeCert.ps1 -CertName "Koncierge UI" -Password "MyStrongPassword123!" -OutputPath "C:\Certs"

.NOTES
    After running this script:
    1. Keep the PFX file secure (it contains the private key)
    2. Convert PFX to base64 for GitHub secrets:
       $bytes = [System.IO.File]::ReadAllBytes("KonciergeUI-CodeSigning.pfx")
       $base64 = [System.Convert]::ToBase64String($bytes)
       $base64 | Out-File "cert-base64.txt"
    3. Add the base64 string to GitHub secrets as WINDOWS_CERTIFICATE
    4. Add the password to GitHub secrets as WINDOWS_CERTIFICATE_PASSWORD
    5. Distribute the CER file to end users who want to trust your certificate
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$CertName = "Koncierge UI",
    
    [Parameter(Mandatory=$false)]
    [string]$Password,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "."
)

$ErrorActionPreference = 'Stop'

# Generate a strong random password if not provided
if ([string]::IsNullOrWhiteSpace($Password)) {
    Write-Host "No password provided. Generating a strong random password..." -ForegroundColor Yellow
    # Generate a 32-character random password with letters, numbers, and symbols
    $Password = -join ((48..57) + (65..90) + (97..122) + (33..47) | Get-Random -Count 32 | ForEach-Object {[char]$_})
    Write-Host "✓ Generated strong random password" -ForegroundColor Green
    Write-Host ""
}

# Validate output path
if (-not (Test-Path $OutputPath)) {
    Write-Host "Creating output directory: $OutputPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$OutputPath = Resolve-Path $OutputPath

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Self-Signed Code Signing Certificate" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Certificate Name: $CertName" -ForegroundColor White
Write-Host "Output Path:      $OutputPath" -ForegroundColor White
Write-Host ""

# Define file paths
$pfxPath = Join-Path $OutputPath "KonciergeUI-CodeSigning.pfx"
$cerPath = Join-Path $OutputPath "KonciergeUI-CodeSigning.cer"

# Check if files already exist
if ((Test-Path $pfxPath) -or (Test-Path $cerPath)) {
    Write-Warning "Certificate files already exist in $OutputPath"
    $response = Read-Host "Overwrite existing files? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

try {
    Write-Host "Creating self-signed certificate..." -ForegroundColor Green
    
    # Create the certificate
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=$CertName" `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(3) `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyUsage DigitalSignature `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3") # Code Signing EKU
    
    Write-Host "✓ Certificate created successfully" -ForegroundColor Green
    Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host "  Valid From: $($cert.NotBefore)" -ForegroundColor Gray
    Write-Host "  Valid To:   $($cert.NotAfter)" -ForegroundColor Gray
    Write-Host ""
    
    # Convert password to secure string
    $securePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText
    
    # Export PFX (with private key)
    Write-Host "Exporting PFX file (with private key)..." -ForegroundColor Green
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null
    Write-Host "✓ Exported: $pfxPath" -ForegroundColor Green
    Write-Host ""
    
    # Export CER (public key only)
    Write-Host "Exporting CER file (public key only)..." -ForegroundColor Green
    Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
    Write-Host "✓ Exported: $cerPath" -ForegroundColor Green
    Write-Host ""
    
    # Generate base64 for GitHub secrets
    Write-Host "Generating base64 encoding for GitHub secrets..." -ForegroundColor Green
    $pfxBytes = [System.IO.File]::ReadAllBytes($pfxPath)
    $base64Cert = [System.Convert]::ToBase64String($pfxBytes)
    $base64Path = Join-Path $OutputPath "cert-base64.txt"
    $base64Cert | Out-File -FilePath $base64Path -Encoding ASCII -NoNewline
    Write-Host "✓ Base64 saved to: $base64Path" -ForegroundColor Green
    Write-Host ""
    
    # Remove certificate from store (optional - user can keep it if needed)
    Write-Host "Removing certificate from current user store..." -ForegroundColor Yellow
    Remove-Item -Path "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force
    Write-Host "✓ Certificate removed from store" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  Certificate Generation Complete!" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Keep the PFX file ($pfxPath) SECURE" -ForegroundColor White
    Write-Host "   This contains your private key!" -ForegroundColor White
    Write-Host ""
    Write-Host "2. Add GitHub Secrets to your repository:" -ForegroundColor White
    Write-Host "   - WINDOWS_CERTIFICATE: Content of $base64Path" -ForegroundColor White
    Write-Host "   - WINDOWS_CERTIFICATE_PASSWORD: $Password" -ForegroundColor White
    Write-Host ""
    Write-Host "3. Distribute the CER file ($cerPath) to end users" -ForegroundColor White
    Write-Host "   Users must install it to 'Trusted Root Certification Authorities'" -ForegroundColor White
    Write-Host "   to trust your signed applications." -ForegroundColor White
    Write-Host ""
    Write-Host "4. See docs/SIGNING.md for complete instructions" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Error "Failed to create certificate: $_"
    exit 1
}
