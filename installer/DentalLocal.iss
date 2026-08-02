; Инсталлятор DentalLocal (Inno Setup) — раздел 13 ТЗ / критерий приёмки §21.8.
;
; Собирается ПОСЛЕ публикации приложения (см. installer/build.ps1), а не напрямую из исходников:
; ожидает готовые publish-каталоги installer\build\server и installer\build\launcher.
;
; Использование:
;   1. powershell -File installer\build.ps1            (публикует Server+Client+Launcher, self-contained win-x64)
;   2. ISCC installer\DentalLocal.iss /DAppVersion=1.0.0
;
; Требует Inno Setup 6+ (https://jrsoftware.org/isinfo.php).

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName "DentalLocal"
#define AppPublisher "DentalLocal"
#define AppExeName "Launcher\DentalLocal.Launcher.exe"
#define BuildDir "build"
#define RepoRoot "..\"

[Setup]
; Фиксированный GUID — не менять между версиями, иначе Windows считает это другим приложением.
AppId={{8F1E7B3A-6C4D-4E2A-9C0B-2B7B4E7A1D10}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=DentalLocal-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#AppExeName}
; TZ п.13.7: регистрация в "Установка и удаление программ" — обеспечивается автоматически Inno Setup.

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Server (self-contained publish, включает смёрженный wwwroot Blazor-клиента — см. build.ps1) — плоско в {app}.
Source: "{#BuildDir}\server\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

; Launcher (self-contained publish) — в подпапку {app}\Launcher.
Source: "{#BuildDir}\launcher\*"; DestDir: "{app}\Launcher"; Flags: recursesubdirs ignoreversion

; Портативный PostgreSQL — кладётся вручную в Database\pgsql перед сборкой инсталлятора (см. installer/README.md).
; skipifsourcedoesntexist — чтобы сборка инсталлятора не падала, если разработчик ещё не положил бинарники.
Source: "{#RepoRoot}Database\pgsql\*"; DestDir: "{app}\Database\pgsql"; Flags: recursesubdirs ignoreversion skipifsourcedoesntexist external

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}\Launcher"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}\Launcher"; Tasks: desktopicon

[Run]
; TZ п.13: один клик по ярлыку сразу после установки — запуск Launcher'а, который сам поднимает backend и БД.
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Логи и временные файлы удаляются всегда — они не являются данными пациентов.
Type: filesandordirs; Name: "{app}\Logs"

[Code]
var
  DeletePatientData: Boolean;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResponseCode: Integer;
begin
  // TZ п.13.7: деинсталлятор спрашивает про данные пациентов отдельным чекбоксом, по умолчанию — НЕ удалять.
  if CurUninstallStep = usUninstall then
  begin
    ResponseCode := MsgBox(
      'Удалить также данные пациентов (базу данных, вложения, резервные копии)?' + #13#10 +
      'Если выбрать «Нет», папки Database\data, Data и Backups останутся на диске.',
      mbConfirmation, MB_YESNO or MB_DEFBUTTON2);
    DeletePatientData := (ResponseCode = IDYES);
  end;

  if (CurUninstallStep = usPostUninstall) and DeletePatientData then
  begin
    DelTree(ExpandConstant('{app}\Database\data'), True, True, True);
    DelTree(ExpandConstant('{app}\Data'), True, True, True);
    DelTree(ExpandConstant('{app}\Backups'), True, True, True);
  end;
end;
