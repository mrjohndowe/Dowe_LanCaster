#ifndef MyAppVersion
  #define MyAppVersion "0.9.5.23"
#endif

#define MyAppName "Dowe LanCaster"
#define MyAppPublisher "Dowe LanCaster"
#define MyAppExeName "DoweLanCaster.exe"

[Setup]
AppId={{B284D87A-AD06-4B03-B29F-54732455A03C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Dowe LanCaster
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\release-assets
OutputBaseFilename=Dowe-LanCaster-v{#MyAppVersion}-Setup
SetupIconFile=..\DoweLanCaster.ico
LicenseFile=..\LICENSE.txt
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=120
WizardImageFile=assets\installer-sidebar.bmp
WizardSmallImageFile=assets\installer-small.bmp
WizardImageStretch=yes
WizardImageBackColor=#01070e
WizardSmallImageBackColor=#10151c
DisableWelcomePage=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=10.0
CloseApplications=yes
RestartApplications=yes
VersionInfoVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} Windows Installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\release-build\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall

[Code]
var
  DoweProgressTrack: TPanel;
  DoweProgressFill: TPanel;

const
  DoweBackground = $0026201C;
  DoweSurface = $00201B17;
  DoweText = $00FCF8F4;
  DoweSecondaryText = $00A8998B;
  DoweBorder = $0051483B;
  DoweCyan = $00F0C725;

procedure ApplyDoweInstallerTheme;
begin
  WizardForm.Caption := 'Dowe LanCaster Setup';
  WizardForm.Color := DoweBackground;
  WizardForm.Font.Name := 'Segoe UI';
  WizardForm.Font.Color := DoweText;

  WizardForm.MainPanel.Color := DoweBackground;
  WizardForm.InnerPage.Color := DoweBackground;
  WizardForm.WelcomePage.Color := DoweBackground;
  WizardForm.FinishedPage.Color := DoweBackground;

  WizardForm.PageNameLabel.Font.Name := 'Segoe UI Semibold';
  WizardForm.PageNameLabel.Font.Color := DoweText;
  WizardForm.PageDescriptionLabel.Font.Color := DoweSecondaryText;
  WizardForm.WelcomeLabel1.Font.Name := 'Segoe UI Semibold';
  WizardForm.WelcomeLabel1.Font.Color := DoweText;
  WizardForm.WelcomeLabel2.Font.Color := DoweSecondaryText;
  WizardForm.FinishedHeadingLabel.Font.Name := 'Segoe UI Semibold';
  WizardForm.FinishedHeadingLabel.Font.Color := DoweText;
  WizardForm.FinishedLabel.Font.Color := DoweSecondaryText;
  WizardForm.StatusLabel.Font.Name := 'Segoe UI Semibold';
  WizardForm.StatusLabel.Font.Color := DoweCyan;
  WizardForm.FilenameLabel.Font.Color := DoweSecondaryText;

  { The Ready page memo retains the native light window background unless it
    is styled explicitly. The form-wide white foreground then becomes
    unreadable in that control. }
  WizardForm.ReadyMemo.Color := DoweSurface;
  WizardForm.ReadyMemo.Font.Color := DoweText;

  DoweProgressTrack := TPanel.Create(WizardForm);
  DoweProgressTrack.Parent := WizardForm.InstallingPage;
  DoweProgressTrack.Left := WizardForm.ProgressGauge.Left;
  DoweProgressTrack.Top := WizardForm.ProgressGauge.Top;
  DoweProgressTrack.Width := WizardForm.ProgressGauge.Width;
  DoweProgressTrack.Height := ScaleY(12);
  DoweProgressTrack.Color := DoweBorder;
  DoweProgressTrack.BevelOuter := bvNone;

  DoweProgressFill := TPanel.Create(WizardForm);
  DoweProgressFill.Parent := DoweProgressTrack;
  DoweProgressFill.Left := 0;
  DoweProgressFill.Top := 0;
  DoweProgressFill.Width := 0;
  DoweProgressFill.Height := DoweProgressTrack.ClientHeight;
  DoweProgressFill.Color := DoweCyan;
  DoweProgressFill.BevelOuter := bvNone;

  WizardForm.ProgressGauge.Visible := False;

  WizardForm.NextButton.Font.Name := 'Segoe UI Semibold';
  WizardForm.BackButton.Font.Name := 'Segoe UI';
  WizardForm.CancelButton.Font.Name := 'Segoe UI';
end;

procedure InitializeWizard;
begin
  ApplyDoweInstallerTheme;
end;

procedure CurInstallProgressChanged(CurProgress, MaxProgress: Integer);
var
  PercentComplete: Integer;
begin
  if MaxProgress > 0 then
  begin
    PercentComplete :=
      (Int64(CurProgress) * 100) div MaxProgress;
    DoweProgressFill.Width :=
      (Int64(CurProgress) * DoweProgressTrack.ClientWidth) div MaxProgress;
    WizardForm.StatusLabel.Caption :=
      Format('Installing Dowe LanCaster: %d%%', [PercentComplete]);
  end;
end;
