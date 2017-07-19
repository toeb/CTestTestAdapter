# CTestTestAdapter
## Developement moved to [https://github.com/micst/CTestTestAdapter](https://github.com/micst/CTestTestAdapter)

Since [micst](https://github.com/micst/) is the primary developer / maintainer now his repository is the master. 

A Test Adapter to run [kitware's CMake/CTest](http://cmake.org/) from the Visual Studio (2013/2015) TestExplorer

## Screenshot

![screenshot](https://github.com/toeb/CTestTestAdapter/blob/master/screenshot.png)

## Features

* Discovers all tests which are visable in your CMakeLists.txt by running `ctest -N` in your binary_dir (i.e. the directory where your .sln file is)
* Control of test execution through Test Explorer Window
* Success or Fail based on Outcome of ctest run
* Shows console output of test if test fails
* Shows source line where test is executed in CTestTestfile.cmake
* Allows debugging of tests
  * Visual Studio may take a few moments to attach itself to the executed program

## Disclaimer

This software is currently not in its finished state, It still needs alot of polishing because it is slow and not tested.
It is currently more of a proof of concept than anything else.

I hope others might want to help developing it as I can't promise to work on it

## Future

* ~~More Test MetaData (line number, file, etc)~~
* More efficient Test Runs


## Issues

if you are missing dependencies:  
* Install the Visual Studio SDK and the 
* other Dependencies can be found in `C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow\`

## Example

See simple example in SampleProject CMake/CTest/C++ Project


*note*:  The CMake logo belongs to kitware and is under the Creative Commons license.

