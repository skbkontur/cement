using System.IO;
using System.Linq;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.Exceptions;
using Cement.Cli.Common.Extensions;
using Cement.Cli.Common.YamlParsers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class PackCommand : Command<PackCommandOptions>
{
    private readonly ILogger<PackCommand> logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly IDepsValidatorFactory depsValidatorFactory;

    public PackCommand(ILogger<PackCommand> logger, ConsoleWriter consoleWriter, IDepsValidatorFactory depsValidatorFactory)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.depsValidatorFactory = depsValidatorFactory;
    }

    public override string Name => "pack";
    public override string HelpMessage => @"
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
";

    protected override int Execute(PackCommandOptions options)
    {
        CommandHelper.SetWorkspace(CommandLocation.InsideModuleDirectory);
        CommandHelper.CheckRequireYaml(CommandLocation.InsideModuleDirectory, true);

        var modulePath = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
        var moduleName = Path.GetFileName(modulePath);
        var project = Yaml.GetProjectFileName(options.Project, moduleName);
        var configuration = options.Configuration ?? "full-build";

        var buildData = Yaml.BuildParser(moduleName).Get(configuration).FirstOrDefault(t => !t.Target.IsFakeTarget());

        var projectPath = Path.GetFullPath(project);
        var csproj = new ProjectFile(projectPath);
        var deps = new DepsParser(consoleWriter, depsValidatorFactory, modulePath).Get(configuration);
        consoleWriter.WriteInfo("patching csproj");
        var patchedDocument = csproj.CreateCsProjWithNugetReferences(deps.Deps, options.PreRelease);
        var backupFileName = Path.Combine(Path.GetDirectoryName(projectPath) ?? "", "backup." + Path.GetFileName(projectPath));
        if (File.Exists(backupFileName))
            File.Delete(backupFileName);
        File.Move(projectPath, backupFileName);
        try
        {
            XmlDocumentHelper.Save(patchedDocument, projectPath, "\n");
            var buildYamlScriptsMaker = new BuildYamlScriptsMaker();
            var moduleBuilder = new ModuleBuilder(logger, consoleWriter, options.BuildSettings, buildYamlScriptsMaker);
            moduleBuilder.Init();
            consoleWriter.WriteInfo("start pack");
            if (!moduleBuilder.DotnetPack(modulePath, projectPath, buildData?.Configuration ?? "Release"))
                return -1;
        }
        finally
        {
            if (File.Exists(projectPath))
                File.Delete(projectPath);
            File.Move(backupFileName, projectPath);
        }

        return 0;
    }

    protected override PackCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParsePack(args);

        string configuration = null;
        if (parsedArgs["configuration"] != null)
            configuration = (string)parsedArgs["configuration"];

        var preRelease = (bool)parsedArgs["prerelease"];

        var buildSettings = new BuildSettings
        {
            ShowAllWarnings = (bool)parsedArgs["warnings"],
            ShowObsoleteWarnings = (bool)parsedArgs["obsolete"],
            ShowOutput = (bool)parsedArgs["verbose"],
            ShowProgress = (bool)parsedArgs["progress"],
            ShowWarningsSummary = true
        };

        var project = (string)parsedArgs["project"];
        if (!project.EndsWith(".csproj"))
            throw new BadArgumentException(project + " is not csproj file");

        return new PackCommandOptions(project, configuration, buildSettings, preRelease);
    }
}
