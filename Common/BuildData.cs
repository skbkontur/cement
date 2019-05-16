using System.Collections.Generic;

namespace Common
{
    public class BuildData
    {
        public string Target { get; }
        public Tool Tool { get; }
        public string Configuration { get; }
        public List<string> Parameters { get; }
        public string Name { get; }

        public BuildData(string target, string configuration)
        {
            Target = target;
            Configuration = configuration;
        }

        public BuildData(string target, string configuration, Tool tool, List<string> parameters, string name)
        {
            Target = target;
            Configuration = configuration;
            Tool = tool;
            Parameters = parameters;
            Name = name;
        }
    }

    /// <summary>
    /// An assembling tool
    /// </summary>
    public class Tool
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