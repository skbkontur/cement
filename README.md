# Cement [![Build status](https://ci.appveyor.com/api/projects/status/nfbn7d6rxmk88o2q/branch/master?svg=true)](https://ci.appveyor.com/project/beevee12723/cement/branch/master)

- Dependency management tool, mainly for C# projects
- Allow getting and building your projects with dependencies
- Every project is a git repository
- Every project is a solution or a content module

# Get started

## Install

1. You should have git and Visual Studio or MSBuild Tools installed 
2. Download zip from https://github.com/skbkontur/cement/releases/latest
3. Unzip and run `dotnet\install.cmd`
4. Restart terminal
5. Command `cm` shows you aviable commands in any directory

## Work with cement

Use `cm help` to view all cement commands.
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
5. Fill `module.yaml` file, describing cement modules
