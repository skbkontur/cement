using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;
using NuGet.CommandLine;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Console = NuGet.CommandLine.Console;
using PackageDownloader = NuGet.PackageManagement.PackageDownloader;
using PackageSourceProvider = NuGet.Configuration.PackageSourceProvider;
using Settings = NuGet.Configuration.Settings;

namespace Common
{
    public static class NuGetPackageHepler
    {
        private static readonly ILog Log = LogManager.GetLogger("NuGetPackageHelper");

        private class NuGetProject
        {
            private readonly List<string> packagesList;
            private readonly Console logger;
            private readonly MSBuildNuGetProject project;
            private readonly ConsoleProjectContext projectContext;
            private readonly MSBuildProjectSystem projectSystem;
            private readonly List<SourceRepository> repositories;
            private readonly HashSet<PackageIdentity> installedPackages;

            public NuGetProject(List<string> packagesList, string packagesPath, string projectFilePath)
            {
                this.packagesList = packagesList;
                installedPackages = new HashSet<PackageIdentity>();
                var sourceProvider = new PackageSourceProvider(Settings.LoadDefaultSettings(null));
                var sourceRepositoryProvider = new CommandLineSourceRepositoryProvider(sourceProvider);
                repositories = sourceProvider.LoadPackageSources().Select(sourceRepositoryProvider.CreateRepository)
                    .ToList();
                logger = new Console();

                var msbuildDirectory =
                    Path.GetDirectoryName(ModuleBuilderHelper.FindMsBuild(null, "Cement NuGet Package Installer"));
                projectContext = new ConsoleProjectContext(logger);
                projectSystem = new MSBuildProjectSystem(
                    msbuildDirectory,
                    projectFilePath,
                    projectContext);
                var projectFolder = Path.GetDirectoryName(projectFilePath);
                project = new MSBuildNuGetProject(projectSystem, packagesPath, projectFolder);
            }

            public void Install()
            {
                using (var sourceCacheContext = new SourceCacheContext())
                {
                    var packageDownloadContext = new PackageDownloadContext(sourceCacheContext);
                    foreach (var packageName in packagesList)
                    {
                        var package = ParsePackage(packageName);
                        InstallPackageWithDependencies(package, packageDownloadContext);
                    }
                }
            }

            private void InstallPackageWithDependencies(PackageIdentity package,
                PackageDownloadContext packageDownloadContext)
            {
                Log.Info($"Loading package {package}");
                var downloadResourceResult = LoadPackage(package, packageDownloadContext);
                var dependencyGroups = downloadResourceResult.PackageReader.GetPackageDependencies().ToList();
                var mostCompatibleFramework = new FrameworkReducer().GetNearest(
                    projectSystem.TargetFramework,
                    dependencyGroups.Select(dg => dg.TargetFramework));
                var dependencyGroup = dependencyGroups.FirstOrDefault(ds =>
                    ds.TargetFramework.Equals(mostCompatibleFramework));
                if (dependencyGroup != null)
                {
                    foreach (var dependency in dependencyGroup.Packages)
                    {
                        var dependencyIdentity = new PackageIdentity(dependency.Id,
                            NuGetVersion.Parse(dependency.VersionRange.MinVersion.ToFullString()));
                        Log.Info($"Resolved dependency of {package}: {dependencyIdentity}");
                        if (installedPackages.Contains(dependencyIdentity)) continue;
                        InstallPackageWithDependencies(dependencyIdentity, packageDownloadContext);
                        installedPackages.Add(dependencyIdentity);
                    }
                }

                var packageIdentity = new PackageIdentity(package.Id, new NuGetVersion(package.Version.Version));
                var installSuccess = project
                    .InstallPackageAsync(packageIdentity, downloadResourceResult, projectContext,
                        CancellationToken.None)
                    .Result;
                if (installSuccess)
                {
                    projectSystem.Save();
                    Log.Info($"Installed {package}");
                }
                else
                {
                    Log.Info($"{package} not installed");
                    ConsoleWriter.WriteWarning($"Nuget package {package} not installed");
                }
            }

            private DownloadResourceResult LoadPackage(PackageIdentity package,
                PackageDownloadContext packageDownloadContext)
            {
                var downloadResourceResult = PackageDownloader.GetDownloadResourceResultAsync(
                    repositories,
                    package,
                    packageDownloadContext,
                    SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null)),
                    logger,
                    CancellationToken.None
                ).Result;
                return downloadResourceResult;
            }

            private static PackageIdentity ParsePackage(string packageName)
            {
                var splitted = packageName.Split('/');
                if (splitted.Length != 2)
                    throw new BadNuGetPackageException(packageName);
                var packageId = splitted[0];
                var version = NuGetVersion.Parse(splitted[1]);
                return new PackageIdentity(packageId, version);
            }
        }

        public static void InstallPackages(List<string> packagesList, string packagesPath, string projectFilePath)
        {
            new NuGetProject(packagesList, packagesPath, projectFilePath).Install();
        }
    }
}