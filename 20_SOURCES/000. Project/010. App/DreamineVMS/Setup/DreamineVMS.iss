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

[Code]
function IsWebView2Installed(): Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version)
         or RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version)
         or RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version);
end;

procedure InstallWebView2();
var
  ResultCode: Integer;
  PSArgs: String;
begin
  PSArgs := '-NoProfile -NonInteractive -Command "' +
    '$out = Join-Path $env:TEMP ''MicrosoftEdgeWebview2Setup.exe''; ' +
    'Invoke-WebRequest ''https://go.microsoft.com/fwlink/p/?LinkId=2124703'' -OutFile $out; ' +
    'Start-Process $out -ArgumentList ''/silent /install'' -Wait"';
  Exec('powershell.exe', PSArgs, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not IsWebView2Installed() then
    begin
      WizardForm.StatusLabel.Caption := 'Microsoft WebView2 Runtime 설치 중...';
      InstallWebView2();
    end;
  end;
end;
