; FathomOS Windows Installer Script
; Created for Inno Setup 6.x
; Version 1.0.48

#define MyAppName "FathomOS"
#define MyAppVersion "1.0.48"
#define MyAppPublisher "Fathom OS"
#define MyAppURL "https://fathomos.com"
#define MyAppExeName "FathomOS.exe"
#define MyAppAssocName MyAppName + " Project File"
#define MyAppAssocExt ".fathom"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; Application identification
AppId={{8F4A9B2C-3D5E-4F6A-B7C8-9D0E1F2A3B4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation directories
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

; Allow user to choose installation directory
DisableDirPage=no

; Installer output settings
OutputDir=output
OutputBaseFilename=FathomOS-Setup-{#MyAppVersion}

; Compression settings (LZMA2 provides best compression)
Compression=lzma2/ultra64
SolidCompression=yes

; Modern wizard style
WizardStyle=modern
WizardSizePercent=100

; Minimum Windows version (Windows 10 or later recommended for .NET 8)
MinVersion=10.0

; Privilege settings - requires admin for Program Files installation
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Uninstaller settings
Uninstallable=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

; Architecture - 64-bit only
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Miscellaneous
DisableProgramGroupPage=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
SetupLogging=yes

; Show "Setup has finished installing..." message
ShowLanguageDialog=auto

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application files from staging folder
Source: "staging\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Note: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
; Start Menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; Desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; Quick Launch shortcut (optional, for older Windows versions)
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Registry]
; Optional: Add application to "Open with" context menu for associated file types
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; Add to App Paths for easier command-line access
Root: HKA; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"

[Run]
; Option to launch application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Check if .NET 8 Runtime is installed
function IsDotNetRuntimeInstalled: Boolean;
var
  ResultCode: Integer;
begin
  // Check for .NET 8 Desktop Runtime (required for WPF apps)
  Result := Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
  if Result then
  begin
    // The app is self-contained, so we don't strictly need .NET runtime
    // But we check anyway for informational purposes
    Result := True;
  end;
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  // Self-contained deployment - no runtime check needed
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Post-installation tasks can be added here
    Log('FathomOS installation completed successfully.');
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = wpSelectDir then
  begin
    // Validate installation directory if needed
    Log('Installation directory selected: ' + WizardDirValue);
  end;
end;

[UninstallDelete]
; Clean up any additional files/folders created during runtime
Type: filesandordirs; Name: "{app}\Logs"
Type: filesandordirs; Name: "{app}\Cache"
Type: filesandordirs; Name: "{localappdata}\{#MyAppName}"
