using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class AddPackageCommandOptions
{
    public AddPackageCommandOptions(string packageName, string packageUrl)
    {
        PackageName = packageName;
        PackageUrl = packageUrl;
    }

    public string PackageName { get; }
    public string PackageUrl { get; }
}