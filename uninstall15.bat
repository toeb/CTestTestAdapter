@echo off
echo removing ctest test adapter 15 ...
@call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\vsvars32.bat"
VsixInstaller /q /u:CTestTestAdapter..7589ccbd-c148-4981-b13a-f61a2643f1ee
echo removing ctest test adapter ... done
