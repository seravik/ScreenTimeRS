@echo off
setlocal
cd /d "%~dp0"

echo [1/2] Building ScreenTime RS v0.1.0...
cargo build --release
if errorlevel 1 (
  echo Rust build failed.
  pause
  exit /b 1
)

where iscc >nul 2>nul
if errorlevel 1 (
  echo.
  echo Inno Setup compiler was not found.
  echo Install Inno Setup, then run this file again.
  echo.
  echo Expected compiler: ISCC.exe
  pause
  exit /b 2
)

echo [2/2] Building Windows installer...
if not exist dist mkdir dist
iscc installer\ScreenTimeRS-v0.1.0.iss
if errorlevel 1 (
  echo Installer build failed.
  pause
  exit /b 1
)

echo.
echo ========================================
echo Installer build complete!
echo dist\ScreenTimeRS-v0.1.0-Setup.exe
echo ========================================
echo.
pause
