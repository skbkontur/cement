using System;
using System.Diagnostics;
using System.IO;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using Cement.Cli.Common.Logging;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

public abstract class Command<TCommandOptions> : ICommand
    where TCommandOptions : notnull
{
    private ILogger logger;
    private ConsoleWriter consoleWriter;

    protected Command(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
    {
        this.consoleWriter = consoleWriter;
        FeatureFlags = featureFlags;

        // backward compatibility
        logger = LogManager.GetLogger(GetType());
    }

    public abstract string Name { get; }
    public abstract string HelpMessage { get; }

    public virtual bool MeasureElapsedTime { get; set; }
    public virtual bool RequireModuleYaml { get; set; }
    public virtual CommandLocation Location { get; set; } = CommandLocation.Any;

    public int Run(string[] args)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            SetWorkspace(Location);
            CheckRequireYaml(Location, RequireModuleYaml);

            var options = LogAndParseArgs(args);

            var exitCode = Execute(options);

            if (MeasureElapsedTime)
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

    protected FeatureFlags FeatureFlags { get; }

    protected abstract int Execute(TCommandOptions commandOptions);
    protected abstract TCommandOptions ParseArgs(string[] args);

    private static void CheckRequireYaml(CommandLocation location, bool requireModuleYaml)
    {
        if (location == CommandLocation.RootModuleDirectory && requireModuleYaml &&
            !File.Exists(Helper.YamlSpecFile))
        {
            throw new CementException("No " + Helper.YamlSpecFile + " file found");
        }
    }

    private static void SetWorkspace(CommandLocation location)
    {
        var cwd = Directory.GetCurrentDirectory();
        if (location == CommandLocation.WorkspaceDirectory)
        {
            if (!Helper.IsCementTrackedDirectory(cwd))
                throw new CementTrackException(cwd + " is not cement workspace directory.");
            Helper.SetWorkspace(cwd);
        }

        if (location == CommandLocation.RootModuleDirectory)
        {
            if (!Helper.IsCurrentDirectoryModule(cwd))
                throw new CementTrackException(cwd + " is not cement module directory.");
            Helper.SetWorkspace(Directory.GetParent(cwd).FullName);
        }

        if (location == CommandLocation.InsideModuleDirectory)
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
