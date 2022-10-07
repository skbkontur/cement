using System;
using System.Diagnostics;
using System.IO;
using Common;
using Common.Exceptions;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Commands;

public abstract class Command<TCommandOptions> : ICommand
    where TCommandOptions : notnull
{
    protected Command(ConsoleWriter consoleWriter, CommandSettings settings, FeatureFlags featureFlags)
    {
        ConsoleWriter = consoleWriter;
        CommandSettings = settings;
        FeatureFlags = featureFlags;
    }

    public abstract string Name { get; }
    public abstract string HelpMessage { get; }

    public int Run(string[] args)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            SetWorkspace();
            CheckRequireYaml();
            InitLogging();
            var options = LogAndParseArgs(args);

            var exitCode = Execute(options);

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
            Log?.LogWarning(e, "Failed to " + GetType().Name.ToLower());
            ConsoleWriter.WriteError(e.Message);
            return -1;
        }
        catch (CementException e)
        {
            Log?.LogError(e, "Failed to " + GetType().Name.ToLower());
            ConsoleWriter.WriteError(e.Message);
            return -1;
        }
        catch (Exception e)
        {
            Log?.LogError(e, "Failed to " + GetType().Name.ToLower());
            ConsoleWriter.WriteError(e.Message);
            ConsoleWriter.WriteError(e.StackTrace);
            return -1;
        }
    }

    protected ILogger Log { get; private set; }

    protected ConsoleWriter ConsoleWriter { get; }
    protected CommandSettings CommandSettings { get; }
    protected FeatureFlags FeatureFlags { get; }

    protected abstract int Execute(TCommandOptions commandOptions);
    protected abstract TCommandOptions ParseArgs(string[] args);

    private void CheckRequireYaml()
    {
        if (CommandSettings.Location == CommandLocation.RootModuleDirectory && CommandSettings.RequireModuleYaml &&
            !File.Exists(Helper.YamlSpecFile))
        {
            throw new CementException("No " + Helper.YamlSpecFile + " file found");
        }
    }

    private void SetWorkspace()
    {
        var cwd = Directory.GetCurrentDirectory();
        if (CommandSettings.Location == CommandLocation.WorkspaceDirectory)
        {
            if (!Helper.IsCementTrackedDirectory(cwd))
                throw new CementTrackException(cwd + " is not cement workspace directory.");
            Helper.SetWorkspace(cwd);
        }

        if (CommandSettings.Location == CommandLocation.RootModuleDirectory)
        {
            if (!Helper.IsCurrentDirectoryModule(cwd))
                throw new CementTrackException(cwd + " is not cement module directory.");
            Helper.SetWorkspace(Directory.GetParent(cwd).FullName);
        }

        if (CommandSettings.Location == CommandLocation.InsideModuleDirectory)
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
            Log.LogInformation("Cement version: {CementVersion}", Helper.GetAssemblyTitle());
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private TCommandOptions LogAndParseArgs(string[] args)
    {
        Log.LogDebug("Parsing args: [{Args}] in {WorkingDirectory}", string.Join(" ", args), Directory.GetCurrentDirectory());

        var options = ParseArgs(args);

        Log.LogDebug("OK parsing args");
        return options;
    }
}
