# Release Process

This document summarizes how the automated Windows release flow works and how to inspect the GitVersion metadata locally.

## Versioning

- GitVersion drives every build. The configuration lives in `GitVersion.yml` and follows Continuous Delivery semantics.
- Branches derived from `main` inherit the version from the `main` line; merging back increments the patch number automatically.
- MAUI reads the generated values via the `GitVersion.MsBuild` package, so `ApplicationDisplayVersion`, `ApplicationVersion`, and assembly metadata always stay consistent.

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
  2. Install the .NET 9 SDK + MAUI workloads and publish the Windows (win-x64) binaries for both the portable ZIP and MSI installer.
  3. Compress the publish directory and upload it as a workflow artifact.
  4. Create/Update a GitHub Release with tag `v<semver>` and attach the zip.

> Note: Code-signing/notarization are intentionally skipped for now. Add the relevant tooling/secrets before distributing to end users.

## Manual checks before merging

1. Run `dotnet build KonciergeUi.sln` to ensure the solution compiles with the resolved version metadata.
2. Validate the footer/info dialog shows the expected version/build when debugging locally.
3. Merge to `main`; the workflow will publish a release within a few minutes.
