using JetBrains.Annotations;

namespace Cement.Cli.Common;

[PublicAPI]
internal static class GlobalLocks
{
    public static readonly object PackageLockObject = new();
}
