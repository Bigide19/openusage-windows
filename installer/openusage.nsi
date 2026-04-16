; OpenUsage Windows installer — NSIS script
;
; Invoked by the `windows-installer.yml` GitHub Actions workflow with:
;   makensis /DVERSION=<ver> openusage.nsi
;
; Expected inputs (relative to this file):
;   ../dist/payload/                  <- dotnet publish output of OpenUsage.App
;   ../dist/payload/backend/          <- Rust headless binary + bundled plugins
;   ../wpf/src/OpenUsage.App/Assets/  <- installer icon
;   ../LICENSE                        <- license shown in the installer UI
;
; Output: OpenUsage-Setup-<VERSION>.exe next to this script.

!include "MUI2.nsh"

!ifndef VERSION
  !define VERSION "0.0.0-dev"
!endif

!define APPNAME   "OpenUsage"
!define COMPANY   "Bigide19"
!define APPEXE    "OpenUsage.exe"
!define BACKEND   "openusage.exe"
!define UKEY      "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"

; ---------------------------------------------------------------------------
; General setup
; ---------------------------------------------------------------------------
Name "${APPNAME}"
OutFile "OpenUsage-Setup-${VERSION}.exe"
; Per-user install — no admin prompt.
InstallDir "$LOCALAPPDATA\${APPNAME}"
InstallDirRegKey HKCU "${UKEY}" "InstallLocation"
RequestExecutionLevel user
SetCompressor /SOLID lzma
Unicode true
ShowInstDetails show
ShowUninstDetails show

VIProductVersion "0.0.0.0"
VIAddVersionKey "ProductName" "${APPNAME}"
VIAddVersionKey "CompanyName" "${COMPANY}"
VIAddVersionKey "FileDescription" "${APPNAME} installer"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "ProductVersion" "${VERSION}"

; ---------------------------------------------------------------------------
; UI
; ---------------------------------------------------------------------------
!define MUI_ICON   "..\wpf\src\OpenUsage.App\Assets\tray-icon.ico"
!define MUI_UNICON "..\wpf\src\OpenUsage.App\Assets\tray-icon.ico"
!define MUI_ABORTWARNING

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APPEXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Launch ${APPNAME}"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; ---------------------------------------------------------------------------
; Install
; ---------------------------------------------------------------------------
Section "Install"
  SetOutPath "$INSTDIR"

  ; File /r bundles at build time — if the payload is missing, makensis
  ; itself will fail, so no runtime check is needed.
  File /r "..\dist\payload\*.*"

  ; Start Menu shortcut
  CreateShortCut "$SMPROGRAMS\${APPNAME}.lnk" "$INSTDIR\${APPEXE}" "" "$INSTDIR\${APPEXE}" 0

  ; Add/Remove Programs entry (per-user)
  WriteRegStr   HKCU "${UKEY}" "DisplayName"     "${APPNAME}"
  WriteRegStr   HKCU "${UKEY}" "DisplayVersion"  "${VERSION}"
  WriteRegStr   HKCU "${UKEY}" "Publisher"       "${COMPANY}"
  WriteRegStr   HKCU "${UKEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr   HKCU "${UKEY}" "DisplayIcon"     "$INSTDIR\${APPEXE}"
  WriteRegStr   HKCU "${UKEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "${UKEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UKEY}" "NoRepair" 1

  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

; ---------------------------------------------------------------------------
; Uninstall
; ---------------------------------------------------------------------------
Section "Uninstall"
  ; Stop running app + its Rust sidecar (best effort; ignore failures).
  nsExec::Exec 'taskkill /F /IM "${APPEXE}"'
  nsExec::Exec 'taskkill /F /IM "${BACKEND}"'
  Sleep 400

  Delete "$SMPROGRAMS\${APPNAME}.lnk"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKCU "${UKEY}"
SectionEnd
