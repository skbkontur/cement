@echo off
rmdir /S /Q %userprofile%\.cement
rmdir /S /Q %userprofile%\bin\dotnet
xcopy /Y .\NuGet.exe ..\..\dotnet\NuGet.exe
xcopy ..\..\dotnet "%userprofile%\bin\dotnet" /s /i /Y
.\cm.exe reinstall
%userprofile%\bin\cm.cmd
