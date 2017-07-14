@echo off
setlocal enabledelayedexpansion

if not defined CTEST_ADAPTER_CMAKE (
  for %%X in (cmake.exe) do (
    set CTEST_ADAPTER_CMAKE=%%~$PATH:X
  )
)
if not exist "%CTEST_ADAPTER_CMAKE%" (
  echo =========================================================
  echo =               error finding cmake                     =
  echo =========================================================
  echo = No CMake executable found anywhere in PATH, and       =
  echo = CTEST_ADAPTER_CMAKE is not set to a CMake executable. =
  echo = Please make sure you have CMake 3.8 or later in your  =
  echo = PATH or set the CTEST_ADAPTER_CMAKE environment       =
  echo = variable to point to a valid CMake executable.        =
  echo =========================================================
  pause
  exit 1
)
echo ===============================================================
echo == using cmake from: 
echo == %CTEST_ADAPTER_CMAKE%
echo ===============================================================

if not defined VS_VERSION set VS_VERSION=15

set CMAKE_SOURCEDIR=%~dp0
set CMAKE_BINARYDIR=%~dp0vs%VS_VERSION%

rem remove build directory, remove these lines
rem if you don't want to delete the build directory
rem everytime when bootstrapping
if exist "%CMAKE_BINARYDIR%" (
  rmdir /S /Q  "%CMAKE_BINARYDIR%"
)
rem setup build directory
if not exist "%CMAKE_BINARYDIR%" (
  mkdir "%CMAKE_BINARYDIR%"
)
if not exist "%CMAKE_BINARYDIR%" (
  echo could not create binary directory "%CMAKE_BINARYDIR%"
  timeout /T 10
  exit 1
)

cd "%CMAKE_BINARYDIR%"
if not %ERRORLEVEL%%==0 (
  echo could not cd to binary directory "%CMAKE_BINARYDIR%"
  timeout /T 10
  exit 1
)

"%CTEST_ADAPTER_CMAKE%"^
  -G "Visual Studio %VS_VERSION%"^
  "%CMAKE_SOURCEDIR%"
if not %ERRORLEVEL%%==0 (
  echo error in cmake, skipping everything ...
  timeout /T 10
  exit 1
)
"%CTEST_ADAPTER_CMAKE%" .

::"%CTEST_ADAPTER_CMAKE%" --build . --config Release
if not %ERRORLEVEL%%==0 (
  echo error when building ...
  pause
)
