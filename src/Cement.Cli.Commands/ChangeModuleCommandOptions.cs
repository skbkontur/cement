using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class ChangeModuleCommandOptions
{
    public ChangeModuleCommandOptions(string moduleName, string pushUrl, string fetchUrl, string packageName)
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
