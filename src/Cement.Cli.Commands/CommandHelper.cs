using System.IO;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;

namespace Cement.Cli.Commands;

public static class CommandHelper
{
    public static void SetWorkspace(CommandLocation commandLocation)
    {
        var cwd = Directory.GetCurrentDirectory();
        if (commandLocation == CommandLocation.WorkspaceDirectory)
        {
            if (!Helper.IsCementTrackedDirectory(cwd))
                throw new CementTrackException(cwd + " is not cement workspace directory.");
            Helper.SetWorkspace(cwd);
        }

        if (commandLocation == CommandLocation.RootModuleDirectory)
        {
            if (!Helper.IsCurrentDirectoryModule(cwd))
                throw new CementTrackException(cwd + " is not cement module directory.");
            Helper.SetWorkspace(Directory.GetParent(cwd).FullName);
        }

        if (commandLocation == CommandLocation.InsideModuleDirectory)
        {
            var currentModuleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
            if (currentModuleDirectory == null)
                throw new CementTrackException("Can't locate module directory");
            Helper.SetWorkspace(Directory.GetParent(currentModuleDirectory).FullName);
        }
    }

    public static void CheckRequireYaml(CommandLocation commandLocation, bool requireModuleYaml = false)
    {
        if (commandLocation == CommandLocation.RootModuleDirectory && requireModuleYaml && !File.Exists(Helper.YamlSpecFile))
            throw new CementException("No " + Helper.YamlSpecFile + " file found");
    }
}
