using System;

namespace Common
{
    public sealed class MsBuildLikeTool
    {
        public MsBuildLikeTool(string path, string version=null, bool isWindowsMsBuild=false)
        {
            Path = path;
            Version = Version.TryParse(version ?? string.Empty, out var v)
                ? v
                : new Version();
            IsWindowsMsBuild = isWindowsMsBuild;
        }

        public string Path { get; }
        public Version Version { get; }
        public bool IsWindowsMsBuild { get; }
    }
}