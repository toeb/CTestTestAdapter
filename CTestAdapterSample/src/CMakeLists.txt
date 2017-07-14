cmake_minimum_required(VERSION 3.8 FATAL_ERROR)
project(CTestAdapterSample)

enable_testing()

set_property(GLOBAL PROPERTY USE_FOLDERS true)
set_property(DIRECTORY PROPERTY VS_STARTUP_PROJECT main)

add_executable(main main.cpp)

add_test(main main)

add_test(NAME "special:main"
  COMMAND main)
