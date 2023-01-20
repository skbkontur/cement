using System.Collections.Generic;
using System.Linq;
using Cement.Cli.Commands.Attributes;
using Cement.Cli.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
[HiddenCommand]
public sealed class CompleteCommand : Command<CompleteCommandOptions>
{
    private readonly ILogger logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly CompleteCommandAutomata completeCommandAutomata;

    public CompleteCommand(ILogger<CompleteCommand> logger, ConsoleWriter consoleWriter,
                           CompleteCommandAutomata completeCommandAutomata)
        : base(consoleWriter)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.completeCommandAutomata = completeCommandAutomata;
    }

    public override string Name => "complete";
    public override string HelpMessage => "";

    protected override int Execute(CompleteCommandOptions options)
    {
        var otherArgs = options.OtherArgs;
        var buffer = otherArgs.Length == 0
            ? ""
            : otherArgs[0];

        if (otherArgs.Length > 1)
        {
            int pos;
            if (int.TryParse(otherArgs[1], out pos) && buffer.Length > pos)
                buffer = buffer.Substring(0, pos);
        }

        logger.LogDebug("[COMPLETE] '{CompleteBuffer}'", buffer);
        var result = completeCommandAutomata.Complete(buffer);
        PrintList(result);

        return 0;
    }

    protected override CompleteCommandOptions ParseArgs(string[] args)
    {
        var otherArgs = args.Skip(1).ToArray();
        return new CompleteCommandOptions(otherArgs);
    }

    private void PrintList(IEnumerable<string> list)
    {
        consoleWriter.WriteLines(list.OrderBy(x => x));
    }
}
