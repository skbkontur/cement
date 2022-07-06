using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Common.Graph
{
    public sealed class ParallelBuilder
    {
        private static readonly ILogger Log = LogManager.GetLogger(typeof(ParallelBuilder));
        public bool IsFailed;

        private readonly Dictionary<Dep, List<Dep>> graph = new Dictionary<Dep, List<Dep>>();
        private readonly AutoResetEvent signal = new AutoResetEvent(true);
        private readonly object sync = new object();

        private readonly List<Dep> waiting = new List<Dep>();
        private readonly HashSet<Dep> building = new HashSet<Dep>();
        private readonly List<Dep> built = new List<Dep>();
        private bool needChecking = true;

        public ParallelBuilder(Dictionary<Dep, List<Dep>> graph)
        {
            foreach (var key in graph.Keys)
            {
                this.graph[key] = GraphHelper.GetChildren(key, graph)
                    .Where(d => d.Name != key.Name)
                    .ToList();
            }

            waiting.AddRange(this.graph.Keys);
        }

        public Dep TryStartBuild()
        {
            while (true)
            {
                if (IsFailed)
                {
                    signal.Set();
                    return null;
                }

                var dep = TryStartOnce(out var finished);
                if (dep != null)
                {
                    signal.Set();
                    return dep;
                }

                if (finished)
                {
                    signal.Set();
                    return null;
                }

                signal.WaitOne();
            }
        }

        public void EndBuild(Dep dep, bool failed = false)
        {
            lock (sync)
            {
                IsFailed |= failed;
                building.Remove(dep);
                built.Add(dep);

                var children = new ConfigurationManager(dep.Name, new Dep[0]).ChildrenConfigurations(dep);
                foreach (var child in children)
                    built.Add(new Dep(dep.Name, null, child));

                needChecking = true;
            }
        }

        private Dep TryStartOnce(out bool finished)
        {
            lock (sync)
            {
                finished = !waiting.Any();

                if (!needChecking)
                {
                    Log.LogInformation("Nothing to build - already checked.");
                    return null;
                }

                foreach (var module in waiting)
                {
                    if (building.Any(m => m.Name == module.Name || graph[m].Any(d => d.Name == module.Name)))
                        continue;

                    var deps = graph[module];
                    if (!deps.All(d => built.Contains(d)))
                        continue;
                    if (deps.Any(d => building.Any(b => d.Name == b.Name)))
                        continue;

                    building.Add(module);
                    waiting.Remove(module);
                    Log.LogInformation($"Building {module} with {building.Count - 1} others.");
                    return module;
                }

                needChecking = false;
            }

            Log.LogInformation("Nothing to build.");
            return null;
        }
    }
}
