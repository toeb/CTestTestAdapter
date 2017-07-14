cmake_minimum_required(VERSION 3.8 FATAL_ERROR)
project(CTestAdapter CSharp)

# verify we are using Visual Studio 2012 or later
string(REGEX MATCH "^Visual Studio ([0-9]+) ([0-9]+)$" 
  checkVar "${CMAKE_GENERATOR}")
if(NOT checkVar)
  message(FATAL_ERROR "Visual Studio generator required.")
endif()
set(VS_VERSION "${CMAKE_MATCH_1}")
if(NOT ${VS_VERSION} GREATER 10)
  message(FATAL_ERROR "Visual Studio 2012 or later required")
endif()

# reduce configuration types to Release and Debug only
set(CMAKE_CONFIGURATION_TYPES "Release;Debug" CACHE INTERNAL "")

# TODO: change depending on VS version?
set(FW_VS11 "4.5")
set(FW_VS12 "4.5")
set(FW_VS14 "4.5")
set(FW_VS15 "4.6")
set(DOTNET_FRAMEWORK_VERSION "v${FW_VS${VS_VERSION}}")

set(MSBUILD_VERSION "${VS_VERSION}.0")

string(REPLACE "devenv.com" "devenv.exe" 
  DEVENV_EXE "${CMAKE_VS_DEVENV_COMMAND}")

include(VersionCompute.cmake)

set_property(DIRECTORY PROPERTY VS_STARTUP_PROJECT CTestAdapter)

add_subdirectory(CTestAdapter)
add_subdirectory(CTestAdapterSample)
