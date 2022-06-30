@echo off
rmdir /S /Q %userprofile%\.cement
rmdir /S /Q %userprofile%\bin\dotnet

copy /Y .\NuGet.exe ..\..\dotnet\NuGet.exe

xcopy ..\..\dotnet "%userprofile%\bin\dotnet" /s /i /Y

copy /Y %userprofile%\bin\dotnet\win10-x64\cm.exe %userprofile%\bin\dotnet\cm.exe

.\cm.exe reinstall
%userprofile%\bin\cm.cmd
