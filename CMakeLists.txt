cmake_minimum_required(VERSION 3.6 FATAL_ERROR)
project(CTestTestAdapter CSharp)

if(${CMAKE_GENERATOR} MATCHES "Visual Studio")
    if(${CMAKE_GENERATOR} MATCHES "2012")
        set(TOOLS_VERSION 11.0)
    elseif(${CMAKE_GENERATOR} MATCHES "2013")
        set(TOOLS_VERSION 12.0)
    elseif(${CMAKE_GENERATOR} MATCHES "2015")
        set(TOOLS_VERSION 14.0)
    else()
        message(FATAL_ERROR
            "This project must be generated using VS 2012, VS 2013, or VS 2015")
    endif()
else()
    message(FATAL_ERROR
        "This project must be generated using Visual Studio")
endif()

set(CMAKE_CONFIGURATION_TYPES "Release;Debug" CACHE INTERNAL "")

set(VC_TEST_WINDOW_SUBPATH "../IDE/CommonExtensions/Microsoft/TestWindow")
set(VC_TEST_WINDOW_PATH11 "$ENV{VS110COMNTOOLS}${VC_TEST_WINDOW_SUBPATH}")
set(VC_TEST_WINDOW_PATH12 "$ENV{VS120COMNTOOLS}${VC_TEST_WINDOW_SUBPATH}")
set(VC_TEST_WINDOW_PATH14 "$ENV{VS140COMNTOOLS}${VC_TEST_WINDOW_SUBPATH}")

if(EXISTS ${VC_TEST_WINDOW_PATH11})
    set(VS11_EXISTS TRUE)
endif()
if(EXISTS ${VC_TEST_WINDOW_PATH12})
    set(VS12_EXISTS TRUE)
endif()
if(EXISTS ${VC_TEST_WINDOW_PATH14})
    set(VS14_EXISTS TRUE)
endif()

#set(PROPERTIES VS_GLOBAL_Platform "AnyCPU")

set(CS_COMPILE_OPTIONS
    "/langversion:4"
    "/platform:anycpu"
    )

option(COPY_VSIX_TO_SOURCE "" ON)

set(ADAPTER_NAME CTestTestAdapter)

# Function to configure a C# project for a single version of Visual Studio.
function(add_csharp_project TARGET_NAME VS_VERSION DOTNET_VERSION ROOT_NAMESPACE)
    cmake_parse_arguments(ARG "VSIX" "" 
        "SOURCES;REFERENCES;TESTWINDOW_REFERENCES;COMPILE_OPTIONS;LINK_LIBRARIES;PROPERTIES" 
        ${ARGN})
    set(FINAL_TARGET_NAME ${TARGET_NAME})
    add_library(${FINAL_TARGET_NAME} SHARED 
        ${ARG_SOURCES})
    set_target_properties(${FINAL_TARGET_NAME}
        PROPERTIES
        VS_GLOBAL_KEYWORD "Win32Proj"
        VS_GLOBAL_ROOTNAMESPACE "${ROOT_NAMESPACE}"
        VS_DOTNET_TARGET_FRAMEWORK_VERSION "${DOTNET_VERSION}"
        VS_GLOBAL_MinimumVisualStudioVersion "${TOOLS_VERSION}"
        )
    if(DEFINED ARG_LINK_LIBRARIES)
        target_link_libraries(${FINAL_TARGET_NAME}
            ${ARG_LINK_LIBRARIES})
    endif()
    if(DEFINED ARG_COMPILE_OPTIONS)
        target_compile_options(${FINAL_TARGET_NAME}
            PRIVATE ${ARG_COMPILE_OPTIONS})
    endif()
    if(ARG_VSIX)
        set_target_properties(${FINAL_TARGET_NAME}
            PROPERTIES
            VS_GLOBAL_PROJECT_TYPES "{82b43b9b-a64c-4715-b499-d71e9ca2bd60};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"
            LINKER_LANGUAGE CSharp
            )
    endif()
    if(DEFINED ARG_PROPERTIES)
        set_target_properties(${FINAL_TARGET_NAME}
            PROPERTIES ${ARG_PROPERTIES})
    endif()
    if(DEFINED ARG_REFERENCES)
        set_target_properties(${FINAL_TARGET_NAME}
            PROPERTIES
            VS_DOTNET_REFERENCES "${ARG_REFERENCES}"
            )
    endif()
    if(DEFINED ARG_TESTWINDOW_REFERENCES)
        set(hint_refs)
        foreach(v ${ARG_TESTWINDOW_REFERENCES})
            list(APPEND hint_refs VS_DOTNET_REFERENCE_${v})
            list(APPEND hint_refs "${VC_TEST_WINDOW_PATH${VS_VERSION}}/${v}.dll")
        endforeach()
        set_target_properties(${FINAL_TARGET_NAME}
            PROPERTIES ${hint_refs})
    endif()
endfunction()

set(VS_VERSIONS)
if(${TOOLS_VERSION} VERSION_GREATER "10.0")
    list(APPEND VS_VERSIONS 11)
    set(FW_VERSION_11 v4.5)
    set(REFERENCES_11
        "Microsoft.VisualStudio.Shell.11.0"
        )
endif()
if(${TOOLS_VERSION} VERSION_GREATER "11.0")
    list(APPEND VS_VERSIONS 12)
    set(FW_VERSION_12 v4.5)
    set(REFERENCES_12
        "Microsoft.VisualStudio.Shell.12.0"
        )
endif()
if(${TOOLS_VERSION} VERSION_GREATER "12.0")
    list(APPEND VS_VERSIONS 14)
    set(FW_VERSION_14 v4.6)
    set(REFERENCES_14
        "Microsoft.VisualStudio.Shell.14.0"
        )
endif()

add_subdirectory(CTestTestAdapter)
add_subdirectory(CTestTestAdapterVsix)
