using System.Collections.Generic;
using System.Linq;

namespace Common
{
	public class DepWithParent
	{
		public Dep Dep { get; }
		public string ParentModule { get; }

		public DepWithParent(Dep dep, string parentModule)
		{
			Dep = dep;
			ParentModule = parentModule;
		}
	}

    public class DepsQueue
    {
        private readonly Queue<DepWithParent> queue;

        public DepsQueue()
        {
            queue = new Queue<DepWithParent>();
        }

        public bool IsEmpty()
        {
            return !queue.Any();
        }

        public DepWithParent Pop()
        {
            return queue.Dequeue();
        }

        public void Add(DepWithParent dep)
        {
            queue.Enqueue(dep);
        }

        public void AddRange(IList<Dep> deps, string parentModule = null)
        {
            if (deps == null)
                return;
            foreach (var dep in deps)
                queue.Enqueue(new DepWithParent(dep, parentModule));
        }

        public void AddRange(IList<DepWithParent> deps)
        {
            if (deps == null)
                return;
            foreach (var dep in deps)
                queue.Enqueue(dep);
        }
    }

    public class ModulesContainer
    {
        private readonly Dictionary<string, IList<DepWithParent>> container;
        private readonly Dictionary<string, IList<string>> proceedConfigs;
        private readonly Dictionary<string, string> currentTreeish;
	    private readonly HashSet<string> needSrc; 

        public ModulesContainer()
        {
            container = new Dictionary<string, IList<DepWithParent>>();
			proceedConfigs = new Dictionary<string, IList<string>>();
			currentTreeish = new Dictionary<string, string>();
			needSrc = new HashSet<string>();
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
                   TreeishManager.TreeishAlreadyProcessed(dep, container[dep.Name].Select(dP => dP.Dep).ToList());
        }

        public void ThrowOnTreeishConflict(DepWithParent depWithParent)
        {
            if (container.ContainsKey(depWithParent.Dep.Name))
                TreeishManager.ThrowOnTreeishConflict(depWithParent, container[depWithParent.Dep.Name]);
        }

	    public void SetNeedSrc(string moduleName)
	    {
		    needSrc.Add(moduleName);
	    }

	    public bool IsNeedSrc(string moduleName)
	    {
		    return needSrc.Contains(moduleName);
	    }
    }

	public static class TreeishManager
    {
        public static bool TreeishAlreadyProcessed(Dep dep, IList<Dep> processed)
        {
            return
                processed.Select(d => d.Treeish)
                    .Any(
                        dtreeish =>
                            (dtreeish == null && dep.Treeish == null) ||
                            (dtreeish != null && (dtreeish.Equals(dep.Treeish) || dep.Treeish == null)));
        }

        public static void ThrowOnTreeishConflict(DepWithParent depWithParent, IList<DepWithParent> processed)
        {
            var conflictDep =
                processed.FirstOrDefault(d => d.Dep.Treeish != null && !d.Dep.Treeish.Equals(depWithParent.Dep.Treeish));
            if (conflictDep != null && depWithParent.Dep.Treeish != null && conflictDep.Dep.Treeish != null && !conflictDep.Dep.Treeish.Trim().Equals(""))
            {
                throw new TreeishConflictException(
                    string.Format("Treeish conflict: {0} refers to {4}:{1}, while {2} refers to {4}:{3}",
					depWithParent.ParentModule, depWithParent.Dep.Treeish, conflictDep.ParentModule, conflictDep.Dep.Treeish, conflictDep.Dep.Name));
            }
        }

    }


}
