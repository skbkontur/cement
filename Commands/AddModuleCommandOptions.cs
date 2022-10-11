using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class AddModuleCommandOptions
{
    public AddModuleCommandOptions(string moduleName, string pushUrl, string fetchUrl, string packageName)
    {
        ModuleName = moduleName;
        PushUrl = pushUrl;
        FetchUrl = fetchUrl;
        PackageName = packageName;
    }

    public string ModuleName { get; }
    public string PushUrl { get; }
    public string FetchUrl { get; }
    public string PackageName { get; }
}
