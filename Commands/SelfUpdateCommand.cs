using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Common;
using Common.Updaters;
using Microsoft.Extensions.Logging;

namespace Commands;

public class SelfUpdateCommand : Command
{
    private static bool isAutoUpdate;
    protected bool IsInstallingCement;
    private string branch;
    private string server;

    public SelfUpdateCommand()
        : base(
            new CommandSettings
            {
                LogFileName = "self-update",
                MeasureElapsedTime = false,
                Location = CommandSettings.CommandLocation.Any
            })
    {
    }

    public static void UpdateIfOld()
    {
        try
        {
            var isEnabledSelfUpdate = CementSettingsRepository.Get().IsEnabledSelfUpdate;
            if (isEnabledSelfUpdate.HasValue && !isEnabledSelfUpdate.Value)
                return;
            var lastUpdate = Helper.GetLastUpdateTime();
            var now = DateTime.Now;
            var diff = now - lastUpdate;
            if (diff <= TimeSpan.FromHours(5))
                return;
            isAutoUpdate = true;
            var exitCode = new SelfUpdateCommand().Run(new[] {"self-update"});
            if (exitCode != 0)
            {
                Log.LogError("Auto update cement failed. 'self-update' exited with code '{Code}'", exitCode);
                ConsoleWriter.Shared.WriteWarning("Auto update failed. Check previous warnings for details");
            }
        }
        catch (Exception exception)
        {
            Log.LogError(exception, "Auto update failed, error: '{ErrorMessage}'", exception.Message);
            ConsoleWriter.Shared.WriteWarning("Auto update failed. Check logs for details");
        }
    }

    public override string HelpMessage => @"
    Updates cement itself (automatically updated every 5 hours)
    Usage:
        cm self-update
";

    protected override void ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseSelfUpdate(args);
        branch = (string)parsedArgs["branch"];
        server = (string)parsedArgs["server"];

        SearchAndSaveBranchInSettings(ref server, ref branch);

        if (isAutoUpdate)
            Log.LogDebug("Auto updating cement");
        IsInstallingCement |= !HasInstalledCement();
        if (IsInstallingCement)
        {
            ConsoleWriter.Shared.WriteInfo("Installing cement");
            Log.LogDebug("Installing cement");
        }
    }

    protected override int Execute()
    {
        ConsoleWriter.Shared.WriteProgressWithoutSave("self-update");
        Helper.SaveLastUpdateTime();

        try
        {
            InstallPowerShell();
            InstallClinkScript();
            InstallBashScript();

            CreateRunners();
            AddInstallToPath();
        }
        catch (Exception exception)
        {
            Log.LogError(exception, "Fail to install cement: '{ErrorMessage}'", exception.Message);
            ConsoleWriter.Shared.WriteError("Fail to install cement: " + exception);
        }

        Log.LogInformation("Cement server: {CementServerUri}", server);

        using ICementUpdater updater = server == null
            ? new GitHubReleaseCementUpdater(Log)
            : new ServerCementUpdater(Log, ConsoleWriter.Shared, server, branch);

        Log.LogInformation("Updater: {CementUpdaterName}", updater.Name);
        return UpdateBinary(updater);
    }

    private static void CreateRunners()
    {
        var installDirectory = Helper.GetCementInstallDirectory();

        if (OperatingSystem.IsWindows())
            Helper.CreateFileAndDirectory(Path.Combine(installDirectory, "cm.cmd"), GetWindowsScript());
        else
            Helper.CreateFileAndDirectory(Path.Combine(installDirectory, "cm"), GetUnixScript());

        Log.LogDebug("Successfully created cm.cmd & cm");
    }

    private static string GetUnixScript()
    {
        const string unixScriptTemplate = @"#!/bin/bash
path=""$HOME/bin/dotnet/cm.exe""

cmd=""$path""
for word in ""$@""; do cmd=""$cmd \""$word\""""; done
eval $cmd
exit_code=$?
if [ -f ~/bin/dotnet/{PlatformSpecificDirectory}/cm ];
then
	cp ~/bin/dotnet/{PlatformSpecificDirectory}/cm ~/bin/dotnet/cm.exe
    rm ~/bin/dotnet/{PlatformSpecificDirectory}/cm
else
    if [ -f ~/bin/dotnet/cm_new.exe ]
    then
        cp ~/bin/dotnet/cm_new.exe ~/bin/dotnet/cm.exe
	    rm ~/bin/dotnet/cm_new.exe
    fi
fi
chmod u+x ~/bin/dotnet/cm.exe
exit $exit_code
";

        return unixScriptTemplate.Replace("{PlatformSpecificDirectory}", GetCurrentOsDirectory()).Replace("\r\n", "\n");
    }

    private static string GetWindowsScript()
    {
        const string windowsScript = @"@echo off
""%~dp0\dotnet\cm.exe"" %*
SET exit_code=%errorlevel%
if exist %~dp0\dotnet\win10-x64\cm.exe (
	copy %~dp0\dotnet\win10-x64\cm.exe %~dp0\dotnet\cm.exe /Y > nul
    del %~dp0\dotnet\win10-x64\cm.exe > nul
) else (
    if exist %~dp0\dotnet\cm_new.exe (
	    copy %~dp0\dotnet\cm_new.exe %~dp0\dotnet\cm.exe /Y > nul
	    del %~dp0\dotnet\cm_new.exe > nul
        )
    )
cmd /C exit %exit_code% > nul";

        return windowsScript;
    }

    private static bool HasInstalledCement()
    {
        var installDirectory = Helper.GetCementInstallDirectory();
        return File.Exists(Path.Combine(installDirectory, "dotnet", "cm.exe"));
    }

    private static bool HasAllCementFiles()
    {
        var installDirectory = Helper.GetCementInstallDirectory();
        return Directory.Exists(Path.Combine(installDirectory, "dotnet", "arborjs"));
    }

    private static void SearchAndSaveBranchInSettings(ref string server, ref string branch)
    {
        var settings = CementSettingsRepository.Get();

        if (branch != null)
            settings.SelfUpdateTreeish = branch;
        else
            branch = settings.SelfUpdateTreeish;

        if (server != null)
        {
            var uri = new Uri(server);
            settings.CementServer = uri.ToString();
        }
        else
        {
            server = settings.CementServer;
        }

        CementSettingsRepository.Save(settings);
    }

    private static IEnumerable<string> GetIncludeFilesForCopy(string from, string currOsDir)
    {
        var filesForCopy = new List<string>();

        if (Directory.Exists(Path.Combine(from, currOsDir)))
        {
            filesForCopy.AddRange(Directory.GetFiles(Path.Combine(from, currOsDir), "*", SearchOption.TopDirectoryOnly).ToList());
        }

        filesForCopy.AddRange(Directory.GetFiles(from, "arborjs\\*", SearchOption.AllDirectories).ToList());

        filesForCopy.AddRange(Directory.GetFiles(from, "*", SearchOption.TopDirectoryOnly));

        return filesForCopy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetCurrentOsDirectory()
    {
        if (OperatingSystem.IsWindows())
            return "win10-x64";
        if (OperatingSystem.IsMacOS())
            return "osx-x64";
        if (OperatingSystem.IsLinux())
            return "linux-x64";

        throw new CementException("Unknown operating system. Terminating");
    }

    private int UpdateBinary(ICementUpdater updater)
    {
        var currentCommitHash = Helper.GetCurrentBuildCommitHash();

        ConsoleWriter.Shared.WriteProgressWithoutSave("Looking for cement updates");
        var newCommitHash = updater.GetNewCommitHash();
        if (newCommitHash == null)
            return -1;

        if (IsInstallingCement)
            currentCommitHash = "(NOT INSTALLED)" + currentCommitHash;
        if (!HasAllCementFiles() || !currentCommitHash.Equals(newCommitHash))
        {
            if (!UpdateBinaries(updater, currentCommitHash, newCommitHash))
                return -1;
        }
        else
        {
            ConsoleWriter.Shared.WriteInfo($"No cement binary updates available ({updater.Name})");
            Log.LogDebug("Already has {0} version", currentCommitHash);
        }

        return 0;
    }

    private bool UpdateBinaries(ICementUpdater updater, string oldHash, string newHash)
    {
        ConsoleWriter.Shared.WriteProgressWithoutSave("Updating cement binaries");

        try
        {
            var zipContent = updater.GetNewCementZip();

            if (zipContent is null)
            {
                ConsoleWriter.Shared.WriteWarning("Failed to receive cement binary");
                return false;
            }

            using (var tempDir = new TempDirectory())
            {
                File.WriteAllBytes(Path.Combine(tempDir.Path, "cement.zip"), zipContent);
                ZipFile.ExtractToDirectory(Path.Combine(tempDir.Path, "cement.zip"), Path.Combine(tempDir.Path, "cement"));
                CopyNewCmExe(tempDir.Path);
            }

            var okMessage = $"Update succeeded: {oldHash} -> {newHash} ({updater.Name})";
            ConsoleWriter.Shared.WriteOk(okMessage);
            Log.LogDebug(okMessage);
            return true;
        }
        catch (WebException webException)
        {
            Log.LogError(webException, "Fail self-update, exception: '{ErrorMessage}'", webException.Message);

            if (webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
            {
                var resp = (HttpWebResponse)webException.Response;
                if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                {
                    ConsoleWriter.Shared.WriteWarning($"Failed to look for updates on branch {branch}. Server replied 404 ({updater.Name})");
                    return false;
                }
            }

            ConsoleWriter.Shared.WriteWarning($"Failed to look for updates on branch {branch}. {webException.Message} ({updater.Name})");
            return false;
        }
    }

    private void CopyNewCmExe(string from)
    {
        from = Path.Combine(from, "cement", "dotnet");
        if (!Directory.Exists(from))
        {
            ConsoleWriter.Shared.WriteError($"Someting bad with self-update: {from} not found.");
            Log.LogError("Someting bad with self-update.");
            return;
        }

        var dotnetInstallFolder = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet");
        if (!Directory.Exists(dotnetInstallFolder))
            Directory.CreateDirectory(dotnetInstallFolder);
        Log.LogDebug("dotnet install folder: " + dotnetInstallFolder);

        var currOsDir = GetCurrentOsDirectory();
        var tempPathToCementBinary = Path.Combine(from, currOsDir);

        var cm = Path.Combine(from, "cm.exe");
        var cmNew = Path.Combine(from, "cm_new.exe");

        if (Directory.Exists(tempPathToCementBinary))
        {
            var isOsWin = OperatingSystem.IsWindows();
            var newcm = Path.Combine(tempPathToCementBinary, $"cm{(isOsWin ? ".exe" : "")}");
            if (File.Exists(newcm))
            {
                File.Delete(cm);
                cm = newcm;
            }
        }

        Log.LogDebug($"cement exe in temp folder: {cm}");

        File.Copy(cm, cmNew, true);
        if (!IsInstallingCement && File.Exists(Path.Combine(dotnetInstallFolder, "cm.exe")))
            File.Delete(cm);

        var includeFiles = GetIncludeFilesForCopy(from, currOsDir);

        foreach (var file in includeFiles)
        {
            var to = file.Replace(from, dotnetInstallFolder).Replace($"{currOsDir}\\", "");
            var toDir = Path.GetDirectoryName(to);
            if (!Directory.Exists(toDir))
                Directory.CreateDirectory(toDir);
            File.Copy(file, to, true);
        }

        Log.LogDebug("copy files from temp folder to bin completed");
    }

    private void AddInstallToPath()
    {
        if (Platform.IsUnix())
            return;

        var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        var toAdd = Path.Combine(Helper.GetCementInstallDirectory());

        if (path == null)
        {
            Log.LogWarning("Path is null");
            path = "";
        }

        if (path.ToLower().Contains(toAdd.ToLower()))
            return;

        if (path.Length > 0)
            path = path + ";" + toAdd;
        else
            path = toAdd;

        Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
        ConsoleWriter.Shared.WriteOk(toAdd + " added to $PATH");
        ConsoleWriter.Shared.WriteOk("To finish installation, please restart your terminal process");
        Log.LogDebug(toAdd + " added to $PATH: " + path);
    }

    private void InstallPowerShell()
    {
        if (Platform.IsUnix() || IsInstallingCement)
            return;

        var directory = Path.Combine(Helper.HomeDirectory(), "Documents", "WindowsPowerShell");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        var profileFile = Path.Combine(directory, "NuGet_profile.ps1");
        if (!File.Exists(profileFile))
            File.Create(profileFile).Close();
        var importStr = "Import-Module cementPM 3>$null";
        if (!File.ReadAllLines(profileFile).Contains(importStr))
            File.AppendAllLines(profileFile, new[] {"", importStr});

        var moduleDirectory = Path.Combine(directory, "Modules", "cementPM");
        if (!Directory.Exists(moduleDirectory))
            Directory.CreateDirectory(moduleDirectory);

        var src = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "cementPM.psm1");
        if (!File.Exists(src))
        {
            ConsoleWriter.Shared.WriteWarning("cement powershell script not found at " + src);
            return;
        }

        File.Copy(src, Path.Combine(moduleDirectory, "cementPM.psm1"), true);
    }

    private void InstallClinkScript()
    {
        if (Platform.IsUnix() || IsInstallingCement)
            return;

        var directory = Path.Combine(Helper.HomeDirectory(), "AppData", "Local", "clink");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var src = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "cement_completion.lua");
        if (!File.Exists(src))
        {
            ConsoleWriter.Shared.WriteWarning("lua script not found at " + src);
            return;
        }

        File.Copy(src, Path.Combine(directory, "cement_completion.lua"), true);
    }

    private void InstallBashScript()
    {
        if (Platform.IsUnix())
            return;

        var file = Path.Combine(Helper.HomeDirectory(), ".profile");
        if (!File.Exists(file))
            File.Create(file).Close();

        var toAdd = ". ~/bin/dotnet/bash-completion";
        var lines = File.ReadAllLines(file);
        if (lines.Contains(toAdd))
            return;

        lines = lines.Where(l => !l.StartsWith(". ~/bin/") || !l.EndsWith("/bash-completion"))
            .Concat(new[] {toAdd})
            .ToArray();
        File.WriteAllLines(file, lines);
    }
}
