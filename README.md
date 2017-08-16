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
5. Command `cm` shows you available commands in any directory

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
5. Fill `module.yaml` file, describing cement modules (see [appropriate documentation](README-module.yaml.md)) 

# Commands
### cm help

    Prints help for command

    Usage:
        cm help <command-name>
        cm <command-name> /?
        cm <command-name> --help

    Example:
        cm help init


### cm self-update

    Updates cement itself (automatically updated every 5 hours)

    Usage:
        cm self-update

### cm --version

    Shows cement's version

    Usage:
        cm --version


### cm init

    Inits current directory as 'cement tracked'

    Usage:
        cm init

    Note:
        $HOME directory cannot be used with this command

### cm get

    Gets module with all deps

    Usage:
        cm get [-f/-r/-p] [-v] [-m[=branch]] [-c <config-name>] module_name[/configuration][@treeish] [treeish]
        cm get module_name@treeish/configuration

        -c/--configuration          gets deps for corresponding configuration

        -f/--force                  forcing local changes(not pulling from remote)
        -r/--reset                  resetting all local changes
        -p/--pull-anyway            try to fast-forward pull if local changes are found

        -m/--merged[=some_branch]   checks if <some_branch> was merged into current dependency repo state. Checks for 'master' by default

        -v/--verbose                show commit info for deps

    Example:
        cm get kanso/client@release -rv
        cm get kanso -c client release -rv

### cm update-deps

    Updates deps for current directory

    Usage:
        cm update-deps [-f/-p/-r] [--bin] [-m] [-c <config-name>] [--allow-local-branch-force] [-v]

        -c/--configuration          updates deps for corresponding configuration

        -f/--force                  forcing local changes(not pulling from remote)
        -r/--reset                  resetting all local changes
        -p/--pull-anyway            try to fast-forward pull if local changes are found

        -m/--merged[=some_branch]   checks if <some_branch> was merged into current dependency repo state. Checks for 'master' by default

        --allow-local-branch-force  allows forcing local-only branches

        -v/--verbose                show commit info for deps

    Example:
        cm update-deps -r --progress

### cm update

    Updates module for current directory

    Usage:
        cm update [-f/-r/-p] [-v] [treeish]

        -f/--force                  forcing local changes(not pulling from remote)
        -r/--reset                  resetting all local changes
        -p/--pull-anyway            try to fast-forward pull if local changes are found

        -v/--verbose                show commit info for deps

    This command runs 'update' ('git pull origin treeish') command for module
    If treeish isn't specified, cement uses current


### cm build-deps

    Performs build for current module dependencies

    Usage:
        cm build-deps [-r|--rebuild] [-v|--verbose|-w|--warnings] [-p|--progress] [-c|--configuration <config-name>]

        -r/--rebuild              - rebuild all deps (default skip module if it was already built, according to its commit-hash)
        -c/--configuration        - build deps for corresponding configuration

        -v/--verbose              - show full msbuild output
        -w/--warnings             - show warnings

        -p/--progress             - show msbuild output in one line

### cm build

    Performs build for the current module

    Usage:
        cm build [-v|--verbose|-w|-W|--warnings] [-p|--progress] [-c|--configuration <config-name>]

        -c/--configuration      - build corresponding configuration

        -v/--verbose            - show full msbuild output
        -w/--warnings           - show warnings
        -W                      - show only obsolete warnings

        -p/--progress           - show msbuild output in one line


### cm ls

    Lists all available modules

    Usage:
        cm ls [-l|-a] [-b=<branch>] [-u] [-p]

        -l/--local                   lists modules in current directory
        -a/--all                     lists all cement-known modules (default)

        -b/--has-branch<=branch>     lists all modules which have such branch
                                     --local key by default

        -u/--url                     shows module fetch url
        -p/--pushurl                 shows module pushurl

    Example:
        cm ls --all --has-branch=temp --url

### cm module

    Adds new or changes existing cement module
    Don't delete old modules

    Usage:
        cm module <add|change> module_name module_fetch_url [-p|--pushurl=module_push_url]
        --pushurl		 - module push url

### cm ref

    Adds or fixes references in *.csproj

    ref add
        Adds module target reference assemblies to msbuild project file

        Usage:
            cm ref add <module-name>[/configuration] <project-file>

        Example:
            cm ref add nunit myproj.csproj
                Adds reference to nunit.framework.dll to myproj.csproj and adds nunit to 'module.yaml' file

    ref fix
        Fixes deps and references in all csproj files to correct install files

        Usage:
            cm ref fix [-e]
            -e/--external       try to fix references to not cement modules

        Example:
            change	<HintPath>..\..\props\libprops\bin\Release\4.0\Kontur.Core.dll</HintPath>
            to		<HintPath>..\..\core\bin\Release\Kontur.Core.dll</HintPath>

### cm show-configs

    Shows configurations of module

    Usage:
        cm show-configs [<module_name>]

### cm check-deps

    Checks deps in module.yaml and references in *.csproj

    Usage:
        cm check-deps [-c configName]

        -c/--configuration      - check deps for specific configuration
        -a/--all                - show csproj names which has bad references
        -s/--short              - show only section with bad references

### cm show-deps

    Shows module deps in arbor.js

    Usage:
        cm show-deps [-c <config-name>]


### cm status

    Prints status of modifed git repos in the cement tracked dir
    It checks repo for push/pull and local state

    Usage:
        cm status

    Runs anywhere in the cement tracked tree
