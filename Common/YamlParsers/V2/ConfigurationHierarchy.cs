using System.Collections.Generic;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class ConfigurationHierarchy
    {
        [CanBeNull]
        private readonly string defaultConfig;
        [NotNull]
        private readonly string[] allConfigsSorted;
        [NotNull]
        private readonly IReadOnlyDictionary<string, string[]> configNameToAllParentsMap;

        public ConfigurationHierarchy(
            [NotNull] string[] allConfigsSorted,
            [NotNull] IReadOnlyDictionary<string, string[]> configNameToAllParentsMap,
            [CanBeNull] string defaultConfig = null)
        {
            this.defaultConfig = defaultConfig;
            this.allConfigsSorted = allConfigsSorted;
            this.configNameToAllParentsMap = configNameToAllParentsMap;
        }

        /// <summary>
        /// Return all ancestors of a given configuration in order:
        /// From smallest config with no deps to complex configs with multiple ancestors.
        /// </summary>
        public string[] GetAllParents(string configName) => configNameToAllParentsMap[configName];


        /// <summary>
        /// Return default configuration name
        /// </summary>
        public string FindDefault() => defaultConfig;


        /// <summary>
        /// Return all configurations in order:
        /// From smallest config with no deps to complex configs with multiple ancestors.
        /// </summary>
        public string[] GetAll() => allConfigsSorted;
    }
}