message(STATUS "adding CTestAdapter sample project"
  " (this will take some seconds)")

set(sampleSourceDir "${CMAKE_CURRENT_SOURCE_DIR}/src")
set(sampleBinaryDir "${CMAKE_CURRENT_BINARY_DIR}/src")

make_directory("${sampleBinaryDir}")

execute_process(
  COMMAND "${CMAKE_COMMAND}" 
    -G "${CMAKE_GENERATOR}" "${sampleSourceDir}"
  WORKING_DIRECTORY "${sampleBinaryDir}"
  RESULT_VARIABLE res
  OUTPUT_FILE "${sampleBinaryDir}/generation.log")

if(res)
  message(FATAL_ERROR "could not create sample project")
else()
  message(STATUS "sample project created")
endif()
  
set(CTEST_ADAPTER_SAMPLE_SOLUTION_FILE
  "${sampleBinaryDir}/CTestAdapterSample.sln"
  CACHE INTERNAL "")
