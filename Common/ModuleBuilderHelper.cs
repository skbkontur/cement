using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net;

namespace Common
{
    public static class ModuleBuilderHelper
    {
        private static readonly ILog Log = LogManager.GetLogger("moduleBuilderHelper");

        public static string FindMsBuild(string version, string moduleName)
        {
            var msBuilds = FindAviableMsBuilds();

            if (version != null)
                msBuilds = msBuilds.Where(b => b.Key == version).ToList();

            if (!msBuilds.Any())
                throw new CementException($"Failed to find msbuild.exe {version ?? ""} for {moduleName}");
            return msBuilds.First().Value;
        }

        private static List<KeyValuePair<string, string>> msBuildsCache;

        private static List<KeyValuePair<string, string>> FindAviableMsBuilds()
        {
            if (msBuildsCache != null)
                return msBuildsCache;

            var result = new List<KeyValuePair<string, string>>();

            var ms1 = FindAviableMsBuildsInProgramFiles();
            var ms2 = FindAviableMsBuildsInWindows();
            result.AddRange(ms1);
            result.AddRange(ms2);

            Log.Debug("MSBUILDS:\n" + string.Join("\n", result.Select(r => $"{r.Key} {r.Value}")));

            return msBuildsCache = result;
        }

        private static List<KeyValuePair<string, string>> FindAviableMsBuildsInProgramFiles()
        {
            var programFiles = Helper.ProgramFiles();
            if (programFiles == null)
                return new List<KeyValuePair<string, string>>();

            var folders = new List<string> {programFiles};

            var variables = VsDevHelper.GetCurrentSetVariables();
            if (variables.ContainsKey("VSINSTALLDIR"))
                folders.Add(variables["VSINSTALLDIR"]);

            folders.AddRange(Helper.VisualStudioVersions().Select(
                version => Path.Combine(programFiles, "Microsoft Visual Studio", "2017", version)));

            return folders.SelectMany(FindAviableMsBuildsIn).Distinct().OrderByDescending(k => k.Key).ToList();
        }

        private static List<KeyValuePair<string, string>> FindAviableMsBuildsIn(string folder)
        {
            var result = new List<KeyValuePair<string, string>>();
            folder = Path.Combine(folder, "MSBuild");
            if (!Directory.Exists(folder))
                return result;
            var subDirs = new DirectoryInfo(folder).GetDirectories().OrderByDescending(x => x.Name);
            foreach (var subDir in subDirs)
            {
                var currentVersion = subDir.Name;
                if (!Helper.IsVisualStudioVersion(currentVersion))
                    continue;
                var bin = Path.Combine(subDir.FullName, "Bin");
                if (!Directory.Exists(bin))
                    continue;
                var matches = new DirectoryInfo(bin).GetFiles("msbuild.exe").ToList();
                if (matches.Any())
                    result.Add(new KeyValuePair<string, string>(currentVersion, matches.Last().FullName));
            }
            return result;
        }

        private static List<KeyValuePair<string, string>> FindAviableMsBuildsInWindows()
        {
            var winDir = Environment.GetEnvironmentVariable("WINDIR");
            if (winDir == null)
                throw new CementException("WINDIR system variable not found");

            var msbuilds = new List<FileInfo>();

            var frameworkDirectory = Path.Combine(winDir, "Microsoft.NET", "Framework");
            msbuilds.AddRange(SearchMsBuild(frameworkDirectory));
            frameworkDirectory = Path.Combine(winDir, "Microsoft.NET", "Framework64");
            msbuilds.AddRange(SearchMsBuild(frameworkDirectory));

            return msbuilds.Select(path =>
                    new KeyValuePair<string, string>(Directory.GetParent(path.FullName).Name, path.FullName)).Reverse()
                .ToList();
        }

        private static List<FileInfo> SearchMsBuild(string frameworkDirectory)
        {
            if (!Directory.Exists(frameworkDirectory))
                return new List<FileInfo>();

            var subFolders = Directory.GetDirectories(frameworkDirectory);
            return subFolders.Select(folder => Path.Combine(folder, "msbuild.exe"))
                .Where(File.Exists).Select(f => new FileInfo(f)).ToList();
        }

        public static void KillMsBuild(ILog log)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("MSBuild"))
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        public static string GetBuildScriptName(Dep dep)
        {
            var configToBuild = dep.Configuration == "full-build" || dep.Configuration == null
                ? ""
                : "." + dep.Configuration;
            return Path.Combine(Helper.CurrentWorkspace, dep.Name,
                "build" + configToBuild + ".cmd");
        }

        public static bool IsObsoleteWarning(string line)
        {
            return line.Contains(": warning CS0618:") || line.Contains(": warning CS0612:");
        }

        public static bool IsWarning(string line)
        {
            return line.Contains(": warning");
        }

        public static bool IsError(string line)
        {
            return line.Contains(": error");
        }

        public static void WriteIfWarning(string line)
        {
            if (IsWarning(line))
            {
                ConsoleWriter.WriteLineBuildWarning(line);
            }
        }

        public static void WriteIfObsoleteFull(string line)
        {
            if (IsObsoleteWarning(line))
            {
                ConsoleWriter.WriteLineBuildWarning(line);
            }
        }

        private static readonly HashSet<string> PrintedObsolete = new HashSet<string>();

        public static void WriteIfObsoleteGrouped(string line)
        {
            if (IsObsoleteWarning(line))
            {
                line = CutObsoleteMethond(line);
                if (PrintedObsolete.Contains(line))
                    return;
                PrintedObsolete.Add(line);
                ConsoleWriter.WriteLineBuildWarning(line);
            }
        }

        private static string CutObsoleteMethond(string line)
        {
            try
            {
                var left = line.IndexOf(": warning ", StringComparison.Ordinal) + 18;
                var right = line.LastIndexOf("[", StringComparison.Ordinal);
                var substr = line.Substring(left, right - left);
                return substr;
            }
            catch (Exception)
            {
                return line;
            }
        }

        public static void WriteIfError(string line)
        {
            if (IsError(line))
                ConsoleWriter.WriteBuildError(line);
        }

        public static void WriteIfErrorToStandartStream(string line)
        {
            if (IsError(line))
                ConsoleWriter.PrintLn(line, ConsoleColor.Red);
        }

        public static void WriteLine(string line)
        {
            if (IsError(line))
                ConsoleWriter.WriteBuildError(line);
            else if (IsWarning(line))
                ConsoleWriter.WriteBuildWarning(line);
            else ConsoleWriter.WriteLine(line);
        }

        public static void WriteProgress(string line)
        {
            ConsoleWriter.WriteProgress(line);
        }
    }
}
