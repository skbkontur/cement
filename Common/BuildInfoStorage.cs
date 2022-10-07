using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Common;

public sealed class BuildInfoStorage
{
    private readonly BuildPreparer buildPreparer;
    private readonly BuildInfoStorageData data;
    private readonly BuildHelper buildHelper;

    private BuildInfoStorage(BuildPreparer buildPreparer, BuildHelper buildHelper, BuildInfoStorageData data)
    {
        this.buildPreparer = buildPreparer;
        this.buildHelper = buildHelper;
        this.data = data;
    }

    public static BuildInfoStorage Deserialize()
    {
        var buildPreparer = BuildPreparer.Shared;
        var buildHelper = BuildHelper.Shared;
        try
        {
            var data = File.ReadAllText(SerializePath());
            var cfg = new JsonSerializerSettings {ContractResolver = new DictionaryFriendlyContractResolver()};
            var storage = JsonConvert.DeserializeObject<BuildInfoStorageData>(data, cfg);
            return new BuildInfoStorage(buildPreparer, buildHelper, storage ?? new BuildInfoStorageData());
        }
        catch (Exception)
        {
            return new BuildInfoStorage(buildPreparer, buildHelper, new BuildInfoStorageData());
        }
    }

    public void RemoveBuildInfo(string moduleName)
    {
        var keys = data.ModulesWithDeps.Keys.ToList();
        var toRemove = keys.Where(m => data.ModulesWithDeps[m].Any(dep => dep.Dep.Name == moduleName)).ToList();
        foreach (var dep in toRemove)
            data.ModulesWithDeps.Remove(dep);
    }

    public void AddBuiltModule(Dep module, Dictionary<string, string> currentCommitHashes)
    {
        var configs = new ConfigurationParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, module.Name))).GetConfigurations();
        var childConfigs = new ConfigurationManager(module.Name, configs).ProcessedChildrenConfigurations(module);
        childConfigs.Add(module.Configuration);

        foreach (var childConfig in childConfigs)
        {
            var deps = buildPreparer.BuildConfigsGraph(module.Name, childConfig).Keys.ToList();
            var depsWithCommit = deps
                .Where(dep => currentCommitHashes.ContainsKey(dep.Name) && currentCommitHashes[dep.Name] != null)
                .Select(dep => new DepWithCommitHash(dep, currentCommitHashes[dep.Name]))
                .ToList();
            data.ModulesWithDeps[new Dep(module.Name, null, childConfig)] = depsWithCommit;
        }
    }

    public void Save()
    {
        var cfg = new JsonSerializerSettings {ContractResolver = new DictionaryFriendlyContractResolver()};
        var content = JsonConvert.SerializeObject(data, Formatting.Indented, cfg);
        var path = SerializePath();
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, content);
    }

    public List<Dep> GetUpdatedModules(List<Dep> modules, Dictionary<string, string> currentCommitHashes)
    {
        return modules.AsParallel().Where(module => IsModuleUpdate(currentCommitHashes, module)).ToList();
    }

    private static string SerializePath()
    {
        return Path.Combine(Helper.CurrentWorkspace, ".cement", "builtCache");
    }

    private bool IsModuleUpdate(Dictionary<string, string> currentCommitHashes, Dep module)
    {
        return
            !data.ModulesWithDeps.ContainsKey(module) ||
            data.ModulesWithDeps[module].Any(
                dep => !currentCommitHashes.ContainsKey(dep.Dep.Name) ||
                       currentCommitHashes[dep.Dep.Name] != dep.CommitHash ||
                       !buildHelper.HasAllOutput(dep.Dep.Name, dep.Dep.Configuration, false)) ||
            !buildHelper.HasAllOutput(module.Name, module.Configuration, false);
    }
}
