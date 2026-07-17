; Script do Inno Setup para o instalador Windows do Cofre de Senhas.
; Compilar com: iscc App\distribuicao\cofre-de-senhas.iss
; (ou use App\distribuicao\gerar-instalador.ps1, que publica e compila em um passo só)
;
; Espera encontrar o binário já publicado em ..\..\publish (veja o README,
; seção "Geração do executável", ou rode o gerar-instalador.ps1).

#define MyAppName "Cofre de Senhas"
#ifndef MyAppVersion
  #define MyAppVersion "2.0.0"
#endif
#define MyAppPublisher "dcCarreto"
#define MyAppURL "https://github.com/dcCarreto/CofreDeSenhas"
#define MyAppExeName "CofreDeSenhas.exe"
#define MyAppId "{{B4E1F5A0-6C3D-4E8A-9F2B-7D3C1A5E8B90}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog
LicenseFile=..\..\LICENSE
OutputDir=..\..\dist
OutputBaseFilename=CofreDeSenhas-Setup-{#MyAppVersion}
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
SourceDir=.

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "..\..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Executar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  PastaCofre: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    PastaCofre := ExpandConstant('{userappdata}\GerenciadorSenhas');
    if DirExists(PastaCofre) and not UninstallSilent() then
    begin
      if MsgBox('Deseja também excluir os dados do seu cofre (senhas salvas)?' + #13#10 + #13#10 +
                'Isso apagará permanentemente a pasta:' + #13#10 + PastaCofre + #13#10 + #13#10 +
                'Esta ação não pode ser desfeita. Se preferir manter o cofre para reinstalar ' +
                'o programa depois, escolha "Não".',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(PastaCofre, True, True, True);
      end;
    end;
  end;
end;
