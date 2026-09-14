@echo off
setlocal
echo Building ScreenTime RS v0.1.0...
cargo build --release
if errorlevel 1 exit /b 1
if not exist dist mkdir dist
copy /Y target\release\screentime-rs.exe dist\ScreenTimeRS-v0.1.0.exe >nul
echo.
echo Output: dist\ScreenTimeRS-v0.1.0.exe
