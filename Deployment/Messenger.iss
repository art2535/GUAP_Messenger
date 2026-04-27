#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

#define MyAppName "GUAP Messenger"
#define MyAppPublisher "art2535"
#define MyAppExeNameWeb "Messenger.Web.exe"
#define MyAppExeNameAPI "Messenger.API.exe"

[Setup]
AppName={#MyAppName}
AppVersion={#AppVersion}
AppVerName={#MyAppName} {#AppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/art2535/GUAP_Messenger
AppSupportURL=https://github.com/art2535/GUAP_Messenger
AppUpdatesURL=https://github.com/art2535/GUAP_Messenger

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=Messenger-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

VersionInfoVersion={#AppVersion}
VersionInfoTextVersion={#AppVersion}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCompany={#MyAppPublisher}

SetupIconFile=favicon.ico
UninstallDisplayIcon={app}\Web\{#MyAppExeNameWeb}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Сервер и клиент (рекомендуется)"; Flags: iscustom
Name: "serveronly"; Description: "Только сервер"
Name: "clientonly"; Description: "Только клиент"

[Components]
Name: "server"; Description: "Сервер API"; Types: full serveronly
Name: "client"; Description: "Клиент Messenger.Web"; Types: full clientonly

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; Components: client

[Files]
Source: "..\publish\win-x64\Messenger.API\*"; DestDir: "{app}\API"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: server
Source: "..\publish\win-x64\Messenger.Web\*"; DestDir: "{app}\Web"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: client

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\Web\{#MyAppExeNameWeb}"; WorkingDir: "{app}\Web"; Components: client
Name: "{group}\API (Сервер)"; Filename: "{app}\API\{#MyAppExeNameAPI}"; WorkingDir: "{app}\API"; Flags: preventpinning; Components: server
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\Web\{#MyAppExeNameWeb}"; WorkingDir: "{app}\Web"; Tasks: desktopicon; Components: client

[Run]
Filename: "{cmd}"; Parameters: "/c taskkill /f /im {#MyAppExeNameAPI} /t /fi ""status eq running"""; Flags: runhidden; StatusMsg: "Очистка портов API..."
Filename: "{cmd}"; Parameters: "/c taskkill /f /im {#MyAppExeNameWeb} /t /fi ""status eq running"""; Flags: runhidden; StatusMsg: "Очистка портов Web..."

Filename: "{app}\API\{#MyAppExeNameAPI}"; \
    Parameters: "--urls ""https://localhost:7001"" --environment Development"; \
    WorkingDir: "{app}\API"; \
    Flags: nowait runhidden; \
    Description: "Запуск сервера API"; \
    Components: server

Filename: "{app}\Web\{#MyAppExeNameWeb}"; \
    Parameters: "--urls ""https://localhost:7010"" --environment Development"; \
    WorkingDir: "{app}\Web"; \
    Flags: nowait runhidden; \
    Description: "Запуск Web-интерфейса"; \
    Components: client

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c taskkill /f /im {#MyAppExeNameAPI} /t"; Flags: runhidden
Filename: "{cmd}"; Parameters: "/c taskkill /f /im {#MyAppExeNameWeb} /t"; Flags: runhidden