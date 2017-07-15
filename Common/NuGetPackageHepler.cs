using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using System.Threading;
using NuGet;
using NuGet.CommandLine;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Console = NuGet.CommandLine.Console;

namespace Common
{
    public static class NuGetPackageHepler
    {
        private static readonly string nuGetDownloadAddress = "https://www.nuget.org/api/v2/package/";

        public static void Install(string packageName, string packagesPath, string projectFilePath)
        {
            var msbuildDirectory =
                Path.GetDirectoryName(ModuleBuilderHelper.FindMsBuild(null, "Cement NuGet Package Installer"));
            var projectContext = new ConsoleProjectContext(new Console());
            var projectSystem = new MSBuildProjectSystem(
                msbuildDirectory,
                projectFilePath,
                projectContext);
            var projectFolder = Path.GetDirectoryName(projectFilePath);
            var project = new MSBuildNuGetProject(projectSystem, packagesPath, projectFolder);
            InstallPackageWithDependencies(packageName, project, projectSystem, projectContext);
        }

        private static void InstallPackageWithDependencies(
            string packageName,
            MSBuildNuGetProject project,
            MSBuildProjectSystem projectSystem,
            INuGetProjectContext projectContext)
        {
            var nupkgPath = LoadPackage(packageName);
            var package = new ZipPackage(nupkgPath);

            var mostCompatibleFramework = new FrameworkReducer().GetNearest(
                projectSystem.TargetFramework,
                package.DependencySets.Select(ds => ds.TargetFramework.ToNuGetFramework()));
            var dependencies =
                package.DependencySets.FirstOrDefault(ds =>
                    ds.TargetFramework.ToNuGetFramework().Equals(mostCompatibleFramework));
            if (dependencies != null)
            {
                foreach (var dependency in dependencies.Dependencies)
                {
                    InstallPackageWithDependencies(
                        $"{dependency.Id}/{dependency.VersionSpec}",
                        project,
                        projectSystem,
                        projectContext);
                }
            }

            var packageIdentity = new PackageIdentity(package.Id, new NuGetVersion(package.Version.Version));
            var downloadResourceResult = new DownloadResourceResult(package.GetStream());
            var installSuccess = project
                .InstallPackageAsync(packageIdentity, downloadResourceResult, projectContext, CancellationToken.None)
                .Result;
            if (installSuccess)
                projectSystem.Save();
            else
                ConsoleWriter.WriteWarning($"Nuget package {packageName} not installed");
        }

        private static string LoadPackage(string packageName)
        {
            var client = new WebClient();
            var nupkgFile = Path.GetTempFileName();
            try
            {
                client.DownloadFile(nuGetDownloadAddress + packageName, nupkgFile);
            }
            catch (WebException)
            {
                return null;
            }
            return nupkgFile;
        }

        private static NuGetFramework ToNuGetFramework(this FrameworkName framework)
        {
            return new NuGetFramework(framework.Identifier, framework.Version);
        }
    }
}