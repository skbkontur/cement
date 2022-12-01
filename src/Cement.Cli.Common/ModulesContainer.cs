using System.Collections.Generic;
using System.Linq;

namespace Cement.Cli.Common;

public sealed class ModulesContainer
{
    private readonly Dictionary<string, IList<DepWithParent>> container;
    private readonly Dictionary<string, IList<string>> proceedConfigs;
    private readonly Dictionary<string, string> currentTreeish;
    private readonly TreeishManager treeishManager;

    public ModulesContainer()
    {
        container = new Dictionary<string, IList<DepWithParent>>();
        proceedConfigs = new Dictionary<string, IList<string>>();
        currentTreeish = new Dictionary<string, string>();
        treeishManager = new TreeishManager();
    }

    public void AddConfig(string moduleName, string config)
    {
        if (!proceedConfigs.ContainsKey(moduleName))
            proceedConfigs[moduleName] = new List<string>();
        proceedConfigs[moduleName].Add(config);
    }

    public IList<string> GetConfigsByName(string moduleName)
    {
        return proceedConfigs.ContainsKey(moduleName)
            ? proceedConfigs[moduleName]
            : new List<string>();
    }

    public void Add(DepWithParent depWithParent)
    {
        if (!container.ContainsKey(depWithParent.Dep.Name))
            container[depWithParent.Dep.Name] = new List<DepWithParent>();
        container[depWithParent.Dep.Name].Add(depWithParent);
    }

    public void ChangeTreeish(string moduleName, string treeish)
    {
        currentTreeish[moduleName] = treeish;
        if (!container.ContainsKey(moduleName))
            return;
        container[moduleName] = container[moduleName].Where(d => d.Dep.Treeish != null).ToList();
    }

    public string GetTreeishByName(string moduleName)
    {
        return currentTreeish.ContainsKey(moduleName) ? currentTreeish[moduleName] : null;
    }

    public IList<Dep> GetDepsByName(string name)
    {
        IList<DepWithParent> result;
        container.TryGetValue(name, out result);
        return result == null ? new List<Dep>() : result.Select(dP => dP.Dep).ToList();
    }

    public bool IsProcessed(Dep dep)
    {
        return container.ContainsKey(dep.Name) && new ConfigurationManager(dep.Name, container[dep.Name].Select(dP => dP.Dep)).ProcessedParent(dep) &&
               treeishManager.TreeishAlreadyProcessed(dep, container[dep.Name].Select(dP => dP.Dep).ToList());
    }

    public void ThrowOnTreeishConflict(DepWithParent depWithParent)
    {
        if (container.ContainsKey(depWithParent.Dep.Name))
            treeishManager.ThrowOnTreeishConflict(depWithParent, container[depWithParent.Dep.Name]);
    }
}
