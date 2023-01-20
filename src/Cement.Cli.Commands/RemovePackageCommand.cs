#nullable enable
using System;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class RemovePackageCommand : Command<RemovePackageCommandOptions>
{
    public RemovePackageCommand(ConsoleWriter consoleWriter)
    {
    }

    public override string Name => "remove";
    public override string HelpMessage => @"usage: cm packages remove <name>";

    protected override int Execute(RemovePackageCommandOptions options)
    {
        var settings = CementSettingsRepository.Get();

        var package = settings.Packages.Find(p => p.Name.Equals(options.PackageName, StringComparison.Ordinal));
        if (package == null)
            return 0;

        settings.Packages.Remove(package);

        CementSettingsRepository.Save(settings);
        return 0;
    }

    protected override RemovePackageCommandOptions ParseArgs(string[] args)
    {
        if (args.Length < 1)
            throw new BadArgumentException($"error: invalid arguments{Environment.NewLine}{HelpMessage}");

        var packageName = args[0];
        return new RemovePackageCommandOptions(packageName);
    }
}
