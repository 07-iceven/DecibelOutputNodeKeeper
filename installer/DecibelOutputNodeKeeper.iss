#ifndef MyAppName
#define MyAppName "Decibel Output Node Keeper"
#endif

#ifndef MyAppExeName
#define MyAppExeName "DecibelOutputNodeKeeper.exe"
#endif

#ifndef MyAppVersion
#define MyAppVersion "0.3"
#endif

#ifndef MyAppPublishDir
#define MyAppPublishDir "..\publish\win-x64"
#endif

[Setup]
AppId={{4C17D9D4-0B11-4D7A-9F3B-6BB0E7193C48}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppName}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=..\logo.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
OutputDir=..\dist\installer
OutputBaseFilename=DecibelOutputNodeKeeper-Setup-v{#MyAppVersion}
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent
