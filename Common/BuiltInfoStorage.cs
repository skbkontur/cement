using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Common
{
    public class BuiltInfoStorage
    {
        [JsonProperty] private readonly Dictionary<Dep, List<DepWithCommitHash>> modulesWithDeps;

        private BuiltInfoStorage()
        {
            modulesWithDeps = new Dictionary<Dep, List<DepWithCommitHash>>();
        }

        private static string SerializePath()
        {
            return Path.Combine(Helper.CurrentWorkspace, ".cement", "builtCache");
        }

        public static BuiltInfoStorage Deserialize()
        {
            try
            {
                var data = File.ReadAllText(SerializePath());
                var cfg = new JsonSerializerSettings {ContractResolver = new DictionaryFriendlyContractResolver()};
                var storage = JsonConvert.DeserializeObject<BuiltInfoStorage>(data, cfg);
                return storage ?? new BuiltInfoStorage();
            }
            catch (Exception)
            {
                return new BuiltInfoStorage();
            }
        }

        public void RemoveBuildInfo(string moduleName)
        {
            var keys = modulesWithDeps.Keys.ToList();
            var toRemove = keys.Where(m => modulesWithDeps[m].Any(dep => dep.Dep.Name == moduleName)).ToList();
            foreach (var dep in toRemove)
                modulesWithDeps.Remove(dep);
        }

        public void AddBuiltModule(Dep module, Dictionary<string, string> currentCommitHashes)
        {
            var configs = new ConfigurationParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, module.Name))).GetConfigurations();
            var childConfigs = new ConfigurationManager(module.Name, configs).ProcessedChildrenConfigurations(module);
            childConfigs.Add(module.Configuration);

            foreach (var childConfig in childConfigs)
            {
                var deps = BuildPreparer.BuildConfigsGraph(module.Name, childConfig).Keys.ToList();
                var depsWithCommit = deps
                    .Where(dep => currentCommitHashes.ContainsKey(dep.Name) && currentCommitHashes[dep.Name] != null)
                    .Select(dep => new DepWithCommitHash(dep, currentCommitHashes[dep.Name]))
                    .ToList();
                modulesWithDeps[new Dep(module.Name, null, childConfig)] = depsWithCommit;
            }
        }

        public void Save()
        {
            var cfg = new JsonSerializerSettings {ContractResolver = new DictionaryFriendlyContractResolver()};
            var data = JsonConvert.SerializeObject(this, Formatting.Indented, cfg);
            var path = SerializePath();
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, data);
        }

        public List<Dep> GetUpdatedModules(List<Dep> modules, Dictionary<string, string> currentCommitHashes)
        {
            return modules.AsParallel().Where(module => IsModuleUpdate(currentCommitHashes, module)).ToList();
        }

        private bool IsModuleUpdate(Dictionary<string, string> currentCommitHashes, Dep module)
        {
            return
                !modulesWithDeps.ContainsKey(module) ||
                modulesWithDeps[module].Any(
                    dep => !currentCommitHashes.ContainsKey(dep.Dep.Name) ||
                           currentCommitHashes[dep.Dep.Name] != dep.CommitHash ||
                           !BuiltHelper.HasAllOutput(dep.Dep.Name, dep.Dep.Configuration, false)) ||
                !BuiltHelper.HasAllOutput(module.Name, module.Configuration, false);
        }
    }
}