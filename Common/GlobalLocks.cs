using JetBrains.Annotations;

namespace Common;

[PublicAPI]
internal static class GlobalLocks
{
    public static readonly object PackageLockObject = new();
}
