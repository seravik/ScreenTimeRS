@echo off
setlocal
cd /d "%~dp0"
call build-windows.bat
if errorlevel 1 exit /b 1
where iscc >nul 2>nul
if errorlevel 1 (
  echo Inno Setup compiler was not found.
  echo Install Inno Setup, then run this file again.
  exit /b 2
)
if not exist dist mkdir dist
iscc installer\ScreenTimeRS-v0.2.0.iss
if errorlevel 1 exit /b 1
echo.
echo ========================================
echo ScreenTime RS v0.2.0 installer complete!
echo dist\ScreenTimeRS-v0.2.0-Setup.exe
echo ========================================
