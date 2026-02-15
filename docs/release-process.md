# Release Process

This document summarizes how the automated Windows release flow works and how to inspect the GitVersion metadata locally.

## Versioning

- GitVersion drives every build. The configuration lives in `GitVersion.yml` and follows Continuous Delivery semantics.
- Branches derived from `main` inherit the version from the `main` line; merging back increments the patch number automatically.
- MAUI reads the generated values via the `GitVersion.MsBuild` package, so `ApplicationDisplayVersion`, `ApplicationVersion`, and assembly metadata always stay consistent.

### How version incrementing works

The version is automatically incremented based on commits to `main`. By default, each commit increments the **patch** version.

#### Controlling version increments via commit messages

Add one of these tags to your commit message to control the increment:

| Tag | Effect | Example |
|-----|--------|---------|
| `+semver: major` or `+semver: breaking` | 1.0.0 → 2.0.0 | Breaking API changes |
| `+semver: minor` or `+semver: feature` | 1.0.0 → 1.1.0 | New features |
| `+semver: patch` or `+semver: fix` | 1.0.0 → 1.0.1 | Bug fixes (default) |
| `+semver: none` or `+semver: skip` | No increment | Docs, CI changes |

**Example commit messages:**
```bash
git commit -m "Add new cluster discovery feature +semver: minor"
git commit -m "Breaking: Change port forward API +semver: major"
git commit -m "Fix connection timeout issue +semver: patch"
git commit -m "Update README +semver: none"
```

#### Setting a specific version with Git tags

To set the version explicitly, create an annotated tag:
```bash
# Set version to 1.0.0
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0
```
Subsequent commits will increment from this tagged version.

#### Ensuring version increases every release

The current configuration (`mode: ContinuousDelivery` + `increment: patch`) ensures that:
1. Every commit to `main` gets a unique version
2. The patch number increments automatically
3. Pre-release tags (e.g., `1.0.1-alpha.1`) are used for feature branches

**Important:** If multiple commits are pushed at once (e.g., merge commit), each build will have a unique version based on commit count since the last tag.

### Local version preview

```bash
# install once
dotnet tool install --global GitVersion.Tool

# inside the repo root
GitVersion
```

The `SemVer`, `InformationalVersion`, and `PreReleaseTag` outputs mirror the values the CI workflow will consume.

## App surfacing

The app retrieves the computed values through `IAppVersionProvider`, which wraps `AppInfo.Current`. Razor components (footer/info dialog) bind to this service so the UI instantly reflects the build version without manual edits.

## GitHub Actions workflow

The project uses two separate workflows for Windows releases:

### Desktop Application (MAUI)

- Workflow file: `.github/workflows/release-windows.yml`
- Trigger: every push (including merges) into `main`.
- Steps:
  1. Resolve the next semantic version with GitVersion.
  2. Install the .NET 9 SDK + MAUI workloads and publish the Windows (win-x64) binaries.
  3. Convert the SVG icon to ICO format using ImageMagick.
  4. Build an Inno Setup installer with desktop shortcut support.
  5. Create a portable ZIP archive.
  6. Calculate SHA256 hashes for both artifacts.
  7. Create/Update a GitHub Release with tag `v<semver>` and attach both installer and ZIP.
  8. Install and use `wingetcreate` to generate WinGet manifest files for `DavideMaggi.KonciergeUI`.
  9. (Optional) Automatically submit PR to microsoft/winget-pkgs if configured.
  10. Upload WinGet manifest as a workflow artifact.

**Output executables:**
- `KonciergeUI.exe` - Main application executable
- `KonciergeUI-setup-X.Y.Z.exe` - Inno Setup installer (creates desktop shortcut, adds `konciergeui` to PATH)
- `KonciergeUi-win-X.Y.Z.zip` - Portable ZIP package

### CLI Application

- Workflow file: `.github/workflows/release-cli-windows.yml`
- Trigger: every push (including merges) into `main`.
- Steps:
  1. Resolve the next semantic version with GitVersion.
  2. Install the .NET 9 SDK and publish the CLI (win-x64) as a self-contained single-file executable.
  3. Build an Inno Setup installer with PATH registration.
  4. Create a portable ZIP archive.
  5. Calculate SHA256 hashes for both artifacts.
  6. Add CLI artifacts to the same GitHub Release (`v<semver>`) created by the MAUI workflow.
  7. Install and use `wingetcreate` to generate WinGet manifest files for `DavideMaggi.Koncierge`.
  8. (Optional) Automatically submit PR to microsoft/winget-pkgs if configured.
  9. Upload WinGet manifest as a workflow artifact.

**Output executables:**
- `Koncierge.exe` - CLI executable
- `Koncierge-cli-setup-X.Y.Z.exe` - Inno Setup installer (adds `koncierge` to PATH)
- `Koncierge-cli-win-X.Y.Z.zip` - Portable ZIP package

## WinGet Distribution

### Package Identifiers

| Application | WinGet Package ID | Command | Executable |
|-------------|-------------------|---------|------------|
| Desktop App | `DavideMaggi.KonciergeUI` | `konciergeui` | `KonciergeUI.exe` |
| CLI Tool | `DavideMaggi.Koncierge` | `koncierge` | `Koncierge.exe` |

### Automatic Manifest Generation

The workflows use [WinGetCreate](https://github.com/microsoft/winget-create) to automatically generate WinGet manifest files for each release:

**Desktop Application (`DavideMaggi.KonciergeUI`):**
- `DavideMaggi.KonciergeUI.yaml` - Version manifest
- `DavideMaggi.KonciergeUI.installer.yaml` - Inno Setup installer configuration (with desktop shortcut)
- `DavideMaggi.KonciergeUI.locale.en-US.yaml` - Package metadata

**CLI Tool (`DavideMaggi.Koncierge`):**
- `DavideMaggi.Koncierge.yaml` - Version manifest
- `DavideMaggi.Koncierge.installer.yaml` - Inno Setup installer configuration (with PATH registration)
- `DavideMaggi.Koncierge.locale.en-US.yaml` - Package metadata

### Installer Features

The Inno Setup installers provide the following features:

**Desktop Application:**
- Creates desktop shortcut (optional during installation, enabled by default via WinGet)
- Adds installation directory to user PATH (optional)
- Registers `konciergeui` command

**CLI Tool:**
- Adds installation directory to user PATH (enabled by default)
- Registers `koncierge` command

### Automatic Submission (Optional)

The workflow can automatically submit a PR to the [winget-pkgs](https://github.com/microsoft/winget-pkgs) repository. To enable this:

1. Create a GitHub Personal Access Token (PAT) with `public_repo` scope
2. Add it as a repository secret named `WINGET_GITHUB_TOKEN`
3. Create a repository variable `WINGET_AUTO_SUBMIT` with value `true`

When configured, each release will automatically create a PR to add/update the package in the WinGet repository.

### Manual Submission

If automatic submission is not configured, you can manually submit:

#### Option 1: Using the workflow artifact
1. Download the WinGet manifest artifact from the GitHub Actions run:
   - `winget-manifest-<version>` for Desktop App
   - `winget-manifest-cli-<version>` for CLI Tool
2. Fork the [winget-pkgs](https://github.com/microsoft/winget-pkgs) repository
3. Create a new branch for your submission
4. Copy the manifest files to:
   - Desktop: `manifests/d/DavideMaggi/KonciergeUI/<version>/`
   - CLI: `manifests/d/DavideMaggi/Koncierge/<version>/`
5. Validate the manifest locally:
   ```powershell
   winget validate --manifest <path-to-manifest-folder>
   ```
6. Submit a Pull Request to `microsoft/winget-pkgs`

#### Option 2: Using WinGetCreate locally
```powershell
# Install wingetcreate
winget install Microsoft.WingetCreate

# Submit Desktop App
wingetcreate update DavideMaggi.KonciergeUI `
  --urls "https://github.com/<owner>/<repo>/releases/download/v<version>/KonciergeUI-setup-<version>.exe" `
  --version "<version>" `
  --submit `
  --token "<your-github-pat>"

# Submit CLI Tool
wingetcreate update DavideMaggi.Koncierge `
  --urls "https://github.com/<owner>/<repo>/releases/download/v<version>/Koncierge-cli-setup-<version>.exe" `
  --version "<version>" `
  --submit `
  --token "<your-github-pat>"
```

### Installation Methods

Once published to WinGet:
```powershell
# Install Desktop App via WinGet (creates desktop shortcut)
winget install DavideMaggi.KonciergeUI

# Install CLI Tool via WinGet (adds 'koncierge' to PATH)
winget install DavideMaggi.Koncierge

# Or directly from GitHub release (manual)
# Desktop App:
#   1. Download KonciergeUI-setup-<version>.exe (installer) or KonciergeUi-win-<version>.zip (portable)
#   2. Run installer or extract ZIP to desired location
#   3. Run KonciergeUI.exe

# CLI Tool:
#   1. Download Koncierge-cli-setup-<version>.exe (installer) or Koncierge-cli-win-<version>.zip (portable)
#   2. Run installer or extract ZIP to desired location
#   3. Run Koncierge.exe (or use 'koncierge' command if installed via installer)
```

## Manual checks before merging

1. Run `dotnet build KonciergeUi.sln` to ensure the solution compiles with the resolved version metadata.
2. Validate the footer/info dialog shows the expected version/build when debugging locally.
3. Merge to `main`; the workflow will publish a release within a few minutes.

## Chocolatey Distribution

### Package Identifiers

| Application | Chocolatey Package ID | Command |
|-------------|----------------------|---------|
| Desktop App | `konciergeui` | `konciergeui` |
| CLI Tool | `koncierge` | `koncierge` |

### Automatic Publishing

The workflows automatically build Chocolatey packages (`.nupkg`) for each release. To enable automatic publishing to the Chocolatey Community Repository:

1. Get your API key from https://community.chocolatey.org/account
2. Add it as a repository secret named `CHOCOLATEY_API_KEY`
3. Create a repository variable `CHOCOLATEY_AUTO_PUBLISH` with value `true`

### Manual Publishing

If automatic publishing is not configured:

1. Download the Chocolatey package artifact from the GitHub Actions run:
   - `chocolatey-konciergeui-<version>` for Desktop App
   - `chocolatey-koncierge-<version>` for CLI Tool
2. Push manually:
   ```powershell
   choco push konciergeui.<version>.nupkg --source https://push.chocolatey.org/ --api-key <your-api-key>
   choco push koncierge.<version>.nupkg --source https://push.chocolatey.org/ --api-key <your-api-key>
   ```

### Installation via Chocolatey

Once published:
```powershell
# Install Desktop App
choco install konciergeui

# Install CLI Tool
choco install koncierge
```

### Notes

- First-time package submissions require manual moderation (1-2 days)
- Subsequent updates are typically auto-approved if they pass automated validation
- Packages use the portable ZIP releases as the source

