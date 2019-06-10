using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class ModuleDefinition
    {
        public ModuleDefinition(
            [NotNull] IReadOnlyDictionary<string, ModuleConfig> allConfigurations,
            [NotNull] ModuleDefaults defaults)
        {
            Defaults = defaults;
            AllConfigurations = allConfigurations;
            defaultConfig = allConfigurations.FirstOrDefault(kvp => kvp.Value.IsDefault).Value;
        }

        [NotNull]
        public IReadOnlyDictionary<string, ModuleConfig> AllConfigurations { get; }

        [CanBeNull]
        public ModuleConfig FindDefaultConfiguration() => defaultConfig;

        [NotNull]
        public ModuleConfig GetDefaultConfiguration()
        {
            if (defaultConfig == null)
                throw new ArgumentException("Cannot determine default module configuration. Specify it via '*default' keyword.");

            return defaultConfig;
        }

        [NotNull]
        public ModuleDefaults Defaults { get; }

        public ModuleConfig this[string configName] => AllConfigurations[configName];

        private readonly ModuleConfig defaultConfig;
    }
}