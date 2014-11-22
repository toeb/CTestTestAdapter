# CTestTestAdapter

A Test Adapter to run kitware's CTest from the Visual Studio (2013) TestExplorer

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

See example CMake/CTest/C++ Project


