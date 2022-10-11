using JetBrains.Annotations;

namespace Common;

[PublicAPI]
public interface IPackageUpdater
{
    void UpdatePackages();
}
