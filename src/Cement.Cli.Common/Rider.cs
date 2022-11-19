using System;
using System.Diagnostics;

namespace Cement.Cli.Common;

public static class Rider
{
    private static readonly Lazy<bool> isRunning = new(
        () => Process.GetProcessesByName("rider64").Length != 0);

    public static bool IsRunning => isRunning.Value;
}
