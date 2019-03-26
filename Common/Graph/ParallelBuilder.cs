using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common.Graph
{
    public class ParallelBuilder
    {
        private readonly Dictionary<Dep, List<Dep>> graph;
        private readonly AutoResetEvent signal = new AutoResetEvent(true);
        public bool IsFailed;
        private readonly object sync = new object();

        private readonly List<Dep> waiting = new List<Dep>();
        private readonly HashSet<Dep> building = new HashSet<Dep>();
        private readonly List<Dep> built = new List<Dep>();

        public ParallelBuilder(Dictionary<Dep, List<Dep>> graph)
        {
            this.graph = graph.ToDictionary(x => x.Key, x => x.Value);
            waiting.AddRange(graph.Keys);
        }

        public Dep TryStartBuild()
        {
            while (true)
            {
                if (IsFailed)
                    return null;

                var dep = TryStartOnce(out var finished);
                if (dep != null)
                    return dep;

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
            }

            signal.Set();
        }

        private Dep TryStartOnce(out bool finished)
        {
            lock (sync)
            {
                finished = !waiting.Any();

                foreach (var module in waiting)
                {
                    if (building.Any(m => graph[m].Any(d => d.Name == module.Name)))
                        continue;

                    var deps = graph[module];
                    if (!deps.All(d => built.Contains(d)))
                        continue;
                    if (deps.Any(d => building.Any(b => d.Name == b.Name)))
                        continue;

                    building.Add(module);
                    waiting.Remove(module);
                    return module;
                }
            }

            return null;
        }
    }
}