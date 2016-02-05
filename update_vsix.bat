@echo off

Setlocal EnableDelayedExpansion

:: get visual studio version
set MSVC_VERSION_PROMPT=true
set MSVC_VERSION_DEFAULT=12
if %MSVC_VERSION_PROMPT%==true (
SET /p MSVC_VERSION=Visual Studio version ^(10/11/12/14^) [%MSVC_VERSION_DEFAULT%]:
)
IF "%MSVC_VERSION%"=="" SET MSVC_VERSION=%MSVC_VERSION_DEFAULT%

echo updating vsix for visual studio %MSVC_VERSION%

:: initialize command line
set TOOLS_VAR=VS%MSVC_VERSION%0COMNTOOLS
@call "!%VS120COMNTOOLS%!\vsvars32.bat"

echo removing ctest test adapter ...
VsixInstaller /q /u:CTestTestAdapter..7589ccbd-c148-4981-b13a-f61a2643f1ee
echo removing ctest test adapter ... done

echo installing ctest test adapter ...
VsixInstaller /q "CTestTestAdapter.vsix"
echo installing ctest test adapter ... done

::pause
