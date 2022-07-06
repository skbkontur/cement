using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Common
{
    public sealed class CycleDetector
    {
        private readonly ISet<string> modulesInProcessing = new HashSet<string>();
        private readonly ISet<string> visitedConfigurations = new HashSet<string>();

        public void WarnIfCycle(string rootModuleName, string configuration, ILogger log)
        {
            log.LogInformation("Looking for cycles");
            ConsoleWriter.Shared.WriteProgress("Looking for cycles");
            var cycle = TryFindCycle(rootModuleName + (configuration == null ? "" : Helper.ConfigurationDelimiter + configuration));
            if (cycle != null)
            {
                log.LogWarning("Detected cycle in deps:\n" + cycle.Aggregate((x, y) => x + "->" + y));
                ConsoleWriter.Shared.WriteWarning("Detected cycle in deps:\n" + cycle.Aggregate((x, y) => x + "->" + y));
            }

            ConsoleWriter.Shared.ResetProgress();
        }

        public List<string> TryFindCycle(string moduleName)
        {
            var cycle = new List<string>();
            modulesInProcessing.Clear();
            visitedConfigurations.Clear();
            var cycleFound = Dfs(new Dep(moduleName), cycle);
            cycle.Reverse();
            return cycleFound ? cycle : null;
        }

        private bool Dfs(Dep dep, List<string> cycle)
        {
            dep.UpdateConfigurationIfNull();
            var depNameAndConfig = dep.Name + Helper.ConfigurationDelimiter + dep.Configuration;
            modulesInProcessing.Add(depNameAndConfig);
            visitedConfigurations.Add(depNameAndConfig);
            var deps = new DepsParser(Path.Combine(Helper.CurrentWorkspace, dep.Name)).Get(dep.Configuration).Deps ?? new List<Dep>();
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
                if (Dfs(d, cycle))
                {
                    cycle.Add(depNameAndConfig);
                    return true;
                }
            }

            modulesInProcessing.Remove(depNameAndConfig);
            return false;
        }
    }
}
