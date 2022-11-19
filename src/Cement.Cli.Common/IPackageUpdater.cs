using JetBrains.Annotations;

namespace Cement.Cli.Common;

[PublicAPI]
public interface IPackageUpdater
{
    void UpdatePackages();
}
