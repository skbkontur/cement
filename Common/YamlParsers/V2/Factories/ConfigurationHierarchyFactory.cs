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

        public static ConfigurationHierarchy Get(ConfigurationLine[] configs)
        {
            // workaround for inheriting non-existent configs
            var existingConfigNames = new HashSet<string>(configs.Select(c => c.ConfigName));

            var adjacencyMap = configs.ToDictionary(
                config => config.ConfigName,
                config => config.ParentConfigs?.Where(pc => existingConfigNames.Contains(pc)).ToArray());

            var allConfigsSorted = GetAllInner(adjacencyMap);
            var configNameToAllParentsMap = new Dictionary<string, string[]>(allConfigsSorted.Length);

            foreach (var configName in allConfigsSorted)
            {
                var closestParents = adjacencyMap[configName] ?? new string[0];
                var allParents = new HashSet<string>(closestParents);
                foreach (var parent in closestParents)
                {
                    foreach (var grandParent in configNameToAllParentsMap[parent])
                    {
                        allParents.Add(grandParent);
                    }
                }

                configNameToAllParentsMap[configName] = allParents.OrderBy(c => Array.IndexOf(allConfigsSorted, c)).ToArray();
            }

            var defaultConfig = DetermineDefaultConfig(configs);
            return new ConfigurationHierarchy(allConfigsSorted, configNameToAllParentsMap, defaultConfig?.ConfigName);
        }

        [NotNull]
        private static string[] GetAllInner(IReadOnlyDictionary<string, string[]> adjacencyMap)
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

        [CanBeNull]
        private static ConfigurationLine DetermineDefaultConfig(ConfigurationLine[] lines)
        {
            var config = lines.FirstOrDefault(l => l.IsDefault) ??
                         lines.FirstOrDefault(l => string.Equals(l.ConfigName, defaultConfigName));

            if (config != null)
                return config;

            return lines.Length == 1 ? lines[0] : null;
        }
    }
}