using System.IO;
using Cement.Cli.Common.Logging;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

public abstract class Command<TCommandOptions> : ICommand
    where TCommandOptions : notnull
{
    private ILogger logger;

    protected Command()
    {
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
        var options = LogAndParseArgs(args);
        return Execute(options);
    }

    protected abstract int Execute(TCommandOptions commandOptions);
    protected abstract TCommandOptions ParseArgs(string[] args);

    private TCommandOptions LogAndParseArgs(string[] args)
    {
        logger.LogDebug("Parsing args: [{Args}] in {WorkingDirectory}", string.Join(" ", args), Directory.GetCurrentDirectory());

        var options = ParseArgs(args);

        logger.LogDebug("OK parsing args");
        return options;
    }
}
