using System.IO;
using log4net;

namespace Common
{
    public static class NuGetHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NuGetHelper));

        public static string GetNugetRunCommand()
        {
            var nuGetPath = GetNuGetPath();
            if (nuGetPath == null)
                return null;
            var nugetCommand = $"\"{nuGetPath}\"";
            if (Helper.OsIsUnix())
                nugetCommand = $"mono {nugetCommand}";
            return nugetCommand;
        }

        private static string GetNuGetPath()
        {
            var nuget = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "NuGet.exe");
            if (!File.Exists(nuget))
            {
                Log.Error($"NuGet.exe not found in {nuget}");
                nuget = Path.Combine(Helper.CurrentWorkspace, "nuget", "bin", "NuGet.exe");
            }
            if (!File.Exists(nuget))
            {
                Log.Error($"NuGet.exe not found in {nuget}");
                return null;
            }

            Log.Debug($"NuGet.exe found in {nuget}");
            return nuget;
        }
    }
}