using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common.DepsValidators;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Common;

public sealed class CycleDetector
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IDepsValidatorFactory depsValidatorFactory;

    public CycleDetector(ConsoleWriter consoleWriter, IDepsValidatorFactory depsValidatorFactory)
    {
        this.consoleWriter = consoleWriter;
        this.depsValidatorFactory = depsValidatorFactory;
    }

    public void WarnIfCycle(string rootModuleName, string configuration, ILogger log)
    {
        log.LogInformation("Looking for cycles");
        consoleWriter.WriteProgress("Looking for cycles");
        var cycle = TryFindCycle(rootModuleName + (configuration == null ? "" : Helper.ConfigurationDelimiter + configuration));
        if (cycle != null)
        {
            log.LogWarning("Detected cycle in deps:\n" + cycle.Aggregate((x, y) => x + "->" + y));
            consoleWriter.WriteWarning("Detected cycle in deps:\n" + cycle.Aggregate((x, y) => x + "->" + y));
        }

        consoleWriter.ResetProgress();
    }

    public List<string> TryFindCycle(string moduleName)
    {
        var cycle = new List<string>();

        var modulesInProcessing = new HashSet<string>();
        var visitedConfigurations = new HashSet<string>();

        var cycleFound = Dfs(modulesInProcessing, visitedConfigurations, new Dep(moduleName), cycle);
        cycle.Reverse();
        return cycleFound ? cycle : null;
    }

    private bool Dfs(ISet<string> modulesInProcessing, ISet<string> visitedConfigurations, Dep dep, List<string> cycle)
    {
        dep.UpdateConfigurationIfNull();
        var depNameAndConfig = dep.Name + Helper.ConfigurationDelimiter + dep.Configuration;
        modulesInProcessing.Add(depNameAndConfig);
        visitedConfigurations.Add(depNameAndConfig);

        var deps = new DepsParser(consoleWriter, depsValidatorFactory, Path.Combine(Helper.CurrentWorkspace, dep.Name))
            .Get(dep.Configuration).Deps ?? new List<Dep>();

        foreach (var d in deps)
        {
            d.UpdateConfigurationIfNull();
            var currentDep = d.Name + Helper.ConfigurationDelimiter + d.Configuration;
            if (modulesInProcessing.Contains(currentDep))
            {
                cycle.Add(currentDep);
                cycle.Add(depNameAndConfig);
                return true;
            }

            if (visitedConfigurations.Contains(currentDep))
                continue;
            if (Dfs(modulesInProcessing, visitedConfigurations, d, cycle))
            {
                cycle.Add(depNameAndConfig);
                return true;
            }
        }

        modulesInProcessing.Remove(depNameAndConfig);
        return false;
    }
}
