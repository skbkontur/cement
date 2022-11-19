using System;

namespace Cement.Cli.Common;

public static class Platform
{
    public static bool IsUnix()
    {
        return OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
    }
}
