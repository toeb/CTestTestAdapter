
set(VSPACKAGE_SOURCES
  CTestAdapterPackge.cs
  CTestAdapterOptionPage.cs
  Key.snk
  VSPackage.resx
  Resources/CTestAdapterPackge.ico
  )

set(ADAPTER_SOURCES
  BuildConfiguration.cs
  CMakeCache.cs
  CTestContainer.cs
  CTestContainerDiscoverer.cs
  CTestContainerDiscovererBase.cs
  CTestDiscoverer.cs
  CTestExecutor.cs
  CTestInfo.cs
  CTestLogWindow.cs
  CTestTestCollector.cs
  DTEHelper.cs
  ProcessExtensions.cs
  Events/ChildProcessWatcher.cs
  Events/CTestTestfileAddRemoveListener.cs
  Events/ISolutionEventsListener.cs
  Events/ITestFileAddRemoveListener.cs
  Events/ITestFilesupdateWatcher.cs
  Events/ProjectItemAddRemoveListener.cs
  Events/SolutionEventListener.cs
  Events/SolutionEventsListenerEventArgs.cs
  Events/TestFileChangedEventArgs.cs
  Events/TestFilesWatcher.cs
  )

set(ADAPTER_CONTENT_FILES
  CMake_logo.png
  screenshot.png
  LICENSE.txt
  )

set(VSPACKAGE_REFERENCES
  EnvDTE
  EnvDTE80
  EnvDTE90
  EnvDTE100
  Microsoft.CSharp
  Microsoft.VisualStudio.CommandBars
  Microsoft.VisualStudio.CoreUtility
  Microsoft.VisualStudio.Imaging
  Microsoft.VisualStudio.OLE.Interop
  Microsoft.VisualStudio.Shell.${VS_VERSION}.0
  Microsoft.VisualStudio.Shell.Interop
  Microsoft.VisualStudio.Shell.Interop.8.0
  Microsoft.VisualStudio.Shell.Interop.9.0
  Microsoft.VisualStudio.Shell.Interop.10.0
  Microsoft.VisualStudio.Shell.Interop.11.0
  Microsoft.VisualStudio.Shell.Interop.12.0
  Microsoft.VisualStudio.TextManager.Interop
  Microsoft.VisualStudio.TextManager.Interop.8.0
  Microsoft.VisualStudio.Threading
  Microsoft.VisualStudio.Utilities
  Microsoft.VisualStudio.Validation
  stdole
  System
  System.Data
  System.Design
  System.Drawing
  System.Windows.Forms
  System.Xml
  )

if(${VS_VERSION} EQUAL 11)
elseif(${VS_VERSION} EQUAL 12)
elseif(${VS_VERSION} EQUAL 14)
elseif(${VS_VERSION} EQUAL 15)
  list(APPEND VSPACKAGE_REFERENCES
    Microsoft.VisualStudio.Shell.Framework
    )
else()
endif()

set(CTEST_ADAPTER_REFERENCES
  ${VSPACKAGE_REFERENCES}
  Microsoft.VisualStudio.Shell.Immutable.10.0
  System.ComponentModel.Composition
  System.Core
  System.Management
  )

set(CTEST_ADAPTER_PROJECT_TYPES
  "{82b43b9b-a64c-4715-b499-d71e9ca2bd60}"
  "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"
  )
  
# set testwindow references
set(ADAPTER_TESTWINDOW_REFERENCES
  Microsoft.VisualStudio.TestPlatform.ObjectModel
  Microsoft.VisualStudio.TestWindow.Core
  Microsoft.VisualStudio.TestWindow.Interfaces
  )
set(VC_TEST_WINDOW_SUBPATH "CommonExtensions/Microsoft/TestWindow")
get_filename_component(devPath "${CMAKE_VS_DEVENV_COMMAND}" DIRECTORY)
set(VC_TEST_WINDOW_PATH "${devPath}/${VC_TEST_WINDOW_SUBPATH}")
if(NOT EXISTS "${VC_TEST_WINDOW_PATH}")
  message(FATAL_ERROR "test window path not found:${VC_TEST_WINDOW_PATH}")
endif()
unset(CTEST_ADAPTER_HINT_REFERENCES)
foreach(v ${ADAPTER_TESTWINDOW_REFERENCES})
  list(APPEND CTEST_ADAPTER_HINT_REFERENCES VS_DOTNET_REFERENCE_${v})
  list(APPEND CTEST_ADAPTER_HINT_REFERENCES "${VC_TEST_WINDOW_PATH}/${v}.dll")
endforeach()

# vsix manifest
set(vsixManifestInputFile
  "${CMAKE_CURRENT_SOURCE_DIR}/source.in.extension.vsixmanifest")
set(vsixManifestOutputFile
  "${CMAKE_CURRENT_BINARY_DIR}/source.extension.vsixmanifest")
configure_file("${vsixManifestInputFile}"
  "${vsixManifestOutputFile}" @ONLY)

# assembly info
set(assemblyInfoInputFile "AssemblyInfo.cs.in")
set(assemblyInfoOutputFile 
  "${CMAKE_CURRENT_BINARY_DIR}/Properties/AssemblyInfo.cs")
configure_file("${assemblyInfoInputFile}"
  "${assemblyInfoOutputFile}" @ONLY)

# vsix content file properties
set_source_files_properties(${ADAPTER_CONTENT_FILES}
  PROPERTIES
  VS_COPY_TO_OUT_DIR "PreserveNewest"
  VS_INCLUDE_IN_VSIX "true"
  VS_TOOL_OVERRIDE "Content"
  )

set_source_files_properties(CTestAdapterOptionPage.cs
  PROPERTIES
  VS_CSHARP_SubType "Code")

add_library(CTestAdapter SHARED
  ${VSPACKAGE_SOURCES}
  ${vsixManifestInputFile}
  ${vsixManifestOutputFile}
  ${assemblyInfoOutputFile}
  ${ADAPTER_CONTENT_FILES}
  ${ADAPTER_SOURCES}
  )

target_compile_options(CTestAdapter PRIVATE
  "/langversion:4"
  "/platform:anycpu"
  )

set_target_properties(CTestAdapter
  PROPERTIES
  LINKER_LANGUAGE "CSharp"
  VS_GLOBAL_PROJECT_TYPES "${CTEST_ADAPTER_PROJECT_TYPES}"
  VS_GLOBAL_ROOTNAMESPACE "CTestAdapter"
  VS_DOTNET_TARGET_FRAMEWORK_VERSION "${DOTNET_FRAMEWORK_VERSION}"
  VS_DOTNET_REFERENCES "${CTEST_ADAPTER_REFERENCES}"
  VS_GLOBAL_MinimumVisualStudioVersion "${MSBUILD_VERSION}"
  VS_GLOBAL_Install "true"
  VS_GLOBAL_InstallFrom "Disk"
  VS_GLOBAL_MapFileExtensions "true"
  VS_GLOBAL_ApplicationRevision 0
  VS_GLOBAL_ApplicationVersion "${CTEST_ADAPTER_VERSION}.0"
  VS_GLOBAL_IsWebBootstrapper "false"
  VS_GLOBAL_UseApplicationTrust "false"
  VS_GLOBAL_BootstrapperEnabled "true"
  VS_GLOBAL_SchemaVersion "2.0"
  VS_GLOBAL_GeneratePkgDefFile "True"
  VS_GLOBAL_CopyBuildOutputToOutputDirectory "True"
  VS_GLOBAL_CopyOutputSymbolsToOutputDirectory "True"
  VS_GLOBAL_DeployExtension "True"
  VS_GLOBAL_IncludeAssemblyInVSIXContainer "true"
  VS_GLOBAL_IncludeDebugSymbolsInLocalVSIXDeployment "true"
  VS_GLOBAL_IncludeDebugSymbolsInVSIXContainer "true"
  VS_GLOBAL_CreateVsixContainer "True"
  VS_GLOBAL_StartArguments "/rootsuffix Exp \"${CTEST_ADAPTER_SAMPLE_SOLUTION_FILE}\""
  VS_GLOBAL_StartAction "Program"
  VS_GLOBAL_StartProgram "${DEVENV_EXE}"
  VS_GLOBAL_SignAssembly "true"
  VS_GLOBAL_AssemblyOriginatorKeyFile "${CMAKE_CURRENT_SOURCE_DIR}/Key.snk"
  VS_GLOBAL_UseCodebase "true"
  VS_USER_PROPS "vsix.props"
  #VS_GLOBAL_PublishUrl "publish\\\\"
  #VS_GLOBAL_CopyVsixExtensionLocation "..\\dist"
  #VS_GLOBAL_CopyVsixExtensionFiles "True"
  ${CTEST_ADAPTER_HINT_REFERENCES}
  )

# find Microsoft.VsDSK.targets and "link" to it
set(VsSdkTargetsFileSubPath
  "Microsoft/VisualStudio/v${MSBUILD_VERSION}/VSSDK/Microsoft.VsSDK.targets")
set(VsSdkTargetsFile "$ENV{ProgramFiles\(x86\)}/MSBuild/${VsSdkTargetsFileSubPath}")
if(${VS_VERSION} STREQUAL "15")
  string(REPLACE 
    "/Common7/IDE/devenv.com" "/MSBuild/${VsSdkTargetsFileSubPath}" 
    VsSdkTargetsFile "${CMAKE_VS_DEVENV_COMMAND}")
endif()
if(NOT EXISTS "${VsSdkTargetsFile}")
  message(FATAL_ERROR ".targets file not found: ${VsSdkTargetsFile}")
endif()
file(TO_CMAKE_PATH "${VsSdkTargetsFile}" VsSdkTargetsFile)
target_link_libraries(CTestAdapter "${VsSdkTargetsFile}")

# add custom command to copy result somewhere
option(COPY_VSIX_TO_SOURCE "" ON)
if(COPY_VSIX_TO_SOURCE)
  add_custom_command(TARGET CTestAdapter POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/CTestAdapter.vsix" 
    "${CMAKE_BINARY_DIR}/CTestAdapter-$<CONFIG>.vsix")
endif()
