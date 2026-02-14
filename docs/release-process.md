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

- Workflow file: `.github/workflows/release-windows.yml`
- Trigger: every push (including merges) into `main`.
- Steps:
  1. Resolve the next semantic version with GitVersion.
  2. Install the .NET 9 SDK + MAUI workloads and publish the Windows (win-x64) binaries as a portable ZIP.
  3. Calculate SHA256 hash for the ZIP file.
  4. Create/Update a GitHub Release with tag `v<semver>` and attach the ZIP.
  5. Install and use `wingetcreate` to generate WinGet manifest files.
  6. (Optional) Automatically submit PR to microsoft/winget-pkgs if configured.
  7. Upload WinGet manifest as a workflow artifact.

## WinGet Distribution

### Automatic Manifest Generation

The workflow uses [WinGetCreate](https://github.com/microsoft/winget-create) to automatically generate WinGet manifest files for each release:
- `DavideMaggi.KonciergeUI.yaml` - Version manifest
- `DavideMaggi.KonciergeUI.installer.yaml` - Installer configuration  
- `DavideMaggi.KonciergeUI.locale.en-US.yaml` - Package metadata

### Automatic Submission (Optional)

The workflow can automatically submit a PR to the [winget-pkgs](https://github.com/microsoft/winget-pkgs) repository. To enable this:

1. Create a GitHub Personal Access Token (PAT) with `public_repo` scope
2. Add it as a repository secret named `WINGET_GITHUB_TOKEN`
3. Create a repository variable `WINGET_AUTO_SUBMIT` with value `true`

When configured, each release will automatically create a PR to add/update the package in the WinGet repository.

### Manual Submission

If automatic submission is not configured, you can manually submit:

#### Option 1: Using the workflow artifact
1. Download the WinGet manifest artifact from the GitHub Actions run
2. Fork the [winget-pkgs](https://github.com/microsoft/winget-pkgs) repository
3. Create a new branch for your submission
4. Copy the manifest files to `manifests/d/DavideMaggi/KonciergeUI/<version>/`
5. Validate the manifest locally:
   ```powershell
   winget validate --manifest <path-to-manifest-folder>
   ```
6. Submit a Pull Request to `microsoft/winget-pkgs`

#### Option 2: Using WinGetCreate locally
```powershell
# Install wingetcreate
winget install Microsoft.WingetCreate

# Submit with your GitHub token
wingetcreate update DavideMaggi.KonciergeUI `
  --urls "https://github.com/<owner>/<repo>/releases/download/v<version>/KonciergeUi-win-<version>.zip" `
  --version "<version>" `
  --submit `
  --token "<your-github-pat>"
```

### Installation Methods

Once published to WinGet:
```powershell
# Install via WinGet
winget install DavideMaggi.KonciergeUI

# Or directly from GitHub release (manual)
# 1. Download KonciergeUi-win-<version>.zip
# 2. Extract to desired location
# 3. Run KonciergeUi.Client.exe
```

## Manual checks before merging

1. Run `dotnet build KonciergeUi.sln` to ensure the solution compiles with the resolved version metadata.
2. Validate the footer/info dialog shows the expected version/build when debugging locally.
3. Merge to `main`; the workflow will publish a release within a few minutes.
