# Code Signing Guide for Koncierge UI

> ⚠️ **ARCHIVED DOCUMENT** - As of the migration to WinGet distribution, code signing for MSIX packages is no longer used. This document is preserved for historical reference and in case code signing is needed in the future. For current release process, see [release-process.md](release-process.md).

This document explains how code signing works for Koncierge UI Windows releases, how to set up the signing infrastructure, and what end users need to know about trusting self-signed certificates.

## Table of Contents
- [Understanding Code Signing](#understanding-code-signing)
- [Self-Signed vs. Commercial Certificates](#self-signed-vs-commercial-certificates)
- [Setting Up Code Signing (For Developers)](#setting-up-code-signing-for-developers)
- [For End Users: Trusting the Certificate](#for-end-users-trusting-the-certificate)
- [Security Notes and Limitations](#security-notes-and-limitations)
- [Migration to Commercial Certificate](#migration-to-commercial-certificate)
- [Troubleshooting](#troubleshooting)

---

## Understanding Code Signing

Code signing is a cryptographic process that:
- **Verifies the publisher**: Confirms who created the software
- **Ensures integrity**: Guarantees the code hasn't been modified since signing
- **Reduces security warnings**: Signed applications are less likely to trigger Windows SmartScreen warnings

When you download and run unsigned software on Windows, you'll see warnings like:
- "Windows protected your PC"
- "Unknown publisher"
- SmartScreen warnings

Code signing helps eliminate these warnings, making it easier for users to install and trust your application.

---

## Self-Signed vs. Commercial Certificates

### Self-Signed Certificates

**Pros:**
- ✅ Free - no annual fees
- ✅ Full control - you manage the certificate
- ✅ Quick setup - create in minutes
- ✅ Good for open-source projects and testing

**Cons:**
- ❌ Not automatically trusted - users must manually install the certificate
- ❌ Windows SmartScreen warnings persist initially
- ❌ No third-party validation of identity
- ❌ Requires user education on installation

### Commercial Certificates (e.g., DigiCert, Sectigo, GlobalSign)

**Pros:**
- ✅ Automatically trusted by Windows
- ✅ No SmartScreen warnings (after building reputation)
- ✅ Validated identity through Certificate Authority
- ✅ Better user experience

**Cons:**
- ❌ Expensive ($200-$500+ per year)
- ❌ Requires business verification
- ❌ Extended Validation (EV) certificates require hardware token
- ❌ Annual renewal process

### Our Approach

Koncierge UI currently uses **self-signed certificates** because:
1. It's an open-source project without commercial backing
2. Users are technical (Kubernetes users) and can understand the trust process
3. The cost of commercial certificates is prohibitive for a free tool

We may migrate to a commercial certificate in the future if funding becomes available or the project gains significant adoption.

---

## Setting Up Code Signing (For Developers)

This section is for repository maintainers who need to set up code signing for the GitHub Actions workflow.

### Step 1: Generate a Self-Signed Certificate

You can generate the certificate on **Windows** or **macOS/Linux**.

#### Option A: On Windows (PowerShell)

```powershell
# Navigate to the repository scripts directory
cd scripts

# Generate the certificate
.\New-SelfSignedCodeCert.ps1 -CertName "Koncierge UI" -Password "YourStrongPassword123!" -OutputPath "C:\Certs"
```

**Parameters:**
- `-CertName`: The subject name for the certificate (default: "Koncierge UI")
- `-Password`: Password to protect the PFX file (use a strong password!)
- `-OutputPath`: Where to save the certificate files (default: current directory)

#### Option B: On macOS/Linux (Bash)

```bash
# Navigate to the repository scripts directory
cd scripts

# Make the script executable (first time only)
chmod +x new-self-signed-code-cert.sh

# Generate the certificate
./new-self-signed-code-cert.sh --name "Koncierge UI" --password "YourStrongPassword123!" --output ~/certs
```

**Parameters:**
- `--name` or `-n`: The subject name for the certificate (default: "Koncierge UI")
- `--password` or `-p`: Password to protect the PFX file (auto-generated if not provided)
- `--output` or `-o`: Where to save the certificate files (default: current directory)

> **Note:** The macOS/Linux script requires `openssl` to be installed. On macOS, you can install it with `brew install openssl`.

**Output files (both scripts):**
- `KonciergeUI-CodeSigning.pfx` - Private key (password-protected) - **KEEP THIS SECRET!**
- `KonciergeUI-CodeSigning.cer` - Public key - Distribute to users
- `cert-base64.txt` - Base64-encoded PFX for GitHub secrets

### Step 2: Add GitHub Secrets

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add the following two secrets:

#### Secret 1: WINDOWS_CERTIFICATE
- **Name**: `WINDOWS_CERTIFICATE`
- **Value**: The entire content of `cert-base64.txt` (the base64-encoded certificate)
- This is a long string of random characters

#### Secret 2: WINDOWS_CERTIFICATE_PASSWORD
- **Name**: `WINDOWS_CERTIFICATE_PASSWORD`
- **Value**: The password you used when creating the certificate
- Example: `YourStrongPassword123!`

### Step 3: Verify the Workflow

The `.github/workflows/release-windows.yml` workflow will automatically:
1. Import the certificate from the GitHub secret
2. Sign all `.exe` and `.dll` files in the ZIP release
3. Sign the MSIX package
4. Clean up the certificate after the build
5. Create a release with signed binaries

### Step 4: Distribute the Public Certificate

Share the `KonciergeUI-CodeSigning.cer` file with end users:
- Add it to the GitHub release assets
- Include it in the repository (e.g., `certs/` directory)
- Reference it in the README

---

## For End Users: Trusting the Certificate

If you download Koncierge UI and see Windows security warnings, you need to install the public certificate to trust the application.

### Why Am I Seeing Warnings?

The application is signed with a **self-signed certificate**, which means:
- The code is properly signed and hasn't been tampered with
- However, Windows doesn't automatically trust self-signed certificates
- You need to manually tell Windows to trust the Koncierge UI certificate

### Installing the Certificate (Windows 10/11)

#### Option 1: Quick Install (Recommended)

1. Download `KonciergeUI-CodeSigning.cer` from the [latest release](https://github.com/davidemaggi/koncierge-ui/releases)
2. Right-click the `.cer` file → **Install Certificate**
3. Choose **Local Machine** (requires admin rights) or **Current User**
4. Select **Place all certificates in the following store**
5. Click **Browse** → Choose **Trusted Root Certification Authorities**
6. Click **OK** → **Next** → **Finish**
7. Confirm the security warning

#### Option 2: Manual Install via Certificate Manager

1. Press `Win + R`, type `certmgr.msc`, press Enter
2. Navigate to **Trusted Root Certification Authorities** → **Certificates**
3. Right-click **Certificates** → **All Tasks** → **Import**
4. Follow the wizard to import `KonciergeUI-CodeSigning.cer`
5. Restart any open applications

#### Option 3: PowerShell (Admin)

```powershell
# Import the certificate to Trusted Root CA store
Import-Certificate -FilePath "KonciergeUI-CodeSigning.cer" -CertStoreLocation Cert:\LocalMachine\Root
```

### Verifying the Installation

After installing the certificate:

1. Right-click a signed `.exe` file → **Properties**
2. Go to the **Digital Signatures** tab
3. Select the signature → **Details** → **View Certificate**
4. Verify:
   - **Issued to**: Koncierge UI (or the certificate subject name)
   - **Issued by**: Koncierge UI (self-signed)
   - Certificate is valid and not expired

### SmartScreen Warnings

Even with the certificate installed, you might still see SmartScreen warnings:
- **"Windows protected your PC"**
- **"Unrecognized app"**

**Why?** Windows SmartScreen uses reputation-based filtering. Self-signed apps don't have reputation, so warnings may appear even after trusting the certificate.

**How to proceed:**
1. Click **More info**
2. Click **Run anyway**

---

## Security Notes and Limitations

### ⚠️ Important Security Considerations

1. **Trust Implications**
   - By installing the certificate, you're trusting **all software** signed with that certificate
   - Only install certificates from sources you trust
   - Verify the certificate fingerprint if provided by the developer

2. **Self-Signed Limitations**
   - No third-party validation of the publisher's identity
   - Certificate could be recreated by anyone with the same name
   - No legal accountability like with commercial certificates

3. **Best Practices for Users**
   - Only download releases from the official GitHub repository
   - Verify checksums if provided
   - Review the code before running (it's open source!)
   - Keep the certificate updated if it's renewed

4. **Private Key Security (For Developers)**
   - The `.pfx` file contains the private key - **NEVER share it publicly**
   - Store GitHub secrets securely
   - Rotate certificates periodically (every 1-3 years)
   - Revoke and recreate if the private key is compromised

### What Code Signing Does NOT Do

- ❌ Does not guarantee the software is safe or bug-free
- ❌ Does not mean the software is "approved" by Microsoft
- ❌ Does not prevent malware (it only confirms the signer's identity)
- ❌ Does not automatically make SmartScreen warnings disappear

Code signing is about **identity and integrity**, not safety or quality.

---

## Migration to Commercial Certificate

If Koncierge UI transitions to a commercial certificate in the future:

### For Developers

1. **Purchase a code signing certificate**
   - Recommended providers: DigiCert, Sectigo, GlobalSign
   - Choose between Standard ($200-300/year) or EV ($300-500/year)
   - EV certificates provide better SmartScreen reputation

2. **Update GitHub Secrets**
   - Replace `WINDOWS_CERTIFICATE` with the new PFX (base64-encoded)
   - Update `WINDOWS_CERTIFICATE_PASSWORD` with the new password

3. **No workflow changes needed**
   - The signing workflow works the same with commercial certificates
   - SmartScreen warnings will gradually disappear as reputation builds

### For End Users

- **No action needed!** Commercial certificates are automatically trusted by Windows
- You can remove the old self-signed certificate from your Trusted Root store if desired

---

## Troubleshooting

### Certificate Import Fails in Workflow

**Error**: `Cannot find object or property`

**Solutions**:
- Verify the `WINDOWS_CERTIFICATE` secret contains valid base64
- Check that `WINDOWS_CERTIFICATE_PASSWORD` is correct
- Ensure the certificate hasn't expired

### Signing Fails During Build

**Error**: `SignTool Error: No certificates were found that met all the given criteria`

**Solutions**:
- Check that the certificate was imported successfully
- Verify the certificate thumbprint matches
- Ensure the certificate has Code Signing purpose (EKU 1.3.6.1.5.5.7.3.3)

### MSIX Won't Install After Signing

**Error**: `This app package's publisher certificate could not be verified`

**Solutions**:
- Install the `.cer` file to Trusted Root Certification Authorities
- Verify the certificate is not expired
- Check that the certificate subject name matches the MSIX publisher

### Certificate Expired

If your self-signed certificate expires:

1. Generate a new certificate using the script
2. Update GitHub secrets with the new certificate
3. Rebuild and re-release signed versions
4. Notify users to install the new certificate
5. Remove the old certificate from user systems

### Workflow Skips Signing

If `WINDOWS_CERTIFICATE` or `WINDOWS_CERTIFICATE_PASSWORD` secrets are not configured, the workflow will skip signing and continue without errors. This allows forks and test builds to work without certificates.

---

## Additional Resources

- [Microsoft: Code Signing Best Practices](https://docs.microsoft.com/windows/win32/seccrypto/cryptography-tools)
- [SignTool Documentation](https://docs.microsoft.com/windows/win32/seccrypto/signtool)
- [Understanding Windows SmartScreen](https://docs.microsoft.com/windows/security/threat-protection/windows-defender-smartscreen/windows-defender-smartscreen-overview)
- [MSIX Signing Requirements](https://docs.microsoft.com/windows/msix/package/sign-app-package-using-signtool)

---

## Questions or Issues?

If you encounter problems with code signing:

1. Check this document first
2. Search [existing issues](https://github.com/davidemaggi/koncierge-ui/issues)
3. Open a new issue with:
   - Your Windows version
   - Error messages (sanitize any sensitive info)
   - Steps to reproduce

---

**Last Updated**: 2026-02-13  
**Certificate Expiry**: Check the latest release notes for current certificate validity
