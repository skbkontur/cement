using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cement.Cli.Common.Exceptions;
using Microsoft.Extensions.Logging;
using NuGet.CommandLine;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Cement.Cli.Common;

public sealed class NuGetPackageHepler
{
    private readonly ILogger log;

    public NuGetPackageHepler(ILogger log)
    {
        this.log = log;
    }

    public void InstallPackages(List<string> packagesList, string packagesPath, ProjectFile projectFilePath)
    {
        new NuGetProject(packagesList, packagesPath, projectFilePath, log).Install();
    }

    private class NuGetProject
    {
        private readonly List<string> packagesList;
        private readonly ProjectFile projectFile;
        private readonly NuGet.CommandLine.Console logger;
        private readonly MSBuildNuGetProject project;
        private readonly ConsoleProjectContext projectContext;
        private readonly MSBuildProjectSystem projectSystem;
        private readonly List<SourceRepository> repositories;
        private readonly HashSet<PackageIdentity> installedPackages;
        private readonly ILogger log;

        public NuGetProject(List<string> packagesList, string packagesPath, ProjectFile projectFile, ILogger log)
        {
            this.log = log;
            this.packagesList = packagesList;
            this.projectFile = projectFile;
            installedPackages = new HashSet<PackageIdentity>();
            var sourceProvider = new PackageSourceProvider(Settings.LoadDefaultSettings(null));
            var sourceRepositoryProvider = new CommandLineSourceRepositoryProvider(sourceProvider);
            repositories = sourceProvider.LoadPackageSources().Select(sourceRepositoryProvider.CreateRepository)
                .ToList();
            logger = new NuGet.CommandLine.Console();

            var projectFilePath = projectFile.FilePath;

            var msbuildDirectory =
                Path.GetDirectoryName(ModuleBuilderHelper.Shared.FindMsBuild(null, "Cement NuGet Package Installer").Path);
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

            projectSystem.Save();

            var projectFileContent = File.ReadAllText(projectSystem.ProjectFileFullPath);
            var contentLines = projectFileContent
                .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            contentLines[0] = contentLines[0].Replace("utf-16", "utf-8");
            File.WriteAllText(
                projectSystem.ProjectFileFullPath,
                string.Join(projectFile.LineEndings, contentLines),
                new UTF8Encoding(true));
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

        private void InstallPackageWithDependencies(PackageIdentity package,
                                                    PackageDownloadContext packageDownloadContext)
        {
            log.LogInformation($"Loading package {package}");
            var downloadResourceResult = LoadPackage(package, packageDownloadContext);
            var dependencyGroups = downloadResourceResult.PackageReader.GetPackageDependencies().ToList();
            var mostCompatibleFramework = new FrameworkReducer().GetNearest(
                projectSystem.TargetFramework,
                dependencyGroups.Select(dg => dg.TargetFramework));
            var dependencyGroup = dependencyGroups.FirstOrDefault(
                ds =>
                    ds.TargetFramework.Equals(mostCompatibleFramework));
            if (dependencyGroup != null)
            {
                foreach (var dependency in dependencyGroup.Packages)
                {
                    var dependencyIdentity = new PackageIdentity(
                        dependency.Id,
                        NuGetVersion.Parse(dependency.VersionRange.MinVersion.ToFullString()));
                    log.LogInformation($"Resolved dependency of {package}: {dependencyIdentity}");
                    if (installedPackages.Contains(dependencyIdentity)) continue;
                    InstallPackageWithDependencies(dependencyIdentity, packageDownloadContext);
                    installedPackages.Add(dependencyIdentity);
                }
            }

            var packageIdentity = new PackageIdentity(package.Id, new NuGetVersion(package.Version.Version));
            var installSuccess = project
                .InstallPackageAsync(
                    packageIdentity, downloadResourceResult, projectContext,
                    CancellationToken.None)
                .Result;
            if (installSuccess)
            {
                log.LogInformation($"Installed {package}");
            }
            else
            {
                log.LogInformation($"{package} not installed");
                ConsoleWriter.Shared.WriteWarning($"Nuget package {package} not installed");
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
    }
}
