; Koncierge CLI Inno Setup Script
; This script creates an installer for the CLI tool

#define MyAppName "Koncierge CLI"
#define MyAppPublisher "Davide Maggi"
#define MyAppURL "https://github.com/davidemaggi/koncierge-ui"
#define MyAppExeName "Koncierge.exe"

[Setup]
AppId={{C8B0F4G3-2D5E-5F6G-9B7C-0D1E2F3G4H5I}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\Koncierge
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; LicenseFile is relative to the .iss file location, need to go up one level
LicenseFile=..\LICENSE
OutputDir=..\artifacts
OutputBaseFilename=Koncierge-cli-setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "addtopath"; Description: "Add 'koncierge' command to PATH"; GroupDescription: "Additional options:"; Flags: checked

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
; Add to user PATH if selected
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Tasks: addtopath; Check: NeedsAddPath(ExpandConstant('{app}'))

[Code]
function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_CURRENT_USER, 'Environment', 'Path', OrigPath)
  then begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Param + ';', ';' + OrigPath + ';') = 0;
end;


