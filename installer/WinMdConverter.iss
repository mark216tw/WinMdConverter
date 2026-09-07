#ifndef MyAppVersion
  #define MyAppVersion "0.2.0"
#endif
#ifndef MyLanguageFile
  #define MyLanguageFile "compiler:Default.isl"
#endif

#define MyAppName "WinMdConverter"
#define MyAppPublisher "WinMdConverter"
#define MyAppExeName "WinMdConverter.exe"

[Setup]
AppId={{A799494A-ECA2-49A8-B847-26F1954D038A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts
OutputBaseFilename=WinMdConverter-Setup-{#MyAppVersion}-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\WinMdConverter.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ChangesEnvironment=yes
SetupLogging=yes

[Languages]
Name: "chinesetraditional"; MessagesFile: "{#MyLanguageFile}"

[Tasks]
Name: "desktopicon"; Description: "建立桌面捷徑"; GroupDescription: "其他工作："; Flags: unchecked
Name: "addtopath"; Description: "將 mdconvert CLI 加入使用者 PATH"; GroupDescription: "其他工作："; Flags: unchecked

[Files]
Source: "..\artifacts\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\WinMdConverter"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\WinMdConverter"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "啟動 WinMdConverter"; Flags: nowait postinstall skipifsilent

[Code]
const
  ProductRegistryKey = 'Software\WinMdConverter';

function NormalizePathEntry(Value: String): String;
begin
  Result := Lowercase(RemoveBackslashUnlessRoot(Trim(Value)));
end;

function PathContains(PathValue: String; Entry: String): Boolean;
var
  Remaining: String;
  Part: String;
  SeparatorPosition: Integer;
begin
  Result := False;
  Remaining := PathValue;
  while Remaining <> '' do
  begin
    SeparatorPosition := Pos(';', Remaining);
    if SeparatorPosition = 0 then
    begin
      Part := Remaining;
      Remaining := '';
    end
    else
    begin
      Part := Copy(Remaining, 1, SeparatorPosition - 1);
      Delete(Remaining, 1, SeparatorPosition);
    end;

    if NormalizePathEntry(Part) = NormalizePathEntry(Entry) then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

function RemovePathEntry(PathValue: String; Entry: String): String;
var
  Remaining: String;
  Part: String;
  SeparatorPosition: Integer;
begin
  Result := '';
  Remaining := PathValue;
  while Remaining <> '' do
  begin
    SeparatorPosition := Pos(';', Remaining);
    if SeparatorPosition = 0 then
    begin
      Part := Trim(Remaining);
      Remaining := '';
    end
    else
    begin
      Part := Trim(Copy(Remaining, 1, SeparatorPosition - 1));
      Delete(Remaining, 1, SeparatorPosition);
    end;

    if (Part <> '') and (NormalizePathEntry(Part) <> NormalizePathEntry(Entry)) then
    begin
      if Result <> '' then
        Result := Result + ';';
      Result := Result + Part;
    end;
  end;
end;

procedure AddCliToPath;
var
  CurrentPath: String;
  AppPath: String;
begin
  AppPath := ExpandConstant('{app}');
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', CurrentPath) then
    CurrentPath := '';

  if not PathContains(CurrentPath, AppPath) then
  begin
    if CurrentPath = '' then
      CurrentPath := AppPath
    else
      CurrentPath := CurrentPath + ';' + AppPath;
    RegWriteExpandStringValue(HKCU, 'Environment', 'Path', CurrentPath);
    RegWriteDWordValue(HKCU, ProductRegistryKey, 'PathAdded', 1);
  end;
end;

procedure RemoveCliFromPath;
var
  CurrentPath: String;
  PathAdded: Cardinal;
begin
  if RegQueryDWordValue(HKCU, ProductRegistryKey, 'PathAdded', PathAdded) and (PathAdded = 1) then
  begin
    if RegQueryStringValue(HKCU, 'Environment', 'Path', CurrentPath) then
      RegWriteExpandStringValue(HKCU, 'Environment', 'Path', RemovePathEntry(CurrentPath, ExpandConstant('{app}')));
    RegDeleteValue(HKCU, ProductRegistryKey, 'PathAdded');
    RegDeleteKeyIfEmpty(HKCU, ProductRegistryKey);
  end;
end;

function IsEdgeInstalled: Boolean;
begin
  Result := FileExists(ExpandConstant('{pf32}\Microsoft\Edge\Application\msedge.exe')) or
            FileExists(ExpandConstant('{pf64}\Microsoft\Edge\Application\msedge.exe')) or
            FileExists(ExpandConstant('{localappdata}\Microsoft\Edge\Application\msedge.exe'));
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  if not IsEdgeInstalled then
    Result := MsgBox('未偵測到 Microsoft Edge。HTML 與 DOCX 轉換仍可使用，但 PDF 轉換需要 Edge。是否繼續安裝？',
      mbConfirmation, MB_YESNO) = IDYES;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('addtopath') then
    AddCliToPath;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RemoveCliFromPath;
end;
