using System;
using System.IO;
using System.Linq;
using System.Threading;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Common
{
    public sealed class PackageUpdater
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly ILogger<PackageUpdater> logger;
        private readonly ConsoleWriter consoleWriter;

        public static PackageUpdater Shared { get; } = new(LogManager.GetLogger<PackageUpdater>(), ConsoleWriter.Shared);

        public PackageUpdater(ILogger<PackageUpdater> logger, ConsoleWriter consoleWriter)
        {
            this.logger = logger;
            this.consoleWriter = consoleWriter;
        }

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
            var runner = new ShellRunner(logger);
            var timeout = TimeSpan.FromMinutes(1);

            var remoteHash = GetRepositoryHeadHash(package);
            var localHash = Helper.GetPackageCommitHash(package.Name);
            if (remoteHash != null && remoteHash.Equals(localHash))
                return;

            for (var i = 0; i < 3; i++)
            {
                using (var tempDir = new TempDirectory())
                {
                    if (runner.RunOnce(
                            $"git clone {package.Url} \"{Path.Combine(tempDir.Path, package.Name)}\"",
                            Directory.GetCurrentDirectory(),
                            timeout)
                        != 0)
                    {
                        if (runner.HasTimeout && i < 2)
                        {
                            timeout = TimeoutHelper.IncreaseTimeout(timeout);
                            continue;
                        }

                        throw new CementException(
                            $"Failed to update package {package.Name}:\n{runner.Output}\nError message:\n{runner.Errors}\n" +
                            $"Ensure that command 'git clone {package.Url}' works in cmd");
                    }

                    lock (Helper.PackageLockObject)
                    {
                        if (!Directory.Exists(Helper.GetGlobalCementDirectory()))
                            Directory.CreateDirectory(Helper.GetGlobalCementDirectory());
                        File.Copy(
                            Path.Combine(tempDir.Path, package.Name, "modules"),
                            Helper.GetPackagePath(package.Name), true);
                        Helper.WritePackageCommitHash(package.Name, remoteHash);
                    }

                    break;
                }
            }
        }

        private string GetRepositoryHeadHash(Package package)
        {
            var runner = new ShellRunner(logger);
            var timeout = TimeSpan.FromMinutes(1);

            var exitCode = runner.RunOnce($"git ls-remote {package.Url} HEAD", Directory.GetCurrentDirectory(), timeout);

            if (exitCode != 0)
            {
                var errorMessage = $"Cannot get hash code of package '{package.Name}' ({package.Url})\n" +
                                   "Git output:\n\n" +
                                   runner.Errors;

                throw new CementException(errorMessage);
            }

            var output = runner.Output;

            return output.Split().FirstOrDefault();
        }
    }
}
