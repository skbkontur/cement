using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.ArgumentsParsing;
using Cement.Cli.Common.DepsValidators;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class CheckDepsCommand : Command<CheckDepsCommandOptions>
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IDepsValidatorFactory depsValidatorFactory;

    public CheckDepsCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, IDepsValidatorFactory depsValidatorFactory)
        : base(consoleWriter, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.depsValidatorFactory = depsValidatorFactory;
    }

    public override bool RequireModuleYaml { get; set; } = true;
    public override CommandLocation Location { get; set; } = CommandLocation.RootModuleDirectory;
    public override string Name => "check-deps";
    public override string HelpMessage => @"
    Checks deps in module.yaml and references in *.csproj

    Usage:
        cm check-deps [-c configName]

        -c/--configuration      - check deps for specific configuration
        -a/--all                - show csproj names which has bad references
        -s/--short              - show only section with bad references
        -e/--external           - check references to not cement modules or to current module
";

    protected override CheckDepsCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseCheckDeps(args);
        var configuration = (string)parsedArgs["configuration"];
        var showAll = (bool)parsedArgs["all"];
        var showShort = (bool)parsedArgs["short"];
        var findExternal = (bool)parsedArgs["external"];

        return new CheckDepsCommandOptions(configuration, showAll, findExternal, showShort);
    }

    protected override int Execute(CheckDepsCommandOptions options)
    {
        var cwd = Directory.GetCurrentDirectory();
        var ok = true;
        var configuration = options.Configuration ?? "full-build";

        consoleWriter.WriteInfo($"Checking {configuration} configuration result:");
        var result = new DepsChecker(consoleWriter, depsValidatorFactory, cwd, configuration, Helper.GetModules())
            .GetCheckDepsResult(options.FindExternal);

        if (result.NoYamlInstallSection.Any())
        {
            ok = false;
            consoleWriter.WriteWarning("No 'install' section in modules:");
            foreach (var m in result.NoYamlInstallSection)
                consoleWriter.WriteBuildWarning("\t- " + m);
        }

        if (result.NotInDeps.Any())
        {
            ok = false;
            consoleWriter.WriteWarning("Found references in *csproj, but not found in deps:");
            var refs = result.NotInDeps.GroupBy(r => r.Reference);
            foreach (var group in refs.OrderBy(g => g.Key))
            {
                consoleWriter.WriteBuildWarning("\t- " + group.Key);
                if (!options.ShowAll)
                    continue;
                foreach (var file in group)
                    consoleWriter.WriteLine("\t\t" + file.CsprojFile);
            }
        }

        if (result.NotUsedDeps.Any() && !options.ShowShort)
        {
            ok = false;
            consoleWriter.WriteWarning("Extra deps:");
            foreach (var m in result.NotUsedDeps)
                consoleWriter.WriteBuildWarning("\t- " + m);
        }

        var overhead = new SortedSet<string>(result.ConfigOverhead.Where(m => !result.NotUsedDeps.Contains(m)));
        if (overhead.Any() && !options.ShowShort)
        {
            ok = false;
            consoleWriter.WriteWarning("Config overhead:");
            foreach (var m in overhead)
                consoleWriter.WriteBuildWarning("\t- " + m);
        }

        if (ok)
        {
            consoleWriter.WriteOk("No problems with deps");
        }
        else
        {
            if (result.NotInDeps.Any())
                consoleWriter.WriteInfo("See also 'ref fix' command.");
        }

        return 0;
    }
}
