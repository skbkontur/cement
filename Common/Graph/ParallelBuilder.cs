using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Common.Graph
{
    public sealed class ParallelBuilder
    {

        private readonly ILogger logger;
        private readonly GraphHelper graphHelper;

        private readonly Dictionary<Dep, List<Dep>> graph = new();
        private readonly AutoResetEvent signal = new(true);
        private readonly SemaphoreSlim semaphore = new(1, 1);

        private readonly List<Dep> waiting = new();
        private readonly HashSet<Dep> building = new();
        private readonly List<Dep> built = new();
        private bool needChecking = true;

        public ParallelBuilder(ILogger<ParallelBuilder> logger, GraphHelper graphHelper)

        {
            this.logger = logger;
            this.graphHelper = graphHelper;
        }

        public bool IsFailed { get; private set; }

        public void Initialize(Dictionary<Dep, List<Dep>> source)
        {
            foreach (var key in source.Keys)
            {
                graph[key] = graphHelper.GetChildren(key, source)
                    .Where(d => d.Name != key.Name)
                    .ToList();
            }

            waiting.AddRange(graph.Keys);
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
            semaphore.Wait();
            try
            {
                IsFailed |= failed;
                building.Remove(dep);
                built.Add(dep);

                var children = new ConfigurationManager(dep.Name, new Dep[0]).ChildrenConfigurations(dep);
                foreach (var child in children)
                    built.Add(new Dep(dep.Name, null, child));

                needChecking = true;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private Dep TryStartOnce(out bool finished)
        {
            semaphore.Wait();
            try
            {
                finished = !waiting.Any();

                if (!needChecking)
                {
                    logger.LogInformation("Nothing to build - already checked.");
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
                    logger.LogInformation($"Building {module} with {building.Count - 1} others.");
                    return module;
                }

                needChecking = false;
            }
            finally
            {
                semaphore.Release();
            }

            logger.LogInformation("Nothing to build.");
            return null;
        }
    }
}
