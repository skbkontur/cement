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
    private ILogger logger;
    private ConsoleWriter consoleWriter;

    protected Command(ConsoleWriter consoleWriter, CommandSettings settings, FeatureFlags featureFlags)
    {
        this.consoleWriter = consoleWriter;
        CommandSettings = settings;
        FeatureFlags = featureFlags;

        // backward compatibility
        logger = LogManager.GetLogger(GetType());
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

            var options = LogAndParseArgs(args);

            var exitCode = Execute(options);

            if (CommandSettings.MeasureElapsedTime)
            {
                consoleWriter.WriteInfo("Total time: " + sw.Elapsed);
                logger.LogDebug("Total time: " + sw.Elapsed);
            }

            return exitCode;
        }
        catch (GitLocalChangesException e)
        {
            logger?.LogWarning(e, "Failed to " + GetType().Name.ToLower());
            consoleWriter.WriteError(e.Message);
            return -1;
        }
        catch (CementException e)
        {
            logger?.LogError(e, "Failed to " + GetType().Name.ToLower());
            consoleWriter.WriteError(e.Message);
            return -1;
        }
        catch (Exception exception)
        {
            logger?.LogError(exception, "Failed to " + GetType().Name.ToLower());
            consoleWriter.WriteError(exception.ToString());
            return -1;
        }
    }

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

    private TCommandOptions LogAndParseArgs(string[] args)
    {
        logger.LogDebug("Parsing args: [{Args}] in {WorkingDirectory}", string.Join(" ", args), Directory.GetCurrentDirectory());

        var options = ParseArgs(args);

        logger.LogDebug("OK parsing args");
        return options;
    }
}
