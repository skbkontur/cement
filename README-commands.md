# Commands

[cm help](#cm-help)

[cm self-update](#cm-self-update)

[cm --version](#cm---version)

[cm init](#cm-init)

[cm get](#cm-get)

[cm update-deps](#cm-update-deps)

[cm update](#cm-update)

[cm build-deps](#cm-build-deps)

[cm build](#cm-build)

[cm ls](#cm-ls)

[cm module](#cm-module)

[cm ref](#cm-ref)

[cm analyzer](#cm-analyzer)

[cm show-configs](#cm-show-configs)

[cm check-deps](#cm-check-deps)

[cm show-deps](#cm-show-deps)

[cm usages](#cm-usages)

[cm pack](#cm-pack)

[cm status](#cm-status)


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

        -m/--merged[=some_branch]   checks if <some_branch> was merged into current dependency repo state. 
                                    Checks for 'master' by default

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

        -m/--merged[=some_branch]   checks if <some_branch> was merged into current dependency repo state.
                                    Checks for 'master' by default

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
        cm build-deps [-r|--rebuild] [-q|--quickly] [-v|--verbose|-w|--warnings] [-p|--progress] [-c|--configuration <config-name>]

        -r/--rebuild              - rebuild all deps (default skip module if it was already built,
                                    according to its commit-hash)
        -q/--quickly              - build deps in parallel
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
        cm module <add|change> module_name module_fetch_url [-p|--pushurl=module_push_url] [--package=package_name]
        --pushurl        - module push url
        --package        - name of repository with modules description, specify if multiple

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
            -e/--external       try to fix references to not cement modules or to current module

        Example:
            change	<HintPath>..\..\props\libprops\bin\Release\4.0\Kontur.Core.dll</HintPath>
            to		<HintPath>..\..\core\bin\Release\Kontur.Core.dll</HintPath>

### cm analyzer

    Adds analyzers in *.sln

    analyzer add
        Adds analyzer target reference assemblies to msbuild project files into solution

        Usage:
            cm analyzer add <module-name>/[<configuration>] [<solution-file>]

        Example:
            cm analyzer add analyzers.async-code/warn
                Adds analyzer from module analyzers.code-style to all projects 
                in current solution and adds analyzers.code-style to 'module.yaml' file
            cm analyzer add analyzers.async-code mysolution.sln
                Adds analyzer from module analyzers.code-style to all projects 
                in mysolution.sln and adds analyzers.code-style to 'module.yaml' file

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
        -e/--external           - check references to not cement modules or to current module

### cm show-deps

    Shows module deps in arbor.js

    Usage:
        cm show-deps [-c <config-name>]

### cm usages

    Performs operations with module usages

    usages show
        shows the modules linked to the given dependence

        Usage:
            cm usages show [-m=<module>] [-c=<configuration>] [-b=<branch>] [-a]
            -m/--module            - module name (current module name by default)
            -c/--configuration     - configuration name (* by default)
            -b/--branch            - branch name (* by default)
            -a/--all               - show every branch of each parent
            -e/--edges             - prints graph in proper format 
                                     for graph visualizers(i.e. arborjs.org/halfviz/)

        Example:
            cm usages show -m=logging
                show the modules which linked to the logging/full-build master

    usages build
        tries get and build all modules (in masters) linked to the current

        Usage:
            cm usages build [-b=<branch>] [-p]
            -b/--branch            - checking parents which use this branch (current by default)
            -p/--pause             - pause on errors

    usages grep
        search for given pattern in modules (in masters) 
        linked to the current (<branch>, master by default)

        Usage:
            cm usages grep [-b=<branch>] [-i/--ignore-case] [-s/--skip-get] <patterns> 
                [-f <patternFile>] [-- <fileMask>]
            -i/--ignore-case
            -s/--skip-get           - skip cloning modules
            -f <patternFile>        - search for patterns from file (line delimited)
            <patterns>              - patterns for search
            <fileMasks>             - limit the search to paths matching at least one pattern
            patterns combined with --or by default, can be combined with --and (<p1> --and <p2>)
            for other options see help for `git grep` command

        Example:
            cm usages grep "new Class" "Class.New" -- *.cs
                show lines contains "new Class" or "Class.New" in modules linked to the current, only in *.cs files

### cm pack

    Packs project to nuget package.
    Replaces file references to package references in csproj file and runs 'dotnet pack' command.
    Allows to publish nuget package to use outside of cement.
    Searches cement deps in nuget by module name.

    Usage:
        cm pack [-v|--verbose|-w|-W|--warnings] [-p|--progress] [-c configName] <project-file>
        -c/--configuration      - build package for specific configuration

        -v/--verbose            - show full msbuild output
        -w/--warnings           - show warnings
        -W                      - show only obsolete warnings

        -p/--progress           - show msbuild output in one line


### cm status

    Prints status of modifed git repos in the cement tracked dir
    It checks repo for push/pull and local state

    Usage:
        cm status

    Runs anywhere in the cement tracked tree
