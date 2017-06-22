using System.IO;
using System.Linq;
using Common.YamlParsers;

namespace Common
{
    public static class BuiltHelper
    {
        private static readonly object LockObject = new object();

        public static void RemoveModuleFromBuiltInfo(string moduleName)
        {
            lock (LockObject)
            {
                var storage = BuiltInfoStorage.Deserialize();
                storage.RemoveBuildInfo(moduleName);
                storage.Save();
            }
        }

        public static bool HasAllOutput(string moduleName, string configuration, bool requireYaml)
        {
            var path = Path.Combine(Helper.CurrentWorkspace, moduleName, Helper.YamlSpecFile);
            if (!File.Exists(path))
                return !requireYaml;
            var artifacts = Yaml.InstallParser(moduleName).Get(configuration).Artifacts;
            return artifacts.Select(Helper.FixPath).All(art => File.Exists(Path.Combine(Helper.CurrentWorkspace, moduleName, art)));
        }
    }
}
