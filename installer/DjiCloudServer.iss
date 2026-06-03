; ============================================================
;  DJI Cloud Server — Inno Setup Installer Script
;  Compilar con:  ISCC.exe DjiCloudServer.iss
;  Prerequisito:  ejecutar publish.ps1 antes de compilar esto
; ============================================================

#define MyAppName      "DJI Cloud Server"
#define MyAppVersion   "1.0.0"
#define MyAppPublisher "USBA Sotomayor"
#define MyServiceName  "DjiCloudServer"
#define MyInputApp     "input\app"
#define MyInputTools   "input\tools"
#define MyHttpPort     "5072"
#define MyMqttPort     "1883"

[Setup]
; Cambia este GUID si creas un fork o producto distinto
AppId={{C4F2A7B8-3E91-4D05-B62A-0F8E1D93C7A0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=DjiCloudServerSetup-v{#MyAppVersion}
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\DjiCloudServer.exe
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} Setup

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

; ─── Archivos ────────────────────────────────────────────────────────────────
[Files]
; Aplicación publicada (self-contained, incluye .NET runtime)
Source: "{#MyInputApp}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; ffmpeg bundled
Source: "{#MyInputTools}\ffmpeg.exe"; DestDir: "{app}\tools"; Flags: ignoreversion

; ─── Menú inicio ─────────────────────────────────────────────────────────────
[Icons]
Name: "{group}\Abrir Panel de Control"; \
      Filename: "{sys}\cmd.exe"; \
      Parameters: "/c start http://localhost:{#MyHttpPort}/map.html"; \
      Comment: "Abre el panel de monitorización en el navegador"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; ─── Pascal script (lógica avanzada) ─────────────────────────────────────────
[Code]

{ Detiene el servicio antes de actualizar archivos }
procedure StopServiceIfRunning();
var ResultCode: Integer;
begin
  Exec('sc.exe', 'stop {#MyServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(2000);
end;

{ Arranca el servicio después de instalar }
procedure StartService();
var ResultCode: Integer;
begin
  Exec('sc.exe', 'start {#MyServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  AppPath: String;
begin
  if CurStep = ssInstall then
    StopServiceIfRunning();

  if CurStep = ssPostInstall then
  begin
    AppPath := ExpandConstant('{app}\DjiCloudServer.exe');

    { Registrar servicio de Windows }
    Exec('sc.exe',
      'create "{#MyServiceName}"' +
      ' binPath= "' + AppPath + '"' +
      ' start= auto' +
      ' DisplayName= "{#MyAppName}"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    Exec('sc.exe',
      'description "{#MyServiceName}"' +
      ' "DJI Cloud API Server — MQTT:{#MyMqttPort}, HTTP:{#MyHttpPort}"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    { Configurar reinicio automático en fallo (3 intentos) }
    Exec('sc.exe',
      'failure "{#MyServiceName}" reset= 86400 actions= restart/5000/restart/10000/restart/30000',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    { Reglas de firewall }
    Exec('netsh.exe',
      'advfirewall firewall add rule' +
      ' name="DJI Cloud Server HTTP {#MyHttpPort}"' +
      ' dir=in action=allow protocol=TCP localport={#MyHttpPort}' +
      ' description="DJI Cloud Server panel web"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    Exec('netsh.exe',
      'advfirewall firewall add rule' +
      ' name="DJI Cloud Server MQTT {#MyMqttPort}"' +
      ' dir=in action=allow protocol=TCP localport={#MyMqttPort}' +
      ' description="DJI Cloud Server broker MQTT"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    { Iniciar el servicio }
    StartService();
  end;
end;

{ Desinstalación: parar y eliminar servicio y reglas de firewall }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    Exec('sc.exe', 'stop "{#MyServiceName}"',   '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);
    Exec('sc.exe', 'delete "{#MyServiceName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    Exec('netsh.exe',
      'advfirewall firewall delete rule name="DJI Cloud Server HTTP {#MyHttpPort}"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    Exec('netsh.exe',
      'advfirewall firewall delete rule name="DJI Cloud Server MQTT {#MyMqttPort}"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

{ Muestra la IP local al finalizar la instalación }
function GetLocalIp(Param: String): String;
begin
  Result := 'localhost';
end;

[CustomMessages]
spanish.FinishedLabel=La instalación ha finalizado.%n%nEl servicio %1 se ha registrado e iniciado.%n%nAccede al panel desde cualquier equipo de la red:%n  http://<IP-del-servidor>:{#MyHttpPort}/map.html%n%nPuerto MQTT: {#MyMqttPort}

[Messages]
spanish.FinishedHeadingLabel=Instalación completada
