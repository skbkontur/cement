using log4net;
using System;
using System.IO;

namespace Common
{
    public static class PackageUpdater
    {
        private static readonly ILog Log = LogManager.GetLogger("PackageUpdater");

        public static void UpdatePackages()
        {
            ConsoleWriter.WriteProgress("Updating module urls");
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
            ConsoleWriter.ResetProgress();
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
                            timeout = TimoutHelper.IncreaceTimeout(timeout);
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
                    }
                    break;
                }
            }
        }
    }
}