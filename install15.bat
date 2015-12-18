@echo off
echo installing ctest test adapter 15 ...
@call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\vsvars32.bat"
VsixInstaller /q "CTestTestAdapter.vsix"
echo installing ctest test adapter ... done
