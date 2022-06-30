namespace Common
{
    /// <summary>
    /// An assembling tool
    /// </summary>
    public sealed class Tool
    {
        public Tool(string name, string version = null)
        {
            Name = name;
            Version = version;
        }

        public Tool()
        {
        }

        // msbuild - for MSBuild.exe at Windows and xbuild at *nix. dotnet - for new .NET Core
        public string Name { get; set; }

        // assembling tool version, the latest at default
        public string Version { get; set; }
    }
}