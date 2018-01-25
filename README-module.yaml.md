# module.yaml
Information about the module is contained in `module.yaml`.

Each module may consist of one or several configurations (for example, client configuration, configuration with server part, configuration including tests).

Each configuration may include the following sections:

- deps
- build
- install/artifacts

Configurations may be inherited in order to avoid repeating of dependencies information.

A module shall contain configuration `full-build`, including all the others, if possible. It will be used while execution of the following commands `cm get` and `cm build`.

Optional configuration `default` can contain default build and deps sections.

Example:

    # configuration description section client
    client: 
      # list of dependencies of client configuration
      # dependencies are listed in the following form: <moduleName>[@branchName][/configName]
      # kanso – reference to kanso module. 
      # kanso/client – reference to kanso in client configuration. 
      # kanso@develop/client - kanso from develop branch in client configuration
      deps: 
        - core
        - log4net
        - nunit
        - logging
        - http
        - http.rp
        - topology
        # to prefer current branch in deps
        - force: $CURRENT_BRANCH
        # or use <moduleName>@$CURRENT_BRANCH to prefer current branch in some module

      # information on build of the present module in the present configuration
      build:
        # build solution Kontur.Drive.sln in Client configuration
        target: Kontur.Drive.sln
        configuration: Client

      # Information on results of the present configuration build
      install:
        - bin/Kontur.Drive.Client.dll
        - bin/Kontur.Drive.ServiceModel.dll
        # For operation of Kontur.Drive.Client.dll you need a reference on logging module build
        - module logging
        # Or a reference on nuget bundle
        - nuget Newtonsoft.Json/6.0.8

    # Configuration sdk "expands" configuration client (> client) 
    # and is a configuration at default (*default) (it is used at referencing on this module)
    sdk > client *default:
      # in order to avoid repetition of dependencies lists
      # deps from client are inherited here and you have no need to state them once again
      deps:
        - auth
        - libfront
        - zebra
        - kanso
        - upload-service
        - access-control
        - zebra-utils
        - config
        - zookeeper

      # Section build is NOT inherited 
      build:
        target: Kontur.Drive.sln
        configuration: Sdk

      # Section install is inherited, so you need to add only missing binaries
      install:
        - Kontur.Drive.TestHost/bin/Release/Kontur.Drive.TestHost.exe
        - Kontur.Drive.TestHost/bin/Release/ServiceStack.Interfaces.dll
      # DLL files that can be used by other modules,
      # but are ignored while running cm ref add
      artifacts:
        - Kontur.Drive.TestHost/bin/Release/SomeOther.dll


    # Configuration full-build expands client and sdk
    full-build > client, sdk:
      deps:
        # So you state diff on deps (in configuration full-build kanso/full-build is used. 
        # At first, we “delete” kanso from dependencies, then add kanso/full-build)
        - -kanso
        - kanso/full-build
     
      build:
        target: Kontur.Drive.sln
        configuration: Release


# build section

#### For a module which does not need to be built:

    build:
      target: None
      configuration: None

#### If it is necessary to state additional parameters:

    build:
      # A statement of build target
      target: Solution.sln
      tool:
        # An assembling tool. msbuild - for MSBuild.exe at Windows and xbuild at *nix. dotnet - for new .NET Core
        name: msbuild
        # msbuild version, the latest at default
        version: "14.0"
        # set VS150COMNTOOLS=D:\Program Files\Microsoft Visual Studio\2017\Professional\Common7\Tools\
      # Solution configuration
      configuration: Release
      # You can state your parameters here. Default parameters are not used. 
      # Quotation marks in parameters are shielded by '\'.
      # Default parameters for msbuild:
      # /t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0 /p:VisualStudioVersion=14.0 (if not stated)
      parameters: "/p:WarningLevel=0"

#### If there are several solutions in a module

    build:
      - name: a.release             # You shall name each part
        target: a.sln
        configuration: release
      - name: b.debug
        target: b.sln
        configuration: debug

#### Build with custom script:

build.xproj:

    <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
        <Target Name="Rebuild">
            <Exec Command="ping google.com > 1.txt" />
        </Target>
    </Project>

module.yaml:

    full-build:
      deps:
      build:
        target: build.xproj
        configuration: Release

# Git hooks

    default:                  # Description in a default configuration
      hooks:
        - myhooks/pre-push    # A way to a hook in a repository 
                              # will be copied to <module_name>/.git/hooks
        # A hook inbuilt into pre-commit will check that files containing non-ASCII symbols have UTF-8 with BOM coding
        - pre-commit.cement

    full-build *default:      # Other module configurations


If there is a hook in a present branch, then it is not deleted when you switch to another branch.
If you want to use your hook with pre-commit.cement, just add its call-out into your hook

    .git/hooks/pre-commit.cement
    if [ $? -ne 0 ]; then
      exit 1
    fi
