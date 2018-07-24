using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.YamlParsers;
using log4net;

namespace Common
{
    public class BuildPreparer
    {
        private readonly ILog log;

        public BuildPreparer(ILog log)
        {
            this.log = log;
        }

        public void GetModulesOrder(string moduleName, string configuration, out List<Dep> topSortedVertices, out List<Dep> updatedModules, out Dictionary<string, string> currentCommitHashes)
        {
            log.Debug("Building configurations graph");
            ConsoleWriter.WriteProgress("Building configurations graph");
            var configsGraph = BuildConfigsGraph(moduleName, configuration);
            configsGraph = EraseExtraChildren(configsGraph);
            topSortedVertices = GetTopologicallySortedGraph(configsGraph, moduleName, configuration);
            
            log.Debug("Getting current commit hashes");
            ConsoleWriter.WriteProgress("Getting current commit hashes");
            currentCommitHashes = GetCurrentCommitHashes(configsGraph);
            updatedModules = BuiltInfoStorage.Deserialize().GetUpdatedModules(topSortedVertices, currentCommitHashes);
            ConsoleWriter.ResetProgress();
        }

        private static Dictionary<Dep, List<Dep>> EraseExtraChildren(Dictionary<Dep, List<Dep>> configsGraph)
        {
            var vertices = configsGraph.Select(e => e.Key).ToList();
            var deletedChildren = new List<Dep>();
            foreach (var parrent in vertices)
            {
                if (deletedChildren.Contains(parrent))
                    continue;
                var hierarchyManager = new ConfigurationManager(parrent.Name, vertices.Where(v => v.Name == parrent.Name).ToArray());
                var childrenConfigurations = hierarchyManager.ProcessedChildrenConfigurations(parrent);
                foreach (var childConfig in childrenConfigurations)
                {
                    var child = new Dep(parrent.Name, null, childConfig);
                    if (deletedChildren.Contains(child))
                        continue;
                    var configsGraph2 = EraseChild(configsGraph, child, parrent);
                    try
                    {
                        GetTopologicallySortedGraph(configsGraph2, parrent.Name, parrent.Configuration, printCycle: false);
                        configsGraph = configsGraph2;
                        deletedChildren.Add(child);
                    }
                    catch (Exception)
                    {
                        // cycle
                    }
                }
            }
            return configsGraph;
        }

        private static Dictionary<Dep, List<Dep>> EraseChild(Dictionary<Dep, List<Dep>> configsGraph, Dep child, Dep parrent)
        {
            var result = new Dictionary<Dep, List<Dep>>();
            foreach (var kvp in configsGraph)
            {
                if (kvp.Key.Equals(child))
                    continue;
                var deps = kvp.Value.Select(to => to.Equals(child) ? parrent : to).ToList();
                result.Add(kvp.Key, deps);
            }
            return result;
        }

        private Dictionary<string, string> GetCurrentCommitHashes(Dictionary<Dep, List<Dep>> configsGraph)
        {
            var deps = configsGraph.Keys.Select(d => d.Name).Distinct().ToList();

            var result = deps.AsParallel()
                .Select(d => new KeyValuePair<string, string>(d, GetCurrentCommitHash(d)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return result;
        }

        private string GetCurrentCommitHash(string moduleName)
        {
            try
            {
                var repo = new GitRepository(moduleName, Helper.CurrentWorkspace, log);
                return repo.CurrentLocalCommitHash();
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteWarning($"Failed to retrieve local commit hash for '{moduleName}': {e}");
                return null;
            }
        }

        public static List<Dep> GetTopologicallySortedGraph(Dictionary<Dep, List<Dep>> graph, string root, string config, bool printCycle = true)
        {
            var visitedConfigurations = new HashSet<Dep>();
            var processingConfigs = new List<Dep>();
            var result = new List<Dep>();
            var rootDep = new Dep(root, null, config);
            TopSort(rootDep, graph, visitedConfigurations, processingConfigs, result, printCycle);
            return result;
        }

        private static void TopSort(Dep dep, Dictionary<Dep, List<Dep>> graph, ISet<Dep> visitedConfigurations, List<Dep> processingConfigs, List<Dep> result, bool printCycle)
        {
            dep.UpdateConfigurationIfNull();
            visitedConfigurations.Add(dep);
            processingConfigs.Add(dep);

            foreach (var d in graph[dep])
            {
                d.UpdateConfigurationIfNull();
                if (processingConfigs.Contains(d))
                {
                    if (!printCycle)
                        throw new CementException("Unable to build! Circular dependency found!");

                    while (!processingConfigs.First().Equals(d))
                        processingConfigs = processingConfigs.Skip(1).ToList();
                    processingConfigs.Add(d);
                    Console.WriteLine(String.Join(" ->\n", processingConfigs));
                    throw new CementException("Unable to build! Circular dependency found!");
                }
                if (!visitedConfigurations.Contains(d))
                {
                    TopSort(d, graph, visitedConfigurations, processingConfigs, result, printCycle);
                }
            }

            processingConfigs.Remove(dep);
            result.Add(dep);
        }

        public static Dictionary<Dep, List<Dep>> BuildConfigsGraph(string moduleName, string config)
        {
            var graph = new Dictionary<Dep, List<Dep>>();
            var visitedConfigurations = new HashSet<Dep>();
            Dfs(new Dep(moduleName, null, config), graph, visitedConfigurations);
            return graph;
        }

        private static readonly Dictionary<string, bool> DepConfigurationExistsCache = new Dictionary<string, bool>();
        private static void CheckAndUpdateDepConfiguration(Dep dep)
        {
            dep.UpdateConfigurationIfNull();
            var key = dep.ToString();
            if (!DepConfigurationExistsCache.ContainsKey(key))
            {
                if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, dep.Name)))
                {
                    throw new CementBuildException("Failed to find module '" + dep.Name + "'");
                }
                DepConfigurationExistsCache[key] = !Yaml.Exists(dep.Name) ||
                    Yaml.ConfigurationParser(dep.Name).ConfigurationExists(dep.Configuration);
            }
            if (!DepConfigurationExistsCache[key])
            {
                ConsoleWriter.WriteWarning(
                    $"Configuration '{dep.Configuration}' was not found in {dep.Name}. Will take full-build config");
                dep.Configuration = "full-build";
            }
        }

        private static void Dfs(Dep dep, Dictionary<Dep, List<Dep>> graph, HashSet<Dep> visitedConfigurations)
        {
            CheckAndUpdateDepConfiguration(dep);
            visitedConfigurations.Add(dep);
            graph[dep] = new List<Dep>();
            var currentDeps = new DepsParser(Path.Combine(Helper.CurrentWorkspace, dep.Name)).Get(dep.Configuration).Deps ?? new List<Dep>();
            currentDeps = currentDeps.Select(d => new Dep(d.Name, null, d.Configuration)).ToList();
            foreach (var d in currentDeps)
            {
                d.UpdateConfigurationIfNull();
                graph[dep].Add(d);
                if (!visitedConfigurations.Contains(d))
                {
                    Dfs(d, graph, visitedConfigurations);
                }
            }
        }
    }
}