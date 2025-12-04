; Inno Setup Script for Pad-Avan Force
; Install Inno Setup from: https://jrsoftware.org/isdl.php

[Setup]
AppId={{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}
AppName=Pad-Avan Force
AppVersion=1.0.0
AppPublisher=Your Name
AppPublisherURL=https://github.com/Ghost3379/Pad-Avan
DefaultDirName={autopf}\Pad-Avan Force
DefaultGroupName=Pad-Avan Force
OutputDir=installer
OutputBaseFilename=PadAvanForce-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
SetupIconFile=Pad-Avan Force\Assets\Pad-Avan Force logo.ico
UninstallDisplayIcon={app}\Pad-Avan Force.exe
PrivilegesRequired=admin
; Allow updates - if same AppId is detected, it will update instead of installing fresh
AllowNoIcons=yes
DisableProgramGroupPage=no
DisableReadyPage=no
VersionInfoVersion=1.0.0.0
VersionInfoCompany=Your Name
VersionInfoDescription=Pad-Avan Force - Macro Pad Configuration Tool
VersionInfoCopyright=Copyright (C) 2025

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"
Name: "quicklaunchicon"; Description: "Create a quick launch icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; Include all files from the published output directory
; NOTE: This ONLY includes the Avalonia app files from dotnet publish
; The FeatherS3 scripts folder and other repository files are NOT included
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Pad-Avan Force"; Filename: "{app}\Pad-Avan Force.exe"
Name: "{group}\Uninstall Pad-Avan Force"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Pad-Avan Force"; Filename: "{app}\Pad-Avan Force.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\Pad-Avan Force"; Filename: "{app}\Pad-Avan Force.exe"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\Pad-Avan Force.exe"; Description: "Launch Pad-Avan Force"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Check if .NET 8.0 Runtime is installed (optional check)
  // If you publish as self-contained, this check is not needed
end;

