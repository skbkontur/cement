using System.Collections.Generic;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class SettingsSectionParser
    {
        [NotNull]
        public ModuleSettings Parse([CanBeNull] object settingsSection)
        {
            if (settingsSection == null)
                return new ModuleSettings();

            var settingsDict = settingsSection as Dictionary<object, object>;
            if (settingsDict == null)
                return new ModuleSettings();

            var isContentModule = settingsDict.ContainsKey("type") && ((string) settingsDict["type"]).Trim() == "content";
            return new ModuleSettings()
            {
                IsContentModule = isContentModule
            };
        }
    }
}