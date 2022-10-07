using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common.Exceptions;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Common;

public static class ModuleBuilderHelper
{
    private static readonly ILogger Log = LogManager.GetLogger(typeof(ModuleBuilderHelper));

    private static readonly HashSet<string> PrintedObsolete = new();

    public static MsBuildLikeTool FindMsBuild(string version, string moduleName)
    {
        if (Platform.IsUnix())
            return new MsBuildLikeTool(FindMsBuildUnix(version, moduleName));

        var msBuilds = FindMsBuildsWindows();

        if (version != null && Version.TryParse(version, out var minVersion))
            msBuilds = msBuilds.Where(b => Version.TryParse(b.Key, out var msBuildVersion) && msBuildVersion >= minVersion).ToList();

        if (!msBuilds.Any())
            throw new CementException($"Failed to find msbuild.exe {version ?? ""} for {moduleName}");

        var msbuild = msBuilds.First();
        return new MsBuildLikeTool(
            msbuild.Value,
            FileVersionInfo.GetVersionInfo(msbuild.Value).FileVersion,
            true);
    }

    public static List<KeyValuePair<string, string>> FindMsBuildsWindows()
    {
        var result = new List<KeyValuePair<string, string>>();

        var ms1 = FindAvailableMsBuildsInProgramFiles();
        var ms2 = FindAvailableMsBuildsInWindows();
        result.AddRange(ms1);
        result.AddRange(ms2);

        Log.LogDebug("MSBUILDS:\n" + string.Join("\n", result.Select(r => $"{r.Key} {r.Value}")));

        return result;
    }

    public static void KillMsBuild(ILogger log)
    {
        if (!CementSettingsRepository.Get().KillMsBuild || Rider.IsRunning)
            return;

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
            log.LogError(e, e.Message);
        }
    }

    public static string GetBuildScriptName(Dep dep)
    {
        var configToBuild = dep.Configuration == "full-build" || dep.Configuration == null ? "" : "." + dep.Configuration;
        return Path.Combine(
            Helper.CurrentWorkspace,
            dep.Name,
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
            ConsoleWriter.Shared.WriteLineBuildWarning(line);
        }
    }

    public static void WriteIfObsoleteFull(string line)
    {
        if (IsObsoleteWarning(line))
        {
            ConsoleWriter.Shared.WriteLineBuildWarning(line);
        }
    }

    public static void WriteIfObsoleteGrouped(string line)
    {
        if (IsObsoleteWarning(line))
        {
            line = CutObsoleteMethond(line);
            if (PrintedObsolete.Contains(line))
                return;
            PrintedObsolete.Add(line);
            ConsoleWriter.Shared.WriteLineBuildWarning(line);
        }
    }

    public static void WriteIfError(string line)
    {
        if (IsError(line))
            ConsoleWriter.Shared.WriteBuildError(line);
    }

    public static void WriteIfErrorToStandartStream(string line)
    {
        if (IsError(line))
            ConsoleWriter.Shared.PrintLn(line, ConsoleColor.Red);
    }

    public static void WriteLine(string line)
    {
        if (IsError(line))
            ConsoleWriter.Shared.WriteBuildError(line);
        else if (IsWarning(line))
            ConsoleWriter.Shared.WriteBuildWarning(line);
        else ConsoleWriter.Shared.WriteLine(line);
    }

    public static void WriteProgress(string line)
    {
        ConsoleWriter.Shared.WriteProgress(line);
    }

    private static string FindMsBuildUnix(string version, string moduleName)
    {
        var monoRuntime = Type.GetType("Mono.Runtime");
        if (monoRuntime == null)
            throw new CementException($"Failed to find msbuild.exe {version ?? ""} for {moduleName}");
        var monoRuntimePath = monoRuntime.Assembly.Location;
        var monoRuntimeDir = Path.GetDirectoryName(monoRuntimePath);
        if (monoRuntimeDir == null)
            throw new CementException($"Failed to find msbuild.exe {version ?? ""} for {moduleName}");
        return Path.Combine(monoRuntimeDir, "../msbuild/15.0/bin/");
    }

    private static List<KeyValuePair<string, string>> FindAvailableMsBuildsInProgramFiles()
    {
        var programFiles = Helper.GetProgramFilesInfo();
        if (programFiles == null)
            return new List<KeyValuePair<string, string>>();

        var folders = new List<string> {programFiles.x86, programFiles.x64};

        var variables = VsDevHelper.GetCurrentSetVariables();
        if (variables.ContainsKey("VSINSTALLDIR"))
            folders.Add(variables["VSINSTALLDIR"]);

        foreach (var version in Helper.VisualStudioVersions)
        foreach (var edition in Helper.VisualStudioEditions)
        {
            if (!string.IsNullOrWhiteSpace(programFiles.x86))
                folders.Add(Path.Combine(programFiles.x86, "Microsoft Visual Studio", version, edition));

            if (!string.IsNullOrWhiteSpace(programFiles.x64))
                folders.Add(Path.Combine(programFiles.x64, "Microsoft Visual Studio", version, edition));
        }

        return folders.SelectMany(FindAvailableMsBuildsIn).Distinct().OrderByDescending(k => k.Key).ToList();
    }

    private static List<KeyValuePair<string, string>> FindAvailableMsBuildsIn(string folder)
    {
        var result = new List<KeyValuePair<string, string>>();
        folder = Path.Combine(folder, "MSBuild");
        if (!Directory.Exists(folder))
            return result;

        var subDirs = new DirectoryInfo(folder).GetDirectories().OrderByDescending(x => x.Name);
        foreach (var subDir in subDirs)
        {
            var currentVersion = subDir.Name;
            if (!Helper.IsVisualStudioVersion(currentVersion) && currentVersion != "Current")
                continue;

            var bin = Path.Combine(subDir.FullName, "Bin");
            if (!Directory.Exists(bin))
                continue;

            var match = new DirectoryInfo(bin).GetFiles("msbuild.exe")
                .Select(m => new {fullPath = m.FullName, version = Helper.GetMsBuildVersion(m.FullName)})
                .LastOrDefault(v => !string.IsNullOrEmpty(v.version));
            if (match != null)
            {
                result.Add(new KeyValuePair<string, string>(match.version, match.fullPath));
            }
        }

        return result;
    }

    private static List<KeyValuePair<string, string>> FindAvailableMsBuildsInWindows()
    {
        var winDir = Environment.GetEnvironmentVariable("WINDIR");

        if (winDir == null)
            throw new CementException("WINDIR system variable not found");

        var msbuilds = new List<FileInfo>();

        var frameworkDirectory = Path.Combine(winDir, "Microsoft.NET", "Framework");
        msbuilds.AddRange(SearchMsBuild(frameworkDirectory));
        frameworkDirectory = Path.Combine(winDir, "Microsoft.NET", "Framework64");
        msbuilds.AddRange(SearchMsBuild(frameworkDirectory));

        return msbuilds.Select(path => new {path = path.FullName, version = Helper.GetMsBuildVersion(path.FullName)})
            .Where(i => !string.IsNullOrEmpty(i.version))
            .Select(i => new KeyValuePair<string, string>(i.version, i.path))
            .Reverse()
            .ToList();
    }

    private static List<FileInfo> SearchMsBuild(string frameworkDirectory)
    {
        if (!Directory.Exists(frameworkDirectory))
            return new List<FileInfo>();

        var subFolders = Directory.GetDirectories(frameworkDirectory);
        return subFolders.Select(folder => Path.Combine(folder, "msbuild.exe"))
            .Where(File.Exists)
            .Select(f => new FileInfo(f))
            .ToList();
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
}
