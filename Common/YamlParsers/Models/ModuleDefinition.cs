using System.Collections.Generic;
using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class ModuleDefinition
    {
        public ModuleDefinition([NotNull] Dictionary<string, ModuleConfiguration> configurations)
        {
            Configurations = configurations;
        }

        [NotNull]
        public Dictionary<string, ModuleConfiguration> Configurations {get; set;}

        [CanBeNull]
        public ModuleDefaults Defaults {get; set;}
    }
}