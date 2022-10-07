﻿using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Logging;

namespace Commands;

public sealed class CompleteCommand : Command
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "complete",
        MeasureElapsedTime = false,
        Location = CommandLocation.Any,
        IsHiddenCommand = true,
        NoElkLog = true
    };
    private readonly ConsoleWriter consoleWriter;
    private readonly CompleteCommandAutomata completeCommandAutomata;
    private string[] otherArgs;

    public CompleteCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, CompleteCommandAutomata completeCommandAutomata)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.completeCommandAutomata = completeCommandAutomata;
    }

    public override string Name => "complete";
    public override string HelpMessage => "";

    protected override int Execute()
    {
        var buffer = otherArgs.Length == 0
            ? ""
            : otherArgs[0];

        if (otherArgs.Length > 1)
        {
            int pos;
            if (int.TryParse(otherArgs[1], out pos) && buffer.Length > pos)
                buffer = buffer.Substring(0, pos);
        }

        LogHelper.SaveLog($"[COMPLETE] '{buffer}'");
        var result = completeCommandAutomata.Complete(buffer);
        PrintList(result);

        return 0;
    }

    protected override void ParseArgs(string[] args)
    {
        otherArgs = args.Skip(1).ToArray();
    }

    private void PrintList(IEnumerable<string> list)
    {
        consoleWriter.WriteLines(list.OrderBy(x => x));
    }
}
