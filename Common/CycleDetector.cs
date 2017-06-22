using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace Common
{
    public static class CycleDetector
    {
        private static readonly ISet<string> ModulesInProcessing = new HashSet<string>();
        private static readonly ISet<string> VisitedConfigurations = new HashSet<string>();

        public static void WarnIfCycle(string rootModuleName, string configuration, ILog log)
        {
            log.Info("Looking for cycles");
            ConsoleWriter.WriteProgress("Looking for cycles");
            var cycle = TryFindCycle(rootModuleName +
                                     (configuration == null ? "" : Helper.ConfigurationDelimiter + configuration));
            if (cycle != null)
            {
                log.Warn("Detected cycle in deps:\n" + cycle.Aggregate((x, y) => x + "->" + y));
                ConsoleWriter.WriteWarning("Detected cycle in deps:\n" + cycle.Aggregate((x, y) => x + "->" + y));
            }
            ConsoleWriter.ResetProgress();
        }

        public static List<string> TryFindCycle(string moduleName)
        {
            var cycle = new List<string>();
            ModulesInProcessing.Clear();
            VisitedConfigurations.Clear();
            var cycleFound = Dfs(new Dep(moduleName), cycle);
            cycle.Reverse();
            return cycleFound ? cycle : null;
        }

        private static bool Dfs(Dep dep, List<string> cycle)
        {
            dep.UpdateConfigurationIfNull();
            var depNameAndConfig = dep.Name + Helper.ConfigurationDelimiter + dep.Configuration;
            ModulesInProcessing.Add(depNameAndConfig);
            VisitedConfigurations.Add(depNameAndConfig);
            var deps = new DepsParser(Path.Combine(Helper.CurrentWorkspace, dep.Name)).Get(dep.Configuration).Deps ??
                       new List<Dep>();
            foreach (var d in deps)
            {
                d.UpdateConfigurationIfNull();
                var currentDep = d.Name + Helper.ConfigurationDelimiter + d.Configuration;
                if (ModulesInProcessing.Contains(currentDep))
                {
                    cycle.Add(currentDep);
                    cycle.Add(depNameAndConfig);
                    return true;
                }
                if (VisitedConfigurations.Contains(currentDep))
                    continue;
                if (Dfs(d, cycle))
                {
                    cycle.Add(depNameAndConfig);
                    return true;
                }
            }
            ModulesInProcessing.Remove(depNameAndConfig);
            return false;
        }
    }
}
