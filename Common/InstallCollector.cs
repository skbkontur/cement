using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.YamlParsers;

namespace Common
{
    public class InstallCollector
    {
        private readonly string path;
        private readonly string moduleName;

        public InstallCollector(string path)
        {
            this.path = path;
            moduleName = Path.GetFileName(path);
        }

        private static void EnqueueRange<T>(Queue<T> queue, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                queue.Enqueue(item);
            }
        }

        public InstallData Get(string configName = null)
        {
            var proceededModules = new HashSet<string>();
            var proceededNuGetPackages = new HashSet<string>();
            if (!File.Exists(Path.Combine(path, Helper.YamlSpecFile)))
                return new InstallData();

            var result = new InstallYamlParser(new FileInfo(path)).Get(configName);
            result.InstallFiles = result.InstallFiles.Select(r => Path.Combine(moduleName, r)).ToList();
            result.Artifacts = result.Artifacts.Select(r => Path.Combine(moduleName, r)).ToList();
            result.CurrentConfigurationInstallFiles = result.CurrentConfigurationInstallFiles.Select(r => Path.Combine(moduleName, r)).ToList();

            proceededModules.Add(Path.GetFileName(path));
            var queue = new Queue<string>(result.ExternalModules);
            while (queue.Count > 0)
            {
                var externalModule = queue.Dequeue();
                proceededModules.Add(externalModule);
                var proceededExternal = ProceedExternalModule(externalModule, proceededModules, proceededNuGetPackages);
                result.InstallFiles.AddRange(proceededExternal.InstallFiles.Where(f => !result.InstallFiles.Contains(f)));
                result.ExternalModules.AddRange(proceededExternal.ExternalModules);
                result.NuGetPackages.AddRange(proceededExternal.NuGetPackages);
                proceededExternal.NuGetPackages.ForEach(m => proceededNuGetPackages.Add(m));
                EnqueueRange(queue, proceededExternal.ExternalModules);
            }
            return result;
        }

        private InstallData ProceedExternalModule(string moduleNameWithConfiguration, HashSet<string> proceededModules, HashSet<string> proceededNuGetPackages)
        {
            var dep = new Dep(moduleNameWithConfiguration);
            var externalModulePath = Path.Combine(path, "..", dep.Name);
            var externalInstallData = new InstallYamlParser(new FileInfo(externalModulePath)).Get(dep.Configuration);
            return new InstallData(
                externalInstallData.InstallFiles
                    .Select(f => Path.Combine(dep.Name, f))
                    .ToList(),
                externalInstallData.ExternalModules
                    .Where(m => !proceededModules.Contains(m))
                    .ToList(),
                externalInstallData.NuGetPackages
                    .Where(m => !proceededNuGetPackages.Contains(m))
                    .ToList());
        }
    }
}