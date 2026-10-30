#define ProductName "Преобразователь кода для Ozon"
#define ProductVersion "1.2.9"
#define ExeName "OzonMarkingConverter.exe"
#ifdef TestMode
  #define DesktopRoot "{app}\_test_shortcuts\Desktop"
  #define StartRoot "{app}\_test_shortcuts\StartMenu"
#else
  #define DesktopRoot "{userdesktop}"
  #define StartRoot "{group}"
#endif

[Setup]
#ifdef TestMode
AppId=PEI.OzonMarkingConverter.SetupTest
OutputBaseFilename=OzonMarkingConverter-Setup-Test
UsePreviousAppDir=no
#else
AppId={{A49E4A30-EB42-44C1-9E36-B9E21D525A75}
OutputBaseFilename=OzonMarkingConverter-Setup-{#ProductVersion}
UsePreviousAppDir=yes
#endif
AppName={#ProductName}
AppVersion={#ProductVersion}
AppVerName={#ProductName} {#ProductVersion}
AppPublisher=by PEI
AppComments=by PEI
DefaultDirName={localappdata}\Programs\OzonMarkingConverter
DefaultGroupName={#ProductName}
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
WizardStyle=modern
SetupIconFile=scanner.ico
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#ProductName}
Uninstallable=yes
CreateUninstallRegKey=yes
#ifdef TestMode
AppMutex=Local\PEI.OzonMarkingConverter.InstallerTest
#else
AppMutex=Local\OzonMarkingDesktop-1
#endif
CloseApplications=yes
RestartApplications=no
OutputDir=..\..\outputs
Compression=lzma2
SolidCompression=yes
VersionInfoVersion=1.2.9.0
VersionInfoProductName={#ProductName}
VersionInfoCompany=by PEI
VersionInfoCopyright=by PEI
VersionInfoDescription=Установка: {#ProductName} — by PEI
ShowLanguageDialog=no

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
Source: "..\..\work\installer-payload\{#ExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\work\installer-payload\OzonMarkingConverter.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\work\installer-payload\scanner.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "README-installed.txt"; DestDir: "{app}"; DestName: "Инструкция.txt"; Flags: ignoreversion

[Icons]
Name: "{#DesktopRoot}\{#ProductName}"; Filename: "{app}\{#ExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#ExeName}"; Comment: "{#ProductName} — by PEI"; AppUserModelID: "PEI.OzonMarkingConverter"
Name: "{#StartRoot}\{#ProductName}"; Filename: "{app}\{#ExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#ExeName}"; Comment: "{#ProductName} — by PEI"; AppUserModelID: "PEI.OzonMarkingConverter"
Name: "{#StartRoot}\Инструкция"; Filename: "{app}\Инструкция.txt"

[Run]
Filename: "{app}\{#ExeName}"; Description: "Запустить программу"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#ExeName}"; Parameters: "--background"; Flags: nowait; Check: RestartAfterUpdate

[Tasks]
#ifndef TestMode
Name: "startup"; Description: "Запускать автоматически при входе в Windows"; Flags: checkedonce
#endif

[Registry]
#ifndef TestMode
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "OzonMarkingConverter"; ValueData: """{app}\{#ExeName}"" --background"; Tasks: startup
#endif

[Code]
function RestartAfterUpdate(): Boolean;
begin
#ifdef TestMode
  Result := False;
#else
  Result := ExpandConstant('{param:AUTORESTART|0}') = '1';
#endif
end;

function InitializeSetup(): Boolean;
begin
  Result := IsDotNetInstalled(net48, 0);
  if not Result then
    SuppressibleMsgBox('Для работы требуется .NET Framework 4.8 или новее в семействе 4.x. Установите этот компонент Windows и повторите установку программы.', mbCriticalError, MB_OK, IDOK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  CurrentRun: String;
begin
#ifndef TestMode
  if CurStep = ssPostInstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'OzonMarkingConverter', CurrentRun) then
      if Pos('ozonmarkingconverter.exe', Lowercase(CurrentRun)) > 0 then
        RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'OzonMarkingConverter', '"' + ExpandConstant('{app}\{#ExeName}') + '" --background');
#endif
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  CurrentRun: String;
  OwnCommand: String;
begin
#ifndef TestMode
  if CurUninstallStep = usUninstall then
  begin
    OwnCommand := '"' + ExpandConstant('{app}\{#ExeName}') + '"';
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'OzonMarkingConverter', CurrentRun) then
      if Pos(Lowercase(OwnCommand), Lowercase(CurrentRun)) = 1 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'OzonMarkingConverter');
  end;
#endif
end;
