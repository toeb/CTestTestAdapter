set(SOURCE_FILES
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
    Properties/AssemblyInfo.cs
    )
set(REFERENCES
	"envdte"
	"EnvDTE80"
    "Microsoft.CSharp"
    "Microsoft.VisualStudio.OLE.Interop"
	"Microsoft.VisualStudio.Shell.Immutable.10.0"
	"Microsoft.VisualStudio.Shell.Interop"
    "System"
    "System.ComponentModel.Composition"
    "System.Core"
    "System.Management"
    "System.Xml"
    "Microsoft.VisualStudio.Shell.Interop.8.0"
    "Microsoft.VisualStudio.Shell.Interop.9.0"
    "Microsoft.VisualStudio.Shell.Interop.10.0"
    )
set(TESTWINDOW_REFERENCES
	Microsoft.VisualStudio.TestPlatform.ObjectModel 
	Microsoft.VisualStudio.TestWindow.Core 
	Microsoft.VisualStudio.TestWindow.Interfaces 
    )

foreach(vs ${VS_VERSIONS})
    if(VS${vs}_EXISTS)
        set(fw ${FW_VERSION_${vs}})
        add_csharp_project(${ADAPTER_NAME}_VS${vs} ${vs} ${fw}
            ${ADAPTER_NAME}
            SOURCES 
            ${SOURCE_FILES}
            REFERENCES 
            ${REFERENCES}
            ${REFERENCES_${vs}}
            TESTWINDOW_REFERENCES
            ${TESTWINDOW_REFERENCES}
            COMPILE_OPTIONS 
            ${CS_COMPILE_OPTIONS}
            PROPERTIES
            ${PROPERTIES}
            )
        set_target_properties(${ADAPTER_NAME}_VS${vs}
            PROPERTIES
            ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/Ar$<CONFIG>${vs}"
            LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/Lib$<CONFIG>${vs}"
            RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>${vs}"
            )
    else()
        message("cannot find Visual Studio TestWindow for VS ${vs}.\n"
                "Not generating testadapter project for this."
            )
    endif()
endforeach()
