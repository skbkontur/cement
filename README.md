# Cement [![Build status](https://ci.appveyor.com/api/projects/status/nfbn7d6rxmk88o2q/branch/master?svg=true)](https://ci.appveyor.com/project/skbkontur/cement/branch/master)

- Dependency management tool, mainly for C# projects
- Allow getting and building your projects with dependencies
- Every project is a git repository
- Every project is a solution or a content module

# Get started

## Install

### Windows
1. You should have git and Visual Studio or MSBuild Tools installed 
2. Download zip from https://github.com/skbkontur/cement/releases/latest
3. Unzip and run `dotnet\install.cmd`
4. Restart terminal
5. Command `cm` shows you available commands in any directory
6. If you have installed Visual Studio 2017 in custom folder run `set VS150COMNTOOLS=D:\Program Files\Microsoft Visual Studio\2017\Professional\Common7\Tools\` (with your custom foler path) in cmd.

### macOS
1. You should have git and mono (5 or above) installed
2. Download zip from https://github.com/skbkontur/cement/releases/latest
3. Unzip and run `./install.sh` from the dotnet directory
4. Either add `~/bin/` to your `PATH` variable or run `alias cm='mono ~/bin/dotnet/cm.exe'`
5. Run `cm` to see the list of commands

### Linux
Here is a Dockerfile example of how to get Ubuntu image with cement installed
```Dockerfile
FROM ubuntu
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/ubuntu xenial main" | tee /etc/apt/sources.list.d/mono-official.list
RUN apt-get update
RUN apt-get install -y mono-devel git wget
RUN cd ~
RUN git clone https://github.com/skbkontur/cement.git ~/cement
RUN wget -O ~/cement/nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
RUN mkdir ~/bin
RUN cd ~/cement && mono nuget.exe restore -OutputDir packages/ && msbuild /p:Configuration=Release
RUN mono ~/bin/dotnet/cm.exe reinstall
```

## Work with cement

Use `cm help` to view [all cement commands](README-commands.md#commands).
Use `cm %command_name% /?` or `cm help %command_name%` to view command description.

All module descriptions are stored in a special git repo. 

In the beginning your cement will use sample modules from https://github.com/KungA/cement-sample-modules/blob/master/modules

Command `cm ls` shows modules A, B, C, D.

## Get modules

Let some commands run to get and build module A, which uses modules B, C, D.

### cm init
All modules should be downloaded into one 'cement tracked' directory.

### cm get A
Download module A with deps B, C, D.

### cd A
Go to module directory.

### cm build-deps
Build dependencies for current module in the right order.

### cm build
Build current module. You can use it now.

![](https://raw.githubusercontent.com/skbkontur/cement/master/images/start.png)

## Update dependencies

### cm update-deps
Get latest versions of dependencies from git

### cm build-deps
Need to build new version of modules, which were changed.

### cm build
And current module.

## Feature flags
Feature flags may be edit in config file '%USERPROFILE%/bin/dotnet/featureFlags.json'

### Clean before build
Deleting all local changes before build in commands 'build' and 'build-deps' if project's TargetFramework is 'netstandardXX'

Default: true

## Creating modules

1. Specify git repo witch will contain all module descriptions like https://github.com/KungA/cement-sample-modules
2. Add empty `modules` file to it and push
2. Fill it into `%userprofile%\.cement\settings` file instead of `git@github.com:KungA/cement-sample-modules.git`
3. Create repositories for your modules like

   https://github.com/KungA/cement-sample-A
   
   https://github.com/KungA/cement-sample-B
   
   https://github.com/KungA/cement-sample-C
   
   https://github.com/KungA/cement-sample-D
   
4. Run `cm module add A git@github.com:KungA/cement-sample-A.git` to add your modules to cement
5. Fill `module.yaml` file, describing cement modules (see [appropriate documentation](README-module.yaml.md#moduleyaml)) 


## All commands description
[here](README-commands.md#commands)

## Module.yaml format
[here](README-module.yaml.md#moduleyaml)
