@echo off
setlocal
cd /d "%~dp0"
echo Building ScreenTime RS v0.5.0...
cargo build --release
if errorlevel 1 exit /b 1
if exist dist\ui rmdir /s /q dist\ui
mkdir dist\ui
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK is required for the WinUI 3 interface.
  exit /b 2
)
dotnet publish ui\ScreenTimeRS.UI.csproj -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -o dist\ui
if errorlevel 1 exit /b 1
copy /Y target\release\screentime-rs.exe dist\ui\screentime-rs.exe >nul
echo.
echo Output: dist\ui\ScreenTimeRS.UI.exe
echo.
