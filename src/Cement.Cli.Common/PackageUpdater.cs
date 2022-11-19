using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cement.Cli.Common.Exceptions;
using Cement.Cli.Common.Logging;
using JetBrains.Annotations;

namespace Cement.Cli.Common;

[PublicAPI]
public sealed class PackageUpdater : IPackageUpdater
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly ConsoleWriter consoleWriter;

    public PackageUpdater(ConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;
    }

    public static PackageUpdater Shared { get; } = new(ConsoleWriter.Shared);

    public void UpdatePackages()
    {
        consoleWriter.WriteProgress("Updating module urls");
        var packages = Helper.GetPackages();
        foreach (var package in packages)
        {
            if (package.Type.Equals("git"))
            {
                UpdateGitPackage(package);
            }

            if (package.Type.Equals("file"))
            {
                UpdateFilePackage(package);
            }
        }

        consoleWriter.ResetProgress();
    }

    private void UpdateFilePackage(Package package)
    {
        semaphore.Wait();
        try
        {
            var source = Path.Combine(package.Url);
            var destination = Helper.GetPackagePath(package.Name);

            File.Copy(source, destination, true);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void UpdateGitPackage(Package package)
    {
        var shellRunnerLogger = LogManager.GetLogger<ShellRunner>();
        var shellRunner = new ShellRunner(shellRunnerLogger);

        var timeout = TimeSpan.FromMinutes(1);

        var remoteHash = GetRepositoryHeadHash(package);
        var localHash = Helper.GetPackageCommitHash(package.Name);
        if (remoteHash != null && remoteHash.Equals(localHash))
            return;

        for (var i = 0; i < 3; i++)
        {
            using (var tempDir = new TempDirectory())
            {
                var command = $"git clone {package.Url} \"{Path.Combine(tempDir.Path, package.Name)}\"";
                var workingDirectory = Directory.GetCurrentDirectory();

                var (exitCode, output, errors, hasTimeout) = shellRunner.RunOnce(command, workingDirectory, timeout);
                if (exitCode != 0)
                {
                    if (hasTimeout && i < 2)
                    {
                        timeout = TimeoutHelper.IncreaseTimeout(timeout);
                        continue;
                    }

                    throw new CementException(
                        $"Failed to update package {package.Name}:\n{output}\nError message:\n{errors}\n" +
                        $"Ensure that command 'git clone {package.Url}' works in cmd");
                }

                lock (GlobalLocks.PackageLockObject)
                {
                    if (!Directory.Exists(Helper.GetGlobalCementDirectory()))
                        Directory.CreateDirectory(Helper.GetGlobalCementDirectory());

                    var source = Path.Combine(tempDir.Path, package.Name, "modules");
                    var destination = Helper.GetPackagePath(package.Name);

                    File.Copy(source, destination, true);
                    Helper.WritePackageCommitHash(package.Name, remoteHash);
                }

                break;
            }
        }
    }

    private string GetRepositoryHeadHash(Package package)
    {
        var shellRunnerLogger = LogManager.GetLogger<ShellRunner>();
        var shellRunner = new ShellRunner(shellRunnerLogger);

        var timeout = TimeSpan.FromMinutes(1);

        var command = $"git ls-remote {package.Url} HEAD";
        var workingDirectory = Directory.GetCurrentDirectory();

        var (exitCode, output, errors) = shellRunner.RunOnce(command, workingDirectory, timeout);
        if (exitCode != 0)
        {
            var errorMessage = $"Cannot get hash code of package '{package.Name}' ({package.Url})\n" +
                               "Git output:\n\n" +
                               errors;

            throw new CementException(errorMessage);
        }

        return output.Split().FirstOrDefault();
    }
}
