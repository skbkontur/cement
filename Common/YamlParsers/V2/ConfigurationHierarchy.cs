using System;
using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class ConfigurationHierarchy
    {
        private const string defaultConfigName = "full-build";
        private readonly ConfigurationLine[] configs;
        private readonly ConfigurationLine defaultConfig;
        private readonly IReadOnlyDictionary<string, string[]> configNameToParentsMap;

        private string[] getAllCached;

        public ConfigurationHierarchy(ConfigurationLine[] configs)
        {
            this.configs = configs;
            defaultConfig = DetermineDefaultConfig(configs);
            configNameToParentsMap = configs.ToDictionary(config => config.ConfigName, config => config.ParentConfigs);
        }

        /// <summary>
        /// Return all ancestors of a given configuration
        /// </summary>
        [CanBeNull]
        public string[] FindAllParents(string configName)
        {
            if (configNameToParentsMap[configName] == null)
                return null;

            var queue = new Queue<string>();
            queue.Enqueue(configName);

            var parents = new HashSet<string>();

            while (queue.Any())
            {
                var current = queue.Dequeue();
                if (configNameToParentsMap[current] == null)
                    continue;

                foreach (var parent in configNameToParentsMap[current])
                {
                    queue.Enqueue(parent);
                    if (!parents.Contains(parent))
                        parents.Add(parent);
                }
            }
            return parents.ToArray();
        }

        /// <summary>
        /// Return closest ancestors of a given configuration
        /// </summary>
        [CanBeNull]
        public string[] FindClosestParents(string configName)
        {
            return configNameToParentsMap[configName];
        }

        /// <summary>
        /// Return default configuration name
        /// </summary>
        [NotNull]
        public string GetDefault()
        {
            return defaultConfig.ConfigName;
        }

        /// <summary>
        /// Return all configurations in order:
        /// From smallest config with no deps to complex configs with multiple ancestors.
        /// </summary>
        [NotNull]
        public string[] GetAll()
        {
            return getAllCached ?? (getAllCached = GetAllInner().ToArray());
        }

        [NotNull]
        private string[] GetAllInner()
        {
            var roots = configNameToParentsMap
                .Where(kvp => kvp.Value == null)
                .Select(kvp => kvp.Key)
                .ToArray();

            var yieldedNodes = new HashSet<string>();
            var queue = new Queue<string>();
            foreach(var root in roots)
                queue.Enqueue(root);

            while (queue.Any())
            {
                var node = queue.Dequeue();
                yieldedNodes.Add(node);

                var children = configNameToParentsMap
                    .Where(kvp => kvp.Value != null && kvp.Value.Contains(node))
                    .Select(kvp => kvp.Key);

                foreach (var child in children)
                {
                    if (queue.Contains(child))
                        continue;
                    var parents = configNameToParentsMap[child];
                    if (parents.Length == 0 || parents.All(p => yieldedNodes.Contains(p)))
                        queue.Enqueue(child);
                }
            }

            return yieldedNodes.ToArray();
        }

        [NotNull]
        private static ConfigurationLine DetermineDefaultConfig(ConfigurationLine[] lines)
        {
            var config = lines.FirstOrDefault(l => l.IsDefault) ??
                         lines.FirstOrDefault(l => string.Equals(l.ConfigName, defaultConfigName));

            if (config != null)
                return config;

            if (lines.Length == 1)
                return lines[0];

            throw new ArgumentException("Cannot determine default module configuration. Specify it via '*default' keyword.", nameof(configs));
        }
    }
}