using System;
using System.Collections.Generic;
using System.IO;
using Cement.Cli.Common.Exceptions;

namespace Cement.Cli.Common.YamlParsers;

public sealed class SettingsYamlParser : ConfigurationYamlParser
{
    private const string SectionName = "settings";

    public SettingsYamlParser(FileInfo moduleName)
        : base(moduleName)
    {
    }

    public SettingsYamlParser(string moduleName, string text)
        : base(moduleName, text)
    {
    }

    public ModuleSettings Get()
    {
        try
        {
            var configDict = GetConfigurationSection("default");
            return GetSettingsFromSection(configDict);
        }
        catch (BadYamlException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new BadYamlException(ModuleName, SectionName, exception.Message);
        }
    }

    private static ModuleSettings GetSettingsFromSection(Dictionary<string, object> configDict)
    {
        if (configDict == null || !configDict.ContainsKey(SectionName))
            return new ModuleSettings();
        if (configDict[SectionName] is not Dictionary<object, object> settingsDict)
            return new ModuleSettings();

        return GetSettings(settingsDict);
    }

    private static ModuleSettings GetSettings(Dictionary<object, object> settingsDict)
    {
        return new ModuleSettings
        {
            IsContentModule = settingsDict.TryGetValue("type", out var type) && ((string)type).Trim() == "content",
        };
    }
}
