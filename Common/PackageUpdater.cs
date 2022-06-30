using System;
using System.IO;
using System.Linq;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Common
{
    public static class PackageUpdater
    {
        private static readonly ILogger Log = LogManager.GetLogger(typeof(PackageUpdater));

        public static void UpdatePackages()
        {
            ConsoleWriter.Shared.WriteProgress("Updating module urls");
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
            ConsoleWriter.Shared.ResetProgress();
        }

        private static void UpdateFilePackage(Package package)
        {
            lock (Helper.PackageLockObject)
                File.Copy(Path.Combine(package.Url),
                    Helper.GetPackagePath(package.Name), true);
        }

        private static void UpdateGitPackage(Package package)
        {
            var runner = new ShellRunner(Log);
            var timeout = TimeSpan.FromMinutes(1);

            var remoteHash = GetRepositoryHeadHash(package);
            var localHash = Helper.GetPackageCommitHash(package.Name);
            if (remoteHash != null && remoteHash.Equals(localHash))
                return;

            for (int i = 0; i < 3; i++)
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
                        File.Copy(Path.Combine(tempDir.Path, package.Name, "modules"),
                            Helper.GetPackagePath(package.Name), true);
                        Helper.WritePackageCommitHash(package.Name, remoteHash);
                    }
                    break;
                }
            }
        }

        private static string GetRepositoryHeadHash(Package package)
        {
            var runner = new ShellRunner(Log);
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
