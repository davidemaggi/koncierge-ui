# Installer Scripts

This directory contains [Inno Setup](https://jrsoftware.org/isinfo.php) scripts used to create Windows installers.

## Files

| File | Description |
|------|-------------|
| `konciergeui.iss` | Installer script for the desktop application (KonciergeUI) |
| `koncierge-cli.iss` | Installer script for the CLI tool (Koncierge) |

## Features

### Desktop Application Installer (`konciergeui.iss`)

- **Executable**: `KonciergeUI.exe`
- **Installation directory**: `%LOCALAPPDATA%\Programs\KonciergeUI`
- **Features**:
  - Creates Start Menu shortcut
  - Optional desktop shortcut (user-selectable during install)
  - Optional PATH registration for `konciergeui` command
  - Supports English and Italian languages

### CLI Installer (`koncierge-cli.iss`)

- **Executable**: `Koncierge.exe`
- **Installation directory**: `%LOCALAPPDATA%\Programs\Koncierge`
- **Features**:
  - Creates Start Menu shortcut
  - PATH registration for `koncierge` command (selected by default)
  - Supports English and Italian languages

## Building Installers Locally

1. Install [Inno Setup 6](https://jrsoftware.org/isdl.php)
2. Build the project first:
   ```powershell
   # Desktop App
   dotnet publish KonciergeUi.Client/KonciergeUi.Client.csproj -c Release -f net9.0-windows10.0.19041.0 -p:RuntimeIdentifierOverride=win10-x64

   # CLI
   dotnet publish KonciergeUI.Cli/KonciergeUI.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```
3. Convert the icon (for desktop app only):
   ```powershell
   # Using ImageMagick
   magick convert -background none -density 256 KonciergeUi.Client/Resources/AppIcon/appicon.svg -define icon:auto-resize=256,128,64,48,32,16 KonciergeUi.Client/Resources/AppIcon/appicon.ico
   ```
4. Run Inno Setup compiler:
   ```powershell
   # Desktop App
   & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DMyAppVersion=1.0.0 /DMyAppSourceDir="KonciergeUi.Client\bin\Release\net9.0-windows10.0.19041.0\win10-x64\publish" installer\konciergeui.iss

   # CLI
   & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DMyAppVersion=1.0.0 /DMyAppSourceDir="KonciergeUI.Cli\bin\Release\net9.0\win-x64\publish" installer\koncierge-cli.iss
   ```

The installers will be created in the `artifacts/` directory.

## WinGet Integration

When installed via WinGet, the installers are run with these silent switches:

**Desktop App:**
```
/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TASKS=desktopicon,addtopath
```

**CLI:**
```
/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TASKS=addtopath
```

This ensures that desktop shortcuts are created and PATH is registered automatically during WinGet installation.

