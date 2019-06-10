using System;
using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2.Factories
{
    public static class ConfigurationHierarchyFactory
    {
        private const string defaultConfigName = "full-build";

        public static ConfigurationHierarchy Get(ConfigSectionTitle[] configs)
        {
            // workaround for inheriting non-existent configs
            var existingConfigNames = new HashSet<string>(configs.Select(c => c.Name));
            var adjacencyMap = configs.ToDictionary(
                config => config.Name,
                config => config.Parents?.Where(pc => existingConfigNames.Contains(pc)).ToArray() ?? new string[0]);

            EnsureHasNoCycles(adjacencyMap);

            var all = GetAllBreadthFirst(adjacencyMap);
            var configNameToAllParentsMap = BuildConfigNameToParentsMap(all, adjacencyMap);
            var defaultConfig = DetermineDefaultConfig(configs);

            return new ConfigurationHierarchy(all, configNameToAllParentsMap, defaultConfig?.Name);
        }

        [CanBeNull]
        private static ConfigSectionTitle DetermineDefaultConfig(IReadOnlyList<ConfigSectionTitle> lines)
        {
            var config = lines.FirstOrDefault(l => l.IsDefault) ??
                         lines.FirstOrDefault(l => string.Equals(l.Name, defaultConfigName));

            if (config != null)
                return config;

            return lines.Count == 1 ? lines[0] : null;
        }

        private static void EnsureHasNoCycles(IReadOnlyDictionary<string, string[]> adjacencyMap)
        {
            var state = new TraversalState(adjacencyMap);
            var roots = adjacencyMap
                .Where(kvp => kvp.Value.Length == 0)
                .Select(kvp => kvp.Key)
                .ToArray();

            if (roots.Length == 0)
            {
                var startingNode = adjacencyMap.OrderBy(kvp => kvp.Value.Length).Select(kvp => kvp.Key).FirstOrDefault();
                // no nodes = no cycles
                if (startingNode == null)
                    return;

                EnsureHasNoCycles(startingNode, state);
            }
            else
            {
                foreach (var node in roots)
                {
                    EnsureHasNoCycles(node, state);
                }
            }
        }

        [NotNull]
        private static string[] GetAllBreadthFirst(IReadOnlyDictionary<string, string[]> adjacencyMap)
        {
            var roots = adjacencyMap
                .Where(kvp => kvp.Value == null || !kvp.Value.Any())
                .Select(kvp => kvp.Key)
                .ToArray();

            var yieldedNodes = new HashSet<string>();
            var queue = new Queue<string>(roots);

            while (queue.Any())
            {
                var node = queue.Dequeue();
                yieldedNodes.Add(node);

                var children = adjacencyMap
                    .Where(kvp => kvp.Value != null && kvp.Value.Contains(node))
                    .Select(kvp => kvp.Key);

                foreach (var child in children)
                {
                    if (queue.Contains(child))
                        continue;
                    var parents = adjacencyMap[child];
                    if (parents.Length == 0 || parents.All(p => yieldedNodes.Contains(p)))
                        queue.Enqueue(child);
                }
            }

            return yieldedNodes.ToArray();
        }

        private static void EnsureHasNoCycles(string node, TraversalState state)
        {
            state.Visit(node);
            var nextNodes = state.GetNext(node);

            if (!nextNodes.Any())
            {
                state.Yield(node);
                return;
            }

            foreach (var adjacentNode in nextNodes)
            {
                if (state.IsVisited(adjacentNode) && !state.IsYielded(adjacentNode))
                {
                    var msg = $"Cyclic config dependency detected, visited undeleted node twice: '{adjacentNode}'";
                    throw new BadYamlException("configurations", msg);
                }

                EnsureHasNoCycles(adjacentNode, state);
            }

            state.Yield(node);
        }

        private static Dictionary<string, string[]> BuildConfigNameToParentsMap(string[] all, IReadOnlyDictionary<string, string[]> adjacencyMap)
        {
            var configNameToAllParentsMap = all.ToDictionary(node => node, node => new string[0]);
            foreach (var configName in all)
            {
                var closestParents = adjacencyMap[configName];
                var allParents = new HashSet<string>(closestParents);

                foreach (var parent in closestParents)
                foreach (var grandparent in configNameToAllParentsMap[parent])
                    allParents.Add(grandparent);

                configNameToAllParentsMap[configName] = allParents.OrderBy(i => Array.IndexOf(all, i)).ToArray();
            }

            return configNameToAllParentsMap;
        }

        private class TraversalState
        {
            private readonly IReadOnlyDictionary<string, string[]> adjacencyMap;
            private readonly HashSet<string> yieldedNodesSet = new HashSet<string>();
            private readonly HashSet<string> visitedNodes = new HashSet<string>();

            public TraversalState(IReadOnlyDictionary<string,string[]> adjacencyMap)
            {
                this.adjacencyMap = adjacencyMap;
            }

            public bool IsVisited(string node) => visitedNodes.Contains(node);
            public void Visit(string node) => visitedNodes.Add(node);

            public bool IsYielded(string node) => yieldedNodesSet.Contains(node);
            public void Yield(string node) => yieldedNodesSet.Add(node);

            public string[] GetNext(string node)
            {
                return adjacencyMap
                    .Where(kvp => kvp.Value.Contains(node))
                    .Select(kvp => kvp.Key)
                    .ToArray();
            }
        }

    }
}