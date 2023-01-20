#nullable enable
using System;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class AddPackageCommand : Command<AddPackageCommandOptions>
{
    public AddPackageCommand(ConsoleWriter consoleWriter)
        : base(consoleWriter)
    {
    }

    public override string Name => "add";
    public override string HelpMessage => @"usage: cm packages add <name> <url>";

    protected override int Execute(AddPackageCommandOptions options)
    {
        var settings = CementSettingsRepository.Get();
        var packageName = options.PackageName;
        var packageUrl = options.PackageUrl;

        var package = settings.Packages.Find(p => p.Name.Equals(packageName, StringComparison.Ordinal));
        if (package != null)
        {
            if (package.Url.Equals(packageUrl))
                return 0;

            throw new CementException($"error: conflict: package '{packageName}' already exists");
        }

        package = new Package(packageName, packageUrl);
        settings.Packages.Add(package);

        CementSettingsRepository.Save(settings);
        return 0;
    }

    protected override AddPackageCommandOptions ParseArgs(string[] args)
    {
        if (args.Length < 2)
            throw new BadArgumentException($"error: invalid arguments{Environment.NewLine}{HelpMessage}");

        var packageName = args[0];
        var packageUrl = args[1];

        return new AddPackageCommandOptions(packageName, packageUrl);
    }
}
