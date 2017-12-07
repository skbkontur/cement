using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;

namespace Commands
{
    public class CheckDeps : Command
    {
        private string configuration;
        private bool showAll;
        private bool findExternal;
        private bool showShort;

        public CheckDeps()
            : base(new CommandSettings
            {
                LogPerfix = "CHECK-DEPS",
                LogFileName = null,
                MeasureElapsedTime = false,
                RequireModuleYaml = true,
                Location = CommandSettings.CommandLocation.RootModuleDirectory
            })
        {
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseCheckDeps(args);
            configuration = (string) parsedArgs["configuration"];
            showAll = (bool) parsedArgs["all"];
            showShort = (bool) parsedArgs["short"];
            findExternal = (bool)parsedArgs["external"];
        }

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();
            var ok = true;
            configuration = configuration ?? "full-build";

            ConsoleWriter.WriteInfo($"Checking {configuration} configuration result:");
            var result = new DepsChecker(cwd, configuration, Helper.GetModules()).GetCheckDepsResult(findExternal);
            if (result.NoYamlInstallSection.Any())
            {
                ok = false;
                ConsoleWriter.WriteWarning("No 'install' section in modules:");
                foreach (var m in result.NoYamlInstallSection)
                    ConsoleWriter.WriteBuildWarning("\t- " + m);
            }

            if (result.NotInDeps.Any())
            {
                ok = false;
                ConsoleWriter.WriteWarning("Found references in *csproj, but not found in deps:");
                var refs = result.NotInDeps.GroupBy(r => r.Reference);
                foreach (var group in refs.OrderBy(g => g.Key))
                {
                    ConsoleWriter.WriteBuildWarning("\t- " + group.Key);
                    if (!showAll)
                        continue;
                    foreach (var file in group)
                        ConsoleWriter.WriteLine("\t\t" + file.CsprojFile);
                }
            }

            if (result.NotUsedDeps.Any() && !showShort)
            {
                ok = false;
                ConsoleWriter.WriteWarning("Extra deps:");
                foreach (var m in result.NotUsedDeps)
                    ConsoleWriter.WriteBuildWarning("\t- " + m);
            }

            var owerhead = new SortedSet<string>(result.ConfigOverhead.Where(m => !result.NotUsedDeps.Contains(m)));
            if (owerhead.Any() && !showShort)
            {
                ok = false;
                ConsoleWriter.WriteWarning("Config owerhead:");
                foreach (var m in owerhead)
                    ConsoleWriter.WriteBuildWarning("\t- " + m);
            }

            if (ok)
            {
                ConsoleWriter.WriteOk("No problems with deps");
            }
            else
            {
                if (result.NotInDeps.Any())
                    ConsoleWriter.WriteInfo("See also 'ref fix' command.");
            }
            return 0;
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
    }
}