@echo off
rmdir /S /Q %userprofile%\.cement
rmdir /S /Q %userprofile%\bin\dotnet
xcopy ..\dotnet "%userprofile%\bin\dotnet" /s /i /Y
cm.exe reinstall
