#include "CodeDependencies.iss"

; Nome dell'applicazione
#define MyAppName "Duplip"
; Preleva la versione dall'eseguibile
#define MyAppVersion GetFileVersion("..\bin\Release\Duplip.exe")
; Nome dell'autore
#define MyAppPublisher "Henrik Sozzi"
; URL di riferimento dell'applicazione
#define MyAppURL "http://www.henriksozzi.it/"
; Nome dell'eseguibile
#define MyAppExeName "Duplip.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{73D93893-C2BA-4C04-9612-B4CBB653108A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={autopf}\{#MyAppName}

OutputBaseFilename={#MyAppName} Setup {#MyAppVersion}
Compression=lzma
SolidCompression=yes
AppContact=io@henriksozzi.it
CreateUninstallRegKey=yes
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCopyright={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
MinVersion=6.1sp1
VersionInfoTextVersion={#MyAppVersion}
VersionInfoProductTextVersion={#MyAppVersion}
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed = dialog
ArchitecturesInstallIn64BitMode=x64
ChangesEnvironment=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "addtopath"; Description: "{cm:AddToPath}"; GroupDescription: "{cm:AdditionalIcons}"


[Files]
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "..\bin\Release\*"; DestDir: "{app}"; Excludes: "*.xml"; Flags: recursesubdirs ignoreversion

[Messages]
BeveledLabel={#MyAppPublisher}

[CustomMessages]
it.AddToPath=Aggiungi alla path di sistema
en.AddToPath=Add to system path
es.AddToPath=Agregar a la ruta del sistema

[Registry]
; USER PATH (sempre, se task selezionata)
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{code:GetUserPath|{app}}"; Tasks: addtopath; Check: IsTaskUserSelected('addtopath')

; SYSTEM PATH (solo admin)
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{code:GetSystemPath|{app}}"; Tasks: addtopath; Check: IsTaskSystemSelected('addtopath')

[Code]
// NPCAP detection
function IsNpcapInstalled(): Boolean;
var
  NpcapPath: String;
  ServiceKey: String;
begin
  // Controlla file NPFInstall.exe (metodo principale)
  NpcapPath := ExpandConstant('{pf}\Npcap\NPFInstall.exe');
  if FileExists(NpcapPath) then
  begin
    Result := True;
    Exit;
  end;
  
  // Fallback: controlla servizio npcap nel registry
  ServiceKey := 'SYSTEM\CurrentControlSet\Services\npcap';
  Result := RegKeyExists(HKEY_LOCAL_MACHINE, ServiceKey);
end;

procedure CheckNpcapAndWarn();
var
  ResultCode: Integer;
begin
  if not IsNpcapInstalled() then
  begin
    if MsgBox('Duplip richiede Npcap per funzionare.'#13#10#13#10 +
              'Npcap NON è installato sul sistema.'#13#10#13#10 +
              'Vuoi aprire la pagina di download di Npcap?', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://npcap.com/#download', '', '', SW_SHOW, ewNoWait, ResultCode);
    end;
  end;
end;
// End NPCAP detection

// Add Path
var
  IsAdminMode: Boolean;

// Controlla task per USER PATH
function IsTaskUserSelected(TaskName: String): Boolean;
begin
  Result := (Not IsAdminInstallMode()) and IsTaskSelected(TaskName);
end;

// Controlla task per SYSTEM PATH (solo admin)
function IsTaskSystemSelected(TaskName: String): Boolean;
begin
  Result := IsAdminInstallMode() and IsTaskSelected(TaskName);
end;

// PATH USER sicura (LEGGE esistente!)
function GetUserPath(NewPath: String): String;
var
  CurrentUserPath: String;
begin
  // Legge PATH utente corrente
  if RegQueryStringValue(HKEY_CURRENT_USER, 'Environment', 'Path', CurrentUserPath) then
  begin
    // Aggiunge solo se NON già presente
    if Pos(';' + NewPath + ';', ';' + CurrentUserPath + ';') = 0 then
      Result := CurrentUserPath + ';' + NewPath
    else
      Result := CurrentUserPath;  // Già presente
  end
  else
  begin
    // PATH utente vuota imposta questo valore
    Result := NewPath;
  end;
end;

// PATH SYSTEM sicura
function GetSystemPath(NewPath: String): String;
var
  CurrentSystemPath: String;
begin
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 
     'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', CurrentSystemPath) then
  begin
    if Pos(';' + NewPath + ';', ';' + CurrentSystemPath + ';') = 0 then
      Result := CurrentSystemPath + ';' + NewPath
    else
      Result := CurrentSystemPath;
  end
  else
    Result := ExpandConstant('%PATH%') + ';' + NewPath;
end;

procedure InitializeWizard;
begin
  IsAdminMode := IsAdminInstallMode();
  if (IsAdminMode) Then Log('Modalità installazione: ADMIN')
  Else Log('Modalità installazione: USER');
  CheckNpcapAndWarn();
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Notifica PATH
    if IsTaskSelected('addtopath') then
    begin
      if IsAdminMode then
        MsgBox('Duplip aggiunto alla SYSTEM PATH!'#13#10 +
               'Riavvia eventuali CMD/PowerShell per caricare nuova path.', mbInformation, MB_OK)
      else
        MsgBox('Duplip aggiunto alla tua USER PATH!'#13#10 +
               'Chiudi e riapri eventuali CMD/PowerShell per caricare nuova path.', mbInformation, MB_OK);
    end;
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
end;
// End add Path

// CodeDependencies
function InitializeSetup: Boolean;
begin
  // comment out functions to disable installing them
  //Dependency_AddDotNet35;
  //Dependency_AddDotNet40;
  //Dependency_AddDotNet45;
  //Dependency_AddDotNet46;
  //Dependency_AddDotNet47;
  Dependency_AddDotNet48;
  //Dependency_AddDotNet481;
  //Dependency_AddNetCore31; // EOL - .NET Core 3.1
  //Dependency_AddNetCore31Asp; // EOL - .NET Core 3.1
  //Dependency_AddNetCore31Desktop; // EOL - .NET Core 3.1
  //Dependency_AddDotNet50; // EOL - .NET 5.0
  //Dependency_AddDotNet50Asp; // EOL - .NET 5.0
  //Dependency_AddDotNet50Desktop; // EOL - .NET 5.0
  //Dependency_AddDotNet60; // EOL - .NET 6.0
  //Dependency_AddDotNet60Asp; // EOL - .NET 6.0
  //Dependency_AddDotNet60Desktop; // EOL - .NET 6.0
  //Dependency_AddDotNet70; // EOL - .NET 7.0
  //Dependency_AddDotNet70Asp; // EOL - .NET 7.0
  //Dependency_AddDotNet70Desktop; // EOL - .NET 7.0
  //Dependency_AddDotNet80;
  //Dependency_AddDotNet80Asp;
  //Dependency_AddDotNet80Desktop;
  //Dependency_AddDotNet90;
  //Dependency_AddDotNet90Asp;
  //Dependency_AddDotNet90Desktop;
  //Dependency_AddDotNet100;
  //Dependency_AddDotNet100Asp;
  //Dependency_AddDotNet100Desktop;

  //Dependency_AddVC2005;
  //Dependency_AddVC2008;
  //Dependency_AddVC2010;
  //Dependency_AddVC2012;
  //Dependency_ForceX86 := True; // force 32-bit install of next dependencies
  //Dependency_AddVC2013;
  //Dependency_ForceX86 := False; // disable forced 32-bit install again
  //Dependency_AddVC2015To2022;

  //Dependency_AddDirectX;

  //Dependency_AddSql2008Express;
  //Dependency_AddSql2012Express;
  //Dependency_AddSql2014Express;
  //Dependency_AddSql2016Express;
  //Dependency_AddSql2017Express;
  //Dependency_AddSql2019Express;
  //Dependency_AddSql2022Express;

  //Dependency_AddWebView2;

  //Dependency_AddAccessDatabaseEngine2010;
  //Dependency_AddAccessDatabaseEngine2016;

  Result := True;
end;
// End CodeDependencies