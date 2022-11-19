using System.IO;
using JetBrains.Annotations;

namespace Cement.Cli.Common;

public static class InstallParser
{
    [NotNull]
    public static InstallData Get(string moduleName, string configuration)
    {
        var path = Path.Combine(Helper.CurrentWorkspace, moduleName);
        return new InstallCollector(path, moduleName).Get(configuration);
    }
}
