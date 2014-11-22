cmake_minimum_required(VERSION 3.0)
project(SampleProject)


enable_testing()
add_executable(successApp test1.cpp)
add_executable(failApp test2.cpp)


add_test(successTest successApp)
add_test(failTest failApp)