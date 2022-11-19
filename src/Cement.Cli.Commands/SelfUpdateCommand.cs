using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using Cement.Cli.Common.Updaters;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class SelfUpdateCommand : Command<SelfUpdateCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        Location = CommandLocation.Any
    };

    private readonly ILogger<SelfUpdateCommand> logger;
    private readonly ConsoleWriter consoleWriter;

    public SelfUpdateCommand(ILogger<SelfUpdateCommand> logger, ConsoleWriter consoleWriter, FeatureFlags featureFlags)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
    }

    public bool IsAutoUpdate { get; set; }
    public bool IsInstallingCement { get; set; }

    public override string Name => "self-update";
    public override string HelpMessage => @"
    Updates cement itself (automatically updated every 5 hours)
    Usage:
        cm self-update
";

    protected override SelfUpdateCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseSelfUpdate(args);
        var branch = (string)parsedArgs["branch"];
        var server = (string)parsedArgs["server"];

        return new SelfUpdateCommandOptions(branch, server);
    }

    protected override int Execute(SelfUpdateCommandOptions options)
    {
        var server = options.Server;
        var branch = options.Branch;

        SearchAndSaveBranchInSettings(ref server, ref branch);

        if (IsAutoUpdate)
            logger.LogDebug("Auto updating cement");

        IsInstallingCement |= !HasInstalledCement();
        if (IsInstallingCement)
        {
            consoleWriter.WriteInfo("Installing cement");
            logger.LogDebug("Installing cement");
        }

        consoleWriter.WriteProgressWithoutSave("self-update");
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
            logger.LogError(exception, "Fail to install cement: '{ErrorMessage}'", exception.Message);
            consoleWriter.WriteError("Fail to install cement: " + exception);
        }

        logger.LogInformation("Cement server: {CementServerUri}", server);

        using ICementUpdater updater = server == null
            ? new GitHubReleaseCementUpdater(logger, consoleWriter)
            : new ServerCementUpdater(logger, consoleWriter, server, branch);

        logger.LogInformation("Updater: {CementUpdaterName}", updater.Name);
        return UpdateBinary(updater, branch);
    }

    private void CreateRunners()
    {
        var installDirectory = Helper.GetCementInstallDirectory();

        if (OperatingSystem.IsWindows())
            Helper.CreateFileAndDirectory(Path.Combine(installDirectory, "cm.cmd"), GetWindowsScript());
        else
            Helper.CreateFileAndDirectory(Path.Combine(installDirectory, "cm"), GetUnixScript());

        logger.LogDebug("Successfully created cm.cmd & cm");
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

    private int UpdateBinary(ICementUpdater updater, string branch)
    {
        var currentCommitHash = Helper.GetCurrentBuildCommitHash();

        consoleWriter.WriteProgressWithoutSave("Looking for cement updates");
        var newCommitHash = updater.GetNewCommitHash();
        if (newCommitHash == null)
            return -1;

        if (IsInstallingCement)
            currentCommitHash = "(NOT INSTALLED)" + currentCommitHash;
        if (!HasAllCementFiles() || !currentCommitHash.Equals(newCommitHash))
        {
            if (!UpdateBinaries(updater, branch, currentCommitHash, newCommitHash))
                return -1;
        }
        else
        {
            consoleWriter.WriteInfo($"No cement binary updates available ({updater.Name})");
            logger.LogDebug("Already has {0} version", currentCommitHash);
        }

        return 0;
    }

    private bool UpdateBinaries(ICementUpdater updater, string branch, string oldHash, string newHash)
    {
        consoleWriter.WriteProgressWithoutSave("Updating cement binaries");

        try
        {
            var zipContent = updater.GetNewCementZip();

            if (zipContent is null)
            {
                consoleWriter.WriteWarning("Failed to receive cement binary");
                return false;
            }

            using (var tempDir = new TempDirectory())
            {
                File.WriteAllBytes(Path.Combine(tempDir.Path, "cement.zip"), zipContent);
                ZipFile.ExtractToDirectory(Path.Combine(tempDir.Path, "cement.zip"), Path.Combine(tempDir.Path, "cement"));
                CopyNewCmExe(tempDir.Path);
            }

            var okMessage = $"Update succeeded: {oldHash} -> {newHash} ({updater.Name})";
            consoleWriter.WriteOk(okMessage);
            logger.LogDebug(okMessage);
            return true;
        }
        catch (WebException webException)
        {
            logger.LogError(webException, "Fail self-update, exception: '{ErrorMessage}'", webException.Message);

            if (webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
            {
                var resp = (HttpWebResponse)webException.Response;
                if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                {
                    consoleWriter.WriteWarning($"Failed to look for updates on branch {branch}. Server replied 404 ({updater.Name})");
                    return false;
                }
            }

            consoleWriter.WriteWarning($"Failed to look for updates on branch {branch}. {webException.Message} ({updater.Name})");
            return false;
        }
    }

    private void CopyNewCmExe(string from)
    {
        from = Path.Combine(from, "cement", "dotnet");
        if (!Directory.Exists(from))
        {
            consoleWriter.WriteError($"Someting bad with self-update: {from} not found.");
            logger.LogError("Someting bad with self-update.");
            return;
        }

        var dotnetInstallFolder = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet");
        if (!Directory.Exists(dotnetInstallFolder))
            Directory.CreateDirectory(dotnetInstallFolder);
        logger.LogDebug("dotnet install folder: " + dotnetInstallFolder);

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

        logger.LogDebug($"cement exe in temp folder: {cm}");

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

        logger.LogDebug("copy files from temp folder to bin completed");
    }

    private void AddInstallToPath()
    {
        if (Platform.IsUnix())
            return;

        var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        var toAdd = Path.Combine(Helper.GetCementInstallDirectory());

        if (path == null)
        {
            logger.LogWarning("Path is null");
            path = "";
        }

        if (path.ToLower().Contains(toAdd.ToLower()))
            return;

        if (path.Length > 0)
            path = path + ";" + toAdd;
        else
            path = toAdd;

        Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
        consoleWriter.WriteOk(toAdd + " added to $PATH");
        consoleWriter.WriteOk("To finish installation, please restart your terminal process");
        logger.LogDebug(toAdd + " added to $PATH: " + path);
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
            consoleWriter.WriteWarning("cement powershell script not found at " + src);
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
            consoleWriter.WriteWarning("lua script not found at " + src);
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
