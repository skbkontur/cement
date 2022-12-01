using System.Diagnostics;
using System.Linq;
using Cement.Cli.Commands.Attributes;
using Cement.Cli.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
[HiddenCommand]
public sealed class UserCommand : Command<UserCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        Location = CommandLocation.Any
    };
    private readonly ILogger<UserCommand> logger;
    private readonly ConsoleWriter consoleWriter;

    public UserCommand(ILogger<UserCommand> logger, ConsoleWriter consoleWriter, FeatureFlags featureFlags)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
    }

    public override string Name => "";
    public override string HelpMessage => @"";

    protected override int Execute(UserCommandOptions options)
    {
        var arguments = options.Arguments;
        var cmd = CementSettingsRepository.Get().UserCommands[arguments[0]];
        logger.LogDebug("Run command {CommandName}: '{Cmd}'", arguments[0], cmd);
        if (arguments.Length > 1)
        {
            arguments = arguments.Skip(1).ToArray();
            cmd = string.Format(cmd, arguments);
        }

        return Run(cmd);
    }

    protected override UserCommandOptions ParseArgs(string[] args)
    {
        return new UserCommandOptions(args);
    }

    private int Run(string cmd)
    {
        consoleWriter.WriteInfo($"Running command '{cmd}'");

        var startInfo = new ProcessStartInfo
        {
            FileName = Platform.IsUnix() ? "/bin/bash" : "cmd",
            Arguments = Platform.IsUnix() ? " -lc " : " /c ",
            UseShellExecute = false
        };
        startInfo.Arguments = startInfo.Arguments + "\"" + cmd + "\"";

        var process = Process.Start(startInfo);
        process.WaitForExit();
        return process.ExitCode;
    }
}
