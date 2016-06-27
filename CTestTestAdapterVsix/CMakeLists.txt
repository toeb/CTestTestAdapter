set(CONTENT_FILES_VSIX
    ../CMake_logo.png
    ../screenshot.png
    ../LICENSE.txt
    )
set_source_files_properties(
    ${CONTENT_FILES_VSIX}
    PROPERTIES
    VS_COPY_TO_OUT_DIR "Always"
    VS_INCLUDE_IN_VSIX "true"
    )
set(PROPERTIES_VSIX
    VS_GLOBAL_VSToolsPath "$(MSBuildExtensionsPath32)/Microsoft/VisualStudio/v${TOOLS_VERSION}"
    VS_CUSTOM_TARGETS "$(VSToolsPath)\\VSSDK\\Microsoft.VsSDK.targets"
    VS_GLOBAL_PublishUrl "publish\\"
	VS_GLOBAL_Install "true"
	VS_GLOBAL_InstallFrom "Disk"
	VS_GLOBAL_MapFileExtensions "true"
	VS_GLOBAL_ApplicationRevision 0
	VS_GLOBAL_ApplicationVersion 1.0.0.0
	VS_GLOBAL_IsWebBootstrapper "false"
	VS_GLOBAL_UseApplicationTrust "false"
	VS_GLOBAL_BootstrapperEnabled "true"
	VS_GLOBAL_SchemaVersion "2.0"
	VS_GLOBAL_GeneratePkgDefFile "false"
    VS_GLOBAL_CopyBuildOutputToOutputDirectory "false"
    VS_GLOBAL_CopyOutputSymbolsToOutputDirectory "false"
    VS_GLOBAL_DeployExtension "False"
    VS_GLOBAL_IncludeAssemblyInVSIXContainer "False"
    VS_GLOBAL_IncludeDebugSymbolsInLocalVSIXDeployment "False"
	VS_GLOBAL_IncludeDebugSymbolsInVSIXContainer "false"
    )

foreach(vs ${VS_VERSIONS})
    if(NOT TARGET ${${ADAPTER_NAME}_VS${vs}})
        set(fw ${FW_VERSION_${vs}})
        set(FINAL_TARGET_NAME ${ADAPTER_NAME}Vsix_VS${vs})
        add_csharp_project(${FINAL_TARGET_NAME} ${vs} ${fw}
            ${ADAPTER_NAME}Vsix
            VSIX
            SOURCES
            vsix${vs}/source.extension.vsixmanifest
            Properties/AssemblyInfo.cs
            ${CONTENT_FILES_VSIX}
            COMPILE_OPTIONS 
            ${CS_COMPILE_OPTIONS}
            PROPERTIES
            ${PROPERTIES_VSIX}
            LINK_LIBRARIES
            ${ADAPTER_NAME}_VS${vs}
            )
        set_target_properties(${FINAL_TARGET_NAME}
            PROPERTIES
            ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/Ar$<CONFIG>${vs}"
            LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/Lib$<CONFIG>${vs}"
            RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/Bin$<CONFIG>${vs}"
            )
        if(COPY_VSIX_TO_SOURCE)
            add_custom_command(TARGET ${FINAL_TARGET_NAME} POST_BUILD
                COMMAND ${CMAKE_COMMAND} -E make_directory "${CMAKE_SOURCE_DIR}/dist"
                COMMAND ${CMAKE_COMMAND} -E copy_if_different
                    "${CMAKE_CURRENT_BINARY_DIR}/Bin$<CONFIG>${vs}/${FINAL_TARGET_NAME}.vsix" 
                    "${CMAKE_SOURCE_DIR}/dist/${FINAL_TARGET_NAME}_${TOOLS_VERSION}.vsix"
                )
        endif()
    else()
        message("Test-Adapter ${ADAPTER_NAME}_VS${vs} does not exist.\n"
                "Not generating VSIX project for it."
            )
    endif()
endforeach()
