using System;
using System.Diagnostics;
using System.IO;
using Common;
using Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public abstract class Command : ICommand
    {
        protected static ILogger Log;
        protected readonly CommandSettings CommandSettings;
        protected readonly FeatureFlags FeatureFlags;

        protected Command(CommandSettings settings)
        {
            CommandSettings = settings;

            var featureFlagsConfigPath = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "featureFlags.json");
            if (!File.Exists(featureFlagsConfigPath))
                throw new FeatureFlagsConfigNotFound(featureFlagsConfigPath);

            var configuration = new ConfigurationBuilder().AddJsonFile(featureFlagsConfigPath).Build();
            FeatureFlags = configuration.Get<FeatureFlags>();
        }

        public int Run(string[] args)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                SetWorkspace();
                CheckRequireYaml();
                InitLogging();
                LogAndParseArgs(args);

                var exitCode = Execute();

                if (!CommandSettings.NoElkLog)
                    LogHelper.SendSavedLog();

                if (CommandSettings.MeasureElapsedTime)
                {
                    ConsoleWriter.WriteInfo("Total time: " + sw.Elapsed);
                    Log.LogDebug("Total time: " + sw.Elapsed);
                }
                return exitCode;
            }
            catch (GitLocalChangesException e)
            {
                Log?.LogWarning("Failed to " + GetType().Name.ToLower(), e);
                ConsoleWriter.WriteError(e.Message);
                return -1;
            }
            catch (CementException e)
            {
                Log?.LogError("Failed to " + GetType().Name.ToLower(), e);
                ConsoleWriter.WriteError(e.Message);
                return -1;
            }
            catch (Exception e)
            {
                Log?.LogError("Failed to " + GetType().Name.ToLower(), e);
                ConsoleWriter.WriteError(e.Message);
                ConsoleWriter.WriteError(e.StackTrace);
                return -1;
            }
        }

        private void CheckRequireYaml()
        {
            if (CommandSettings.Location == CommandSettings.CommandLocation.RootModuleDirectory &&
                CommandSettings.RequireModuleYaml &&
                !File.Exists(Helper.YamlSpecFile))
                throw new CementException("This command require module.yaml file.\nUse convert-spec for convert old spec to module.yaml.");
        }

        private void SetWorkspace()
        {
            var cwd = Directory.GetCurrentDirectory();
            if (CommandSettings.Location == CommandSettings.CommandLocation.WorkspaceDirectory)
            {
                if (!Helper.IsCementTrackedDirectory(cwd))
                    throw new CementTrackException(cwd + " is not cement workspace directory.");
                Helper.SetWorkspace(cwd);
            }
            if (CommandSettings.Location == CommandSettings.CommandLocation.RootModuleDirectory)
            {
                if (!Helper.IsCurrentDirectoryModule(cwd))
                    throw new CementTrackException(cwd + " is not cement module directory.");
                Helper.SetWorkspace(Directory.GetParent(cwd).FullName);
            }
            if (CommandSettings.Location == CommandSettings.CommandLocation.InsideModuleDirectory)
            {
                var currentModuleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
                if (currentModuleDirectory == null)
                    throw new CementTrackException("Can't locate module directory");
                Helper.SetWorkspace(Directory.GetParent(currentModuleDirectory).FullName);
            }
        }

        private void InitLogging()
        {
            if (CommandSettings.LogFileName != null)
                LogHelper.InitializeFileAndElkLogging(CommandSettings.LogFileName, GetType().ToString());
            else if (!CommandSettings.NoElkLog)
                LogHelper.InitializeGlobalFileAndElkLogging(GetType().ToString());

            Log = LogManager.GetLogger(GetType());

            try
            {
                Log.LogInformation("Cement version: " + Helper.GetAssemblyTitle());
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LogAndParseArgs(string[] args)
        {
            Log.LogDebug($"Parsing args: [{string.Join(" ", args)}] in {Directory.GetCurrentDirectory()}");
            ParseArgs(args);
            Log.LogDebug("OK parsing args");
        }

        protected abstract int Execute();
        protected abstract void ParseArgs(string[] args);
        public abstract string HelpMessage { get; }

        public bool IsHiddenCommand => CommandSettings.IsHiddenCommand;
    }

    public class CommandSettings
    {
        public string LogPerfix;
        public string LogFileName;
        public bool MeasureElapsedTime;
        public bool RequireModuleYaml;
        public bool IsHiddenCommand = false;
        public bool NoElkLog = false;
        public CommandLocation Location;

        public enum CommandLocation
        {
            RootModuleDirectory,
            WorkspaceDirectory,
            Any,
            InsideModuleDirectory
        }
    }
}