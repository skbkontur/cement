cd ..\Cement.Net
if not exist ..\files-common\win10-x64 mkdir ..\files-common\win10-x64
dotnet publish -r win10-x64 -o ..\files-common\win10-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:DebugType=None --self-contained true
if not exist ..\files-common\linux-x64 mkdir ..\files-common\linux-x64
dotnet publish -r linux-x64 -o ..\files-common\linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:DebugType=None --self-contained true
if not exist ..\files-common\osx-x64 mkdir ..\files-common\osx-x64
dotnet publish -r osx-x64 -o ..\files-common\osx-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:DebugType=None --self-contained true
timeout 2