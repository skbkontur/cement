using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using Common;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Extensions.Logging;
using NuGet.ProjectModel;

namespace Commands
{
    public class SelfUpdate : Command
    {
        private string branch;
        private static bool isAutoUpdate;
        protected bool IsInstallingCement;

        public SelfUpdate()
            : base(new CommandSettings
            {
                LogPerfix = "SELF-UPDATE",
                LogFileName = null,
                MeasureElapsedTime = false,
                Location = CommandSettings.CommandLocation.Any
            })
        {
        }

        public static void UpdateIfOld()
        {
            try
            {
                var lastUpdate = Helper.GetLastUpdateTime();
                var now = DateTime.Now;
                var diff = now - lastUpdate;
                if (diff <= TimeSpan.FromMinutes(15))
                    //if (diff <= TimeSpan.FromHours(5))
                    return;
                isAutoUpdate = true;
                var exitCode = new SelfUpdate().Run(new[] {"self-update"});
                if (exitCode != 0)
                {
                    Log.LogError("Auto update cement failed. 'self-update' exited with code '{Code}'", exitCode);
                    ConsoleWriter.WriteWarning("Auto update failed. Check previous warnings for details");
                }
            }
            catch (Exception exception)
            {
                Log.LogError("Auto update failed, error: '{Exception}'", exception);
                ConsoleWriter.WriteWarning("Auto update failed. Check logs for details");
            }
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseSelfUpdate(args);
            branch = (string) parsedArgs["branch"];
            SearchAndSaveBranchInSettings(ref branch);
            if (isAutoUpdate)
                Log.LogDebug("Auto updating cement");
            IsInstallingCement |= !HasInstalledCement();
            if (IsInstallingCement)
            {
                ConsoleWriter.WriteInfo("Installing cement");
                Log.LogDebug("Installing cement");
            }
        }

        protected override int Execute()
        {
            ConsoleWriter.WriteProgressWithoutSave("self-update");
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
                Log.LogError("Fail to install cement", exception);
                ConsoleWriter.WriteError("Fail to install cement: " + exception);
            }

            var server = CementSettings.Get().CementServer;
            Log.LogInformation($"Cement server: {server}");
            //добавить реализацию для создания класса тестового обновления из локальной папки
            // var updater = server == null
            //     ? (ICementUpdater) new CementFromGitHubUpdater(Log)
            //     : (ICementUpdater) new CementFromServerUpdater(server, branch, Log);
            var updater = new CementFromLocalPathUpdater(Log);

            return UpdateBinary(updater);
        }

        private static void CreateRunners()
        {
            const string cmdText = @"@echo off
""%~dp0\dotnet\cm.exe"" %*
SET exit_code=%errorlevel%
if exist %~dp0\dotnet\cm_new.exe (
	copy %~dp0\dotnet\cm_new.exe %~dp0\dotnet\cm.exe /Y > nul
	del %~dp0\dotnet\cm_new.exe > nul
)
cmd /C exit %exit_code% > nul";

            var bashTextUnix = @"#!/bin/bash
path=""`dirname \""$0\""`/dotnet/cm.exe""
cmd=""$path""
for word in ""$@""; do cmd=""$cmd \""$word\""""; done
eval $cmd
exit_code=$?
if [ -f ~/bin/dotnet/cm_new.exe ];
then
	cp ~/bin/dotnet/cm_new.exe ~/bin/dotnet/cm.exe
	rm ~/bin/dotnet/cm_new.exe
	chmod u+x ~/bin/dotnet/cm.exe
fi
exit $exit_code";
            bashTextUnix = bashTextUnix.Replace("\r\n", "\n");

            const string bashText = @"#!/bin/bash
path=""`dirname \""$0\""`/dotnet/cm.exe""
args=$@
$path ""$@""
exit_code=$?
if [ -f ~/bin/dotnet/cm_new.exe ];
then
	cp ~/bin/dotnet/cm_new.exe ~/bin/dotnet/cm.exe
	rm ~/bin/dotnet/cm_new.exe
	chmod u+x ~/bin/dotnet/cm.exe
fi
exit $exit_code";

            var installDirectory = Helper.GetCementInstallDirectory();
            Helper.CreateFileAndDirectory(Path.Combine(installDirectory, "cm.cmd"), cmdText);
            Helper.CreateFileAndDirectory(Path.Combine(installDirectory, "cm"), Helper.OsIsUnix() ? bashTextUnix : bashText);
            Log.LogDebug("Successfully created cm.cmd & cm.");
        }

        private int UpdateBinary(ICementUpdater updater)
        {
            var currentCommitHash = Helper.GetCurrentBuildCommitHash();

            ConsoleWriter.WriteProgressWithoutSave("Looking for cement updates");
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
                ConsoleWriter.WriteInfo($"No cement binary updates available ({updater.GetName()})");
                Log.LogDebug("Already has {0} version", currentCommitHash);
            }
            return 0;
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

        private bool UpdateBinaries(ICementUpdater updater, string oldHash, string newHash)
        {
            ConsoleWriter.WriteProgressWithoutSave("Updating cement binaries");

            try
            {
                var zipContent = updater.GetNewCementZip();
                using (var tempDir = new TempDirectory())
                {
                    File.WriteAllBytes(Path.Combine(tempDir.Path, "cement.zip"), zipContent);
                    ZipFile.ExtractToDirectory(Path.Combine(tempDir.Path, "cement.zip"), Path.Combine(tempDir.Path, "cement"));
                    CopyNewCmExe(tempDir.Path);
                }

                var okMessage = $"Successfully updated cement binaries. {oldHash} -> {newHash} ({updater.GetName()})";
                ConsoleWriter.WriteOk(okMessage);
                Log.LogDebug(okMessage);
                return true;
            }
            catch (WebException webException)
            {
                Log.LogError("Fail self-update, exception: '{Exception}'", webException);

                if (webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
                {
                    var resp = (HttpWebResponse) webException.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                    {
                        ConsoleWriter.WriteWarning($"Failed to look for updates on branch {branch}. Server replied 404 ({updater.GetName()})");
                        return false;
                    }
                }

                ConsoleWriter.WriteWarning($"Failed to look for updates on branch {branch}. {webException.Message} ({updater.GetName()})");
                return false;
            }
        }

        private static void SearchAndSaveBranchInSettings(ref string branch)
        {
            var settings = CementSettings.Get();
            if (branch != null)
                settings.SelfUpdateTreeish = branch;
            else
                branch = settings.SelfUpdateTreeish;
            settings.Save();
        }

        private void CopyNewCmExe(string from)
        {
            string fromOs = Path.Combine(from, "cement", "dotnet", Helper.GetOsPublishPath());
            from = Path.Combine(from, "cement", "dotnet");
            if (!Directory.Exists(from))
            {
                ConsoleWriter.WriteError($"Someting bad with self-update: {from} not found.");
                Log.LogError("Someting bad with self-update.");
                return;
            }

            var dotnetInstallFolder = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet");
            if (!Directory.Exists(dotnetInstallFolder))
                Directory.CreateDirectory(dotnetInstallFolder);
            Log.LogDebug("dotnet install folder: " + dotnetInstallFolder);

            var cm = Path.Combine(fromOs, "cm.exe");
            var cmNew = Path.Combine(fromOs, "cm_new.exe");
            File.Copy(cm, cmNew, true);
            if (!IsInstallingCement && File.Exists(Path.Combine(dotnetInstallFolder, "cm.exe")))
                File.Delete(cm);

            var files = Directory.GetFiles(from, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var to = file.Replace(from, dotnetInstallFolder);
                var toDir = Path.GetDirectoryName(to);
                if (!Directory.Exists(toDir))
                    Directory.CreateDirectory(toDir);
                File.Copy(file, to, true);
            }
            
            var filesFromOs = Directory.GetFiles(fromOs, "*", SearchOption.AllDirectories);
            foreach (var file in filesFromOs)
            {
                var to = file.Replace(fromOs, dotnetInstallFolder);
                var toDir = Path.GetDirectoryName(to);
                if (!Directory.Exists(toDir))
                    Directory.CreateDirectory(toDir);
                File.Copy(file, to, true);
            }
        }

        private void AddInstallToPath()
        {
            if (Helper.OsIsUnix())
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
            ConsoleWriter.WriteOk(toAdd + " added to $PATH");
            ConsoleWriter.WriteOk("To finish installation, please restart your terminal process");
            Log.LogDebug(toAdd + " added to $PATH: " + path);
        }

        private void InstallPowerShell()
        {
            if (Helper.OsIsUnix() || IsInstallingCement)
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
                ConsoleWriter.WriteWarning("cement powershell script not found at " + src);
                return;
            }
            File.Copy(src, Path.Combine(moduleDirectory, "cementPM.psm1"), true);
        }

        private void InstallClinkScript()
        {
            if (Helper.OsIsUnix() || IsInstallingCement)
                return;

            var directory = Path.Combine(Helper.HomeDirectory(), "AppData", "Local", "clink");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var src = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "cement_completion.lua");
            if (!File.Exists(src))
            {
                ConsoleWriter.WriteWarning("lua script not found at " + src);
                return;
            }
            File.Copy(src, Path.Combine(directory, "cement_completion.lua"), true);
        }

        private void InstallBashScript()
        {
            if (Helper.OsIsUnix())
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

        public override string HelpMessage => @"
    Updates cement itself (automatically updated every 5 hours)

    Usage:
        cm self-update
";
    }
}