@echo off
rem This script will remove all experimental hives of all
rem Visual Studio configurations to to reset everything
rem for debugging of VSIX projects 

set VS_PATH=Microsoft\VisualStudio

cd %LOCALAPPDATA%\%VS_PATH%
if not %ERRORLEVEL%%==0 (
  echo could not change directory to local app data
  timeout /T 10
  exit 1
)

echo removing all experimental hives from local app data
for /f %%G in ('dir /b "*Exp"') do (
  echo found experimental config:%%G
  rmdir /S /Q %%G
)

cd %APPDATA%\%VS_PATH%
if not %ERRORLEVEL%%==0 (
  echo could not change directory to app data
  timeout /T 10
  exit 1
)

echo removing all experimental hives from app data
for /f %%G in ('dir /b "*Exp"') do (
  echo found experimental config:%%G
  rmdir /S /Q %%G
)

pause
