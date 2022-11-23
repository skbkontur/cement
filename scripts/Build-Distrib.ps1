#!/usr/bin/pwsh

<#
.SYNOPSIS
Build logic from CI scripts for local builds
#>

$configuration = "Debug"

function Confirm-Path($path)
{
    if (-not (Test-Path $path -PathType Container))
    {
        New-Item $path -ItemType Directory
    }
}

function Reset-Path($path)
{
    if (Test-Path $path)
    {
        Remove-Item $path -Recurse -Force
        Confirm-Path $path
    }
}

function Invoke-Publish($rid)
{
    dotnet publish -c $configuration -r $rid -o ".\files-common\$rid" /p:PublishSingleFile=true /p:DebugType=None --self-contained true .\Cement.Net\CementEntry.csproj
}

Set-Location -Path ..

git submodule update --init

Reset-Path .\dotnet
Confirm-Path .\files-common\win10-x64
Confirm-Path .\files-common\linux-x64
Confirm-Path .\files-common\osx-x64

Invoke-Publish win10-x64
Invoke-Publish linux-x64
Invoke-Publish osx-x64

Copy-Item .\externals\NuGet.exe .\files-common\win10-x64\ -Force

$commit = git log -1 --pretty=format:"%H"
Copy-Item .\files-common\* .\dotnet -Recurse
Compress-Archive .\dotnet "$commit.zip"
