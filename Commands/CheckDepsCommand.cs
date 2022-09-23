using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;

namespace Commands
{
    public sealed class CheckDepsCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "check-deps",
            MeasureElapsedTime = false,
            RequireModuleYaml = true,
            Location = CommandSettings.CommandLocation.RootModuleDirectory
        };
        private readonly ConsoleWriter consoleWriter;
        private string configuration;
        private bool showAll;
        private bool findExternal;
        private bool showShort;

        public CheckDepsCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
            : base(consoleWriter, Settings, featureFlags)
        {
            this.consoleWriter = consoleWriter;
        }

        public override string HelpMessage => @"
    Checks deps in module.yaml and references in *.csproj

    Usage:
        cm check-deps [-c configName]

        -c/--configuration      - check deps for specific configuration
        -a/--all                - show csproj names which has bad references
        -s/--short              - show only section with bad references
        -e/--external           - check references to not cement modules or to current module
";

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseCheckDeps(args);
            configuration = (string)parsedArgs["configuration"];
            showAll = (bool)parsedArgs["all"];
            showShort = (bool)parsedArgs["short"];
            findExternal = (bool)parsedArgs["external"];
        }

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();
            var ok = true;
            configuration = configuration ?? "full-build";

            consoleWriter.WriteInfo($"Checking {configuration} configuration result:");
            var result = new DepsChecker(cwd, configuration, Helper.GetModules()).GetCheckDepsResult(findExternal);
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
                    if (!showAll)
                        continue;
                    foreach (var file in group)
                        consoleWriter.WriteLine("\t\t" + file.CsprojFile);
                }
            }

            if (result.NotUsedDeps.Any() && !showShort)
            {
                ok = false;
                consoleWriter.WriteWarning("Extra deps:");
                foreach (var m in result.NotUsedDeps)
                    consoleWriter.WriteBuildWarning("\t- " + m);
            }

            var overhead = new SortedSet<string>(result.ConfigOverhead.Where(m => !result.NotUsedDeps.Contains(m)));
            if (overhead.Any() && !showShort)
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
}
