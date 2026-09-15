@echo off
setlocal
cd /d "%~dp0"

echo Building ScreenTime RS v0.2.3...
call build-windows.bat
if errorlevel 1 exit /b 1

set "ISCC=C:\PROGRA~2\INNOSE~1\ISCC.exe"
if not exist "%ISCC%" (
  echo Inno Setup compiler was not found.
  echo Expected: %ISCC%
  exit /b 2
)

if exist installer-output rmdir /s /q installer-output
mkdir installer-output
"%ISCC%" installer\ScreenTimeRS-v0.2.3.iss
if errorlevel 1 exit /b 1

echo.
echo ========================================
echo ScreenTime RS v0.2.3 installer complete!
echo installer-output\ScreenTimeRS-v0.2.3-Setup.exe
echo ========================================
