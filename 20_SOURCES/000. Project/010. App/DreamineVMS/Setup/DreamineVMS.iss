#define MyAppName "Dreamine VMS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Codemaru"
#define MyAppExeName "DreamineVMS.exe"
#define MyAppURL "https://codemaru.co.kr"
#define SourceDir "..\bin\Release\Publish"
#define IconDir "..\wwwroot\images"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={localappdata}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=DreamineVMS-Setup-v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayName={#MyAppName}
SetupIconFile={#IconDir}\setup.ico
WizardImageFile={#IconDir}\setup.png
WizardSmallImageFile={#IconDir}\Icon.png
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕화면에 바로가기 만들기"; GroupDescription: "추가 아이콘:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppName} 제거"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "설치 완료 후 바로 실행"; Flags: nowait postinstall skipifsilent
