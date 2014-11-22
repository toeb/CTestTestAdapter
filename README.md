# CTestTestAdapter

A Test Adapter to run [kitware's CMake/CTest](http://cmake.org/) from the Visual Studio (2013) TestExplorer

## Screenshot

![screenshot](https://github.com/toeb/CTestTestAdapter/blob/master/screenshot.png)

## Features

* Discovers all tests which are visable in your CMakeLists.txt by running `ctest -N` in your binary_dir
* Control of test execution through Test Explorer Window
* Success or Fail based on Outcome of ctest run

## Disclaimer

This software is currently not in its finished state, It still needs alot of polishing because it is slow and not tested.
It is currently more of a proof of concept than anything else.

I hope others might want to help developing it as I can't promise to work on it

## Future

* More Test MetaData (line number, file, etc)
* More efficient Test Runs


## Example

See simple example in SampleProject CMake/CTest/C++ Project


*note*:  The CMake logo belongs to kitware and is under the Creative Commons license.

