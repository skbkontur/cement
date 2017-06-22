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
            if (!File.Exists(Path.Combine(path, Helper.YamlSpecFile)))
                return new InstallData();

            var result = new InstallYamlParser(new FileInfo(path)).Get(configName);
            result.BuildFiles = result.BuildFiles.Select(r => Path.Combine(moduleName, r)).ToList();
            result.Artifacts = result.Artifacts.Select(r => Path.Combine(moduleName, r)).ToList();
            result.MainConfigBuildFiles = result.MainConfigBuildFiles.Select(r => Path.Combine(moduleName, r)).ToList();

            proceededModules.Add(Path.GetFileName(path));
            var queue = new Queue<string>();
            EnqueueRange(queue, result.ExternalModules);
            while (queue.Count > 0)
            {
                var externalModule = queue.Dequeue();
                proceededModules.Add(externalModule);
                var proceededExternal = ProceedExternalModule(externalModule, proceededModules);
                result.BuildFiles.AddRange(proceededExternal.BuildFiles.Where(f => !result.BuildFiles.Contains(f)));
                result.ExternalModules.AddRange(proceededExternal.ExternalModules);
                EnqueueRange(queue, proceededExternal.ExternalModules);
            }
            return result;
        }

        private InstallData ProceedExternalModule(string moduleNameWithConfiguration, HashSet<string> proceededModules)
        {
            var dep = new Dep(moduleNameWithConfiguration);
            var externalModulePath = Path.Combine(path, "..", dep.Name);
            var externalInstallData = new InstallYamlParser(new FileInfo(externalModulePath)).Get(dep.Configuration);
            return new InstallData(
                externalInstallData.BuildFiles
                    .Select(f => Path.Combine(dep.Name, f))
                    .ToList(),
                externalInstallData.ExternalModules
                    .Where(m => !proceededModules.Contains(m))
                    .ToList());
        }
    }
}