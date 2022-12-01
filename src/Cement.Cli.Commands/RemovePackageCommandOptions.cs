using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class RemovePackageCommandOptions
{
    public RemovePackageCommandOptions(string packageName)
    {
        PackageName = packageName;
    }

    public string PackageName { get; }
}