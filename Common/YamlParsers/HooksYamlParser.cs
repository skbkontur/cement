using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.YamlParsers
{
    public class HooksYamlParser : ConfigurationYamlParser
    {
        public HooksYamlParser(FileInfo moduleName) : base(moduleName)
        {
        }

        public HooksYamlParser(string moduleName, string text) : base(moduleName, text)
        {
        }

        public List<string> Get()
        {
            try
            {
                var configDict = GetConfigurationSection("default");
                return GetHooksFromSection(configDict);
            }
            catch (BadYamlException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new BadYamlException(ModuleName, "hooks", exception.Message);
            }
        }

        private static List<string> GetHooksFromSection(Dictionary<string, object> configDict)
        {
            if (configDict == null || !configDict.ContainsKey("hooks"))
                return new List<string>();
            var hooks = configDict["hooks"] as List<object>;
            if (hooks == null)
                return new List<string>();

            return hooks.Select(h => h.ToString()).ToList();
        }
    }
}