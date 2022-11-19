using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Cement.Cli.Common.YamlParsers.Models;

public sealed class ModuleDefinition
{
    private readonly ModuleConfig defaultConfig;

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

    [NotNull]
    public ModuleDefaults Defaults { get; }

    [CanBeNull]
    public ModuleConfig FindDefaultConfiguration() => defaultConfig;

    [NotNull]
    public ModuleConfig GetDefaultConfiguration()
    {
        if (defaultConfig == null)
            throw new ArgumentException("Cannot determine default module configuration. Specify it via '*default' keyword.");

        return defaultConfig;
    }

    public ModuleConfig this[string configName] => AllConfigurations[configName];
}
