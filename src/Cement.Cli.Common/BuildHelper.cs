using System.IO;
using System.Linq;
using System.Threading;
using Cement.Cli.Common.YamlParsers;

namespace Cement.Cli.Common;

public sealed class BuildHelper
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public static BuildHelper Shared { get; } = new();

    private BuildHelper()
    {
    }

    public void RemoveModuleFromBuiltInfo(string moduleName)
    {
        semaphore.Wait();
        try
        {
            var storage = BuildInfoStorage.Deserialize();
            storage.RemoveBuildInfo(moduleName);
            storage.Save();
        }
        finally
        {
            semaphore.Release();
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
