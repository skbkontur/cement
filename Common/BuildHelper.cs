using System.IO;
using System.Linq;
using Common.YamlParsers;

namespace Common
{
    public sealed class BuildHelper
    {
        private readonly object lockObject = new();

        public static BuildHelper Shared { get; } = new();

        public void RemoveModuleFromBuiltInfo(string moduleName)
        {
            lock (lockObject)
            {
                var storage = BuildInfoStorage.Deserialize();
                storage.RemoveBuildInfo(moduleName);
                storage.Save();
            }
        }

        public bool HasAllOutput(string moduleName, string configuration, bool requireYaml)
        {
            var path = Path.Combine(Helper.CurrentWorkspace, moduleName, Helper.YamlSpecFile);
            if (!File.Exists(path))
                return !requireYaml;
            var artifacts = Yaml.InstallParser(moduleName).Get(configuration).Artifacts;
            return artifacts!.Select(Helper.FixPath).All(art => File.Exists(Path.Combine(Helper.CurrentWorkspace, moduleName, art)));
        }
    }
}
