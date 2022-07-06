using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.YamlParsers;

namespace Common
{
    public static class InstallHelper
    {
        private static List<string> allInstallFiles;

        public static List<string> GetAllInstallFiles()
        {
            if (allInstallFiles != null)
                return allInstallFiles;

            var modules = Helper.GetModules().Select(m => m.Name).Where(Yaml.Exists).ToList();
            allInstallFiles = modules.SelectMany(GetAllInstallFiles).ToList();
            return allInstallFiles;
        }

        public static List<string> GetAllInstallFiles(string module)
        {
            if (!File.Exists(Path.Combine(Helper.CurrentWorkspace, module, Helper.YamlSpecFile)))
                return new List<string>();
            var configs = Yaml.ConfigurationParser(module).GetConfigurations();
            var result = configs.Select(config => Yaml.InstallParser(module).Get(config)).SelectMany(parser => parser.Artifacts);
            return result.Distinct().Select(file => Path.Combine(module, file)).ToList();
        }
    }
}
