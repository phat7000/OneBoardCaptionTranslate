#define MyAppName "OneBoard Capture Translate"
#define MyAppVersion "1.1.1"
#define MyAppPublisher "OneBoard"
#define MyAppURL "https://oneboard.io.vn/"
#define MyAppExeName "OneBoardCaptureTranslate.exe"
#define MyArtifactName "OneBoardCaptureTranslate-Setup-1.1.1-win-x64"
#define MyPayloadDir "..\release\OneBoardCaptureTranslate-1.1.1-win-x64"

[Setup]
AppId={{B716520A-EAED-43AD-BCFF-2BD74429FB9D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\release
OutputBaseFilename={#MyArtifactName}
SetupIconFile=..\src\LiveCaptions-Translator.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
LicenseFile=..\LICENSE
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=1.1.1.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion=1.1.1.0
CreateUninstallRegKey=yes
Uninstallable=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#MyPayloadDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyPayloadDir}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyPayloadDir}\UPSTREAM_ATTRIBUTION.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyPayloadDir}\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyPayloadDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyPayloadDir}\README_zh-CN.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyPayloadDir}\RELEASE_NOTES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
