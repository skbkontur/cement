using System;
using System.Collections.Generic;
using System.IO;

namespace Common.YamlParsers
{
    public class SettingsYamlParser : ConfigurationYamlParser
    {
        public SettingsYamlParser(FileInfo moduleName) : base(moduleName)
        {
        }

        public SettingsYamlParser(string moduleName, string text): base(moduleName, text)
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
                throw new BadYamlException(ModuleName, "settings", exception.Message);
            }
        }

        private static ModuleSettings GetSettingsFromSection(Dictionary<string, object> configDict)
        {
            if (configDict == null || !configDict.ContainsKey("settings"))
                return new ModuleSettings();
            var settingsDict = configDict["settings"] as Dictionary<object, object>;
            if (settingsDict == null)
                return new ModuleSettings();

            return GetSettings(settingsDict);
        }

        private static ModuleSettings GetSettings(Dictionary<object, object> settingsDict)
        {
            var result = new ModuleSettings();
            if (settingsDict.ContainsKey("type") && ((string) settingsDict["type"]).Trim() == "content")
                result.IsContentModule = true;
            return result;
        }
    }

    public class ModuleSettings
    {
        public bool IsContentModule;
    }
}