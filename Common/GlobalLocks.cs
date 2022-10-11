using JetBrains.Annotations;

namespace Common;

[PublicAPI]
public static class GlobalLocks
{
    public static readonly object PackageLockObject = new();
}
