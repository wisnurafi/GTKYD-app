; Inno Setup script for Get To Know Your Device
; Builds a Windows installer from the self-contained x64 publish output.
; Compile with: ISCC.exe GetToKnowYourDevice.iss

#define MyAppName "Get To Know Your Device"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Wisnu"
#define MyAppExeName "GetToKnowYourDevice.exe"

; Path to the dotnet publish output (relative to this script).
#define PublishDir "..\src\GetToKnowYourDevice.App\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish"
#define IconFile "..\src\GetToKnowYourDevice.App\Assets\app.ico"

[Setup]
AppId={{B7E9C1A4-3F2D-4E8B-9A6C-1D5F7E0B2C3A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile={#IconFile}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Self-contained x64 build: only run on 64-bit Windows.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Windows 10 1809 (build 17763) minimum, matching TargetPlatformMinVersion.
MinVersion=10.0.17763
OutputDir=output
OutputBaseFilename=GetToKnowYourDevice-Setup-{#MyAppVersion}-x64
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Recurse the entire publish folder (app + .NET runtime + WinAppSDK runtime).
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
