using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Common
{
    public static class Helper
    {
        public const string CementDirectory = ".cement";
        public const string YamlSpecFile = "module.yaml";
        public const string ConfigurationDelimiter = "/";
        public static readonly object LockObject = new object();
        public static readonly int MaxDegreeOfParallelism = CementSettings.Get().MaxDegreeOfParallelism ?? 2 * Environment.ProcessorCount;
        public static ParallelOptions ParallelOptions => new ParallelOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism};
        public static string CurrentWorkspace { get; private set; }
        public static readonly object PackageLockObject = new object();
        private static readonly ILogger Log = LogManager.GetLogger(typeof(Helper));

        public static void SetWorkspace(string workspace)
        {
            CurrentWorkspace = workspace;
        }

        public static bool IsCementTrackedDirectory(string path)
        {
            return Directory.Exists(Path.Combine(path, CementDirectory));
        }

        public static bool IsCurrentDirectoryModule(string cwd)
        {
            if (cwd.Equals(Directory.GetDirectoryRoot(cwd)))
                return false;

            if (IsCementTrackedDirectory(cwd))
                return false;

            var parentDirectory = Directory.GetParent(cwd).FullName;
            if (!IsCementTrackedDirectory(parentDirectory))
                return false;
            return true;
        }

        public static string GetGlobalCementDirectory()
        {
            return Path.Combine(HomeDirectory(), CementDirectory);
        }

        public static string GetCementInstallDirectory()
        {
            return Path.Combine(HomeDirectory(), "bin");
        }

        public static string GetZipCementDirectory()
        {
            var zipDir = Path.Combine(HomeDirectory(), "work");
            if (!Directory.Exists(zipDir))
                Directory.CreateDirectory(zipDir);
            return zipDir;
        }
        
        public static bool OsIsUnix()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix;
        }

        public static string GetOsPublishPath()
        {
            if (OsIsUnix())
            {
                return "linux-x64";
            }
            else
            {
                if (Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    return "os-x64";
                }

                return "win-x64";
            }
        }

        public static string HomeDirectory()
        {
            return OsIsUnix()
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.GetEnvironmentVariable("USERPROFILE");
        }

        public static string GetPackagePath(string packageName)
        {
            return Path.Combine(GetGlobalCementDirectory(), packageName + ".cmpkg");
        }

        public static string GetPackageCommitHash(string packageName)
        {
            var path = Path.Combine(GetGlobalCementDirectory(), packageName + ".cmpkg.hash");
            if (!File.Exists(path))
                return "";
            return File.ReadAllText(path);
        }

        public static void WritePackageCommitHash(string packageName, string commitHash)
        {
            var path = Path.Combine(GetGlobalCementDirectory(), packageName + ".cmpkg.hash");
            File.WriteAllText(path, commitHash);
        }

        public static string GetServerRepositoriesPath()
        {
            return Path.Combine(Directory.GetDirectoryRoot(HomeDirectory()), "CementServer", "Repositories");
        }

        public static bool DirectoryContainsModule(string directory, string moduleName)
        {
            return Directory.EnumerateDirectories(directory)
                .Select(Path.GetFileName)
                .Contains(moduleName);
        }

        public static IList<Package> GetPackages()
        {
            return CementSettings.Get().Packages ?? throw new CementException("Packages not specified.");
        }

        public static IList<Module> GetModulesFromPackage(Package package)
        {
            lock (PackageLockObject)
            {
                var packageConfig = GetPackagePath(package.Name);
                if (!File.Exists(packageConfig))
                    PackageUpdater.UpdatePackages();
                var configData = File.ReadAllText(packageConfig);
                return ModuleIniParser.Parse(configData).ToList();
            }
        }

        public static List<Module> GetModules()
        {
            lock (PackageLockObject)
            {
                var modules = new List<Module>();
                var packages = GetPackages();
                foreach (var package in packages)
                    modules.AddRange(GetModulesFromPackage(package));
                return modules;
            }
        }

        public static string TryFixModuleCase(string module)
        {
            var modules = GetModules();
            foreach (var m in modules)
                if (m.Name.ToLower() == module.ToLower())
                    return m.Name;
            return module;
        }

        public static bool HasModule(string module)
        {
            return GetModules().Any(m => m.Name == module);
        }

        public static string DefineForce(string force, GitRepository rootRepo)
        {
            if (force == null || !force.Contains("->") && !force.Contains("CURRENT_BRANCH"))
                return force;
            if (force.Equals("%CURRENT_BRANCH%") || force.Equals("$CURRENT_BRANCH"))
                return rootRepo.CurrentLocalTreeish().Value;

            return null;
        }

        public static string DefineForce(string force, string branch)
        {
            if (force == null || !force.Contains("->") && !force.Contains("CURRENT_BRANCH"))
                return force;
            if (force.Equals("%CURRENT_BRANCH%") || force.Equals("$CURRENT_BRANCH"))
                return branch;

            return null;
        }

        public static string GetCurrentBuildCommitHash()
        {
            var gitInfo = GetAssemblyTitle();
            var commitHash = gitInfo.Split('\n').Skip(1).First().Replace("Commit: ", String.Empty).Trim();
            return commitHash;
        }

        public static string GetAssemblyTitle()
        {
            return ((AssemblyTitleAttribute)
                Attribute.GetCustomAttribute(
                    Assembly.GetEntryAssembly(),
                    typeof(AssemblyTitleAttribute))).Title;
        }

        public static string ConvertTime(long millisecs)
        {
            var ts = TimeSpan.FromMilliseconds(millisecs);
            var res = ts.ToString(@"d\:hh\:mm\:ss\.fff");

            int idx = 0;
            while (res[idx] == '0' || res[idx] == ':')
            {
                idx++;
            }

            res = res.Substring(idx);
            return res;
        }

        // ReSharper disable once UnusedMember.Global
        public static string GetBinariesPath()
        {
            return Path.Combine(Directory.GetDirectoryRoot(HomeDirectory()), "CementServer", "Binaries");
        }

        public static string GetModuleDirectory(string path)
        {
            if (path == null)
                return null;
            var parent = Directory.GetParent(path);
            while (parent != null)
            {
                if (IsCementTrackedDirectory(parent.FullName))
                    return path;
                path = parent.FullName;
                parent = Directory.GetParent(parent.FullName);
            }

            return null;
        }

        public static string GetWorkspaceDirectory(string path)
        {
            var folder = new DirectoryInfo(path);
            while (folder != null && !IsCementTrackedDirectory(folder.FullName))
            {
                folder = Directory.GetParent(folder.FullName);
            }

            return folder?.FullName;
        }

        public static string GetRelativePath(string filePath, string fromFolder)
        {
            var pathUri = new Uri(filePath);
            if (!fromFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                fromFolder += Path.DirectorySeparatorChar;
            }

            var folderUri = new Uri(fromFolder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static string GetRootFolder(string path)
        {
            while (true)
            {
                var temp = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(temp))
                    break;
                path = temp;
            }

            return path;
        }

        private static string GetLastUpdateFilePath()
        {
            return Path.Combine(GetGlobalCementDirectory(), "last-update2");
        }

        public static DateTime GetLastUpdateTime()
        {
            var file = GetLastUpdateFilePath();
            if (!File.Exists(file))
                return DateTime.MinValue;

            return File.GetLastWriteTime(file);
        }

        public static void SaveLastUpdateTime()
        {
            var file = GetLastUpdateFilePath();
            CreateFileAndDirectory(file, "");
        }

        public static void CreateFileAndDirectory(string filePath, string content)
        {
            CreateFileAndDirectory(filePath);
            File.WriteAllText(filePath, content);
        }

        private static void CreateFileAndDirectory(string filePath)
        {
            var dir = Directory.GetParent(filePath).FullName;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (!File.Exists(filePath))
                File.Create(filePath).Close();
        }

        public static void RemoveOldKey(ref string[] args, string oldKey, ILogger log)
        {
            if (args.Contains(oldKey))
            {
                ConsoleWriter.WriteError("Don't use old " + oldKey + " key.");
                log.LogWarning("Found old key " + oldKey + " in " + string.Join(" ", args) + " in " + Directory.GetCurrentDirectory());
                args = args.Where(a => a != oldKey).ToArray();
            }
        }

        public static string FixPath([NotNull] string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar);
        }

        public static ProgramFilesInfo GetProgramFilesInfo()
        {
            var x86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            var x64 = Environment.GetEnvironmentVariable("ProgramFiles");
            if (x64 == null && x86 == null)
                return null;

            return new ProgramFilesInfo {x64 = x64, x86 = x86};
        }

        public static IReadOnlyList<string> VisualStudioEditions { get; } =
            new List<string>
            {
                "Community",
                "Professional",
                "Enterprise",
                "BuildTools",
            }.AsReadOnly();

        public static IReadOnlyList<string> VisualStudioVersions { get; } =
            new List<string> {"2017", "2019", "2022"}.AsReadOnly();

        public static string GetEnvVariableByVisualStudioVersion(string version)
        {
            switch (version)
            {
                case "2022": return "VS170COMNTOOLS";
                case "2019": return "VS160COMNTOOLS";
                default: return "VS150COMNTOOLS";
            }
        }

        public static bool IsVisualStudioVersion(string version)
        {
            return !string.IsNullOrEmpty(version) && Regex.IsMatch(version, "^[0-9][0-9].[0-9]$");
        }

        public static string Encrypt(string password)
        {
            byte[] passwordBytes = Encoding.Unicode.GetBytes(password);

            byte[] cipherBytes = ProtectedData.Protect(passwordBytes, null, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(cipherBytes);
        }

        public static string Decrypt(string cipher)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipher);

            byte[] passwordBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);

            return Encoding.Unicode.GetString(passwordBytes);
        }

        public static string FixLineEndings(string text)
        {
            return text.Replace("\r\n", "\n");
        }

        public static string UnixPathSlashesToWindows(string path)
        {
            return path.Replace('/', '\\');
        }

        public static string WindowsPathSlashesToUnix(string path)
        {
            return path.Replace('\\', '/');
        }

        public static string GetMsBuildVersion(string fullPathToMsBuild)
        {
            if (!File.Exists(fullPathToMsBuild))
                return null;

            try
            {
                var shellRunner = new ShellRunner();
                var exitCode = shellRunner.RunOnce(Path.GetFileName(fullPathToMsBuild) + " -version", Path.GetDirectoryName(fullPathToMsBuild), TimeSpan.FromSeconds(10));
                if (exitCode == 0 && !string.IsNullOrEmpty(shellRunner.Output))
                {
                    var versionMatches = Regex.Matches(shellRunner.Output, @"^(?<version>\d+(\.\d+)+)", RegexOptions.ExplicitCapture | RegexOptions.Multiline);
                    if (versionMatches.Count > 0)
                    {
                        var version = versionMatches[versionMatches.Count - 1].Groups["version"].Value;
                        if (!string.IsNullOrEmpty(version))
                            return version;
                    }
                }
                else
                    Log.LogDebug("Failed to get msbuild version for " + fullPathToMsBuild);
            }
            catch (Exception e)
            {
                Log.LogWarning("Failed to get MSBuild version from " + fullPathToMsBuild, e);
            }

            return null;
        }
    }
}