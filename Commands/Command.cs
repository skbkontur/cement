using System;
using System.Diagnostics;
using System.IO;
using Common;
using log4net;

namespace Commands
{
    public abstract class Command : ICommand
    {
        protected static ILog Log = LogManager.GetLogger(typeof(Command));
        private readonly CommandSettings commandSettings;

        protected Command(CommandSettings settings)
        {
            commandSettings = settings;
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

                if (!commandSettings.NoElkLog)
                    LogHelper.SendSavedLog();

                if (commandSettings.MeasureElapsedTime)
                {
                    ConsoleWriter.WriteInfo("Total time: " + sw.Elapsed);
                    Log.Debug("Total time: " + sw.Elapsed);
                }
                return exitCode;
            }
            catch (GitLocalChangesException e)
            {
                Log?.Warn("Failed to " + GetType().Name.ToLower(), e);
                ConsoleWriter.WriteError(e.Message);
                return -1;
            }
            catch (CementException e)
            {
                Log?.Error("Failed to " + GetType().Name.ToLower(), e);
                ConsoleWriter.WriteError(e.Message);
                return -1;
            }
            catch (Exception e)
            {
                Log?.Error("Failed to " + GetType().Name.ToLower(), e);
                ConsoleWriter.WriteError(e.Message);
                ConsoleWriter.WriteError(e.StackTrace);
                return -1;
            }
        }

        private void CheckRequireYaml()
        {
            if (commandSettings.Location == CommandSettings.CommandLocation.RootModuleDirectory &&
                commandSettings.RequireModuleYaml &&
                !File.Exists(Helper.YamlSpecFile))
                throw new CementException("This command require module.yaml file.\nUse convert-spec for convert old spec to module.yaml.");
        }

        private void SetWorkspace()
        {
            var cwd = Directory.GetCurrentDirectory();
            if (commandSettings.Location == CommandSettings.CommandLocation.WorkspaceDirectory)
            {
                if (!Helper.IsCementTrackedDirectory(cwd))
                    throw new CementTrackException(cwd + " is not cement workspace directory.");
                Helper.SetWorkspace(cwd);
            }
            if (commandSettings.Location == CommandSettings.CommandLocation.RootModuleDirectory)
            {
                if (!Helper.IsCurrentDirectoryModule(cwd))
                    throw new CementTrackException(cwd + " is not cement module directory.");
                Helper.SetWorkspace(Directory.GetParent(cwd).FullName);
            }
            if (commandSettings.Location == CommandSettings.CommandLocation.InsideModuleDirectory)
            {
                var currentModuleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
                if (currentModuleDirectory == null)
                    throw new CementTrackException("Can't locate module directory");
                Helper.SetWorkspace(Directory.GetParent(currentModuleDirectory).FullName);
            }
        }

        private void InitLogging()
        {
            Log = new PrefixAppender(commandSettings.LogPerfix, LogManager.GetLogger(GetType().Name));
            if (commandSettings.LogFileName != null)
                LogHelper.InitializeFileAndElkLogging(commandSettings.LogFileName);
            else if (!commandSettings.NoElkLog)
                LogHelper.InitializeElkOnlyLogging();

            try
            {
                Log.Info("Cement version: " + Helper.GetAssemblyTitle());
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LogAndParseArgs(string[] args)
        {
            Log.Debug($"Parsing args: [{string.Join(" ", args)}] in {Directory.GetCurrentDirectory()}");
            ParseArgs(args);
            Log.Debug("OK parsing args");
        }

        protected abstract int Execute();
        protected abstract void ParseArgs(string[] args);
        public abstract string HelpMessage { get; }

        public bool IsHiddenCommand => commandSettings.IsHiddenCommand;
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
