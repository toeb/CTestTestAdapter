@echo off
echo installing ctest test adapter ...
@call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\vsvars32.bat"
VsixInstaller /q "CTestTestAdapter.vsix"
echo installing ctest test adapter ... done
