using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class ModuleDefinition
    {
        public ModuleDefinition(
            [NotNull] IReadOnlyDictionary<string, ModuleConfiguration> allConfigurations,
            [NotNull] ModuleDefaults defaults)
        {
            Defaults = defaults;
            AllConfigurations = allConfigurations;
            defaultConfiguration = allConfigurations.FirstOrDefault(kvp => kvp.Value.IsDefault).Value;
        }

        [NotNull]
        public IReadOnlyDictionary<string, ModuleConfiguration> AllConfigurations { get; }

        [CanBeNull]
        public ModuleConfiguration FindDefaultConfiguration() => defaultConfiguration;

        [NotNull]
        public ModuleConfiguration GetDefaultConfiguration()
        {
            if (defaultConfiguration == null)
                throw new ArgumentException("Cannot determine default module configuration. Specify it via '*default' keyword.");

            return defaultConfiguration;
        }

        [NotNull]
        public ModuleDefaults Defaults { get; }

        public ModuleConfiguration this[string configName] => AllConfigurations[configName];

        private readonly ModuleConfiguration defaultConfiguration;
    }
}