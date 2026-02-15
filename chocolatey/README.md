# Chocolatey Packages

This directory contains [Chocolatey](https://chocolatey.org/) package definitions for Koncierge applications.

## Packages

| Package | Chocolatey ID | Description |
|---------|---------------|-------------|
| Desktop App | `konciergeui` | Kubernetes port-forwarding manager with UI |
| CLI Tool | `koncierge` | Command-line interface for Kubernetes port-forwarding |

## Installation

Once published to the Chocolatey Community Repository:

```powershell
# Install Desktop App (creates desktop shortcut)
choco install konciergeui

# Install CLI Tool (adds 'koncierge' to PATH)
choco install koncierge
```

## Package Structure

```
chocolatey/
├── konciergeui/
│   ├── konciergeui.nuspec      # Package metadata
│   └── tools/
│       ├── chocolateyinstall.ps1    # Install script
│       └── chocolateyuninstall.ps1  # Uninstall script
└── koncierge/
    ├── koncierge.nuspec        # Package metadata
    └── tools/
        ├── chocolateyinstall.ps1    # Install script
        └── chocolateyuninstall.ps1  # Uninstall script
```

## Building Packages Locally

1. Install Chocolatey: https://chocolatey.org/install
2. Update the placeholders in the install scripts:
   - `$url64$` → URL to the ZIP file
   - `$checksum64$` → SHA256 hash of the ZIP file
3. Update `$version$` in the nuspec file
4. Build the package:

```powershell
cd chocolatey/konciergeui
choco pack

cd ../koncierge
choco pack
```

## Publishing

### Automatic (via GitHub Actions)

The workflow automatically publishes to Chocolatey when:
1. `CHOCOLATEY_API_KEY` secret is configured
2. `CHOCOLATEY_AUTO_PUBLISH` repository variable is set to `true`

### Manual

```powershell
# Get your API key from https://community.chocolatey.org/account
choco push konciergeui.<version>.nupkg --source https://push.chocolatey.org/ --api-key <your-api-key>
choco push koncierge.<version>.nupkg --source https://push.chocolatey.org/ --api-key <your-api-key>
```

## Notes

- Packages are built from the portable ZIP releases
- The desktop app creates a desktop shortcut during installation
- The CLI tool is automatically added to PATH via Chocolatey's shim mechanism
- First-time submissions require manual approval (usually 1-2 days)
- Subsequent updates are typically auto-approved if they pass automated checks

