using System.Collections.Generic;
using Common;
using Common.DepsValidators;
using Common.Logging;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class UsagesCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly Dictionary<string, ICommand> commands;

    public UsagesCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, IUsagesProvider usagesProvider,
                         GetCommand getCommand, BuildDepsCommand buildDepsCommand, BuildCommand buildCommand,
                         CycleDetector cycleDetector, IDepsValidatorFactory depsValidatorFactory,
                         IGitRepositoryFactory gitRepositoryFactory)
    {
        this.consoleWriter = consoleWriter;

        commands = new Dictionary<string, ICommand>
        {
            {"show", new UsagesShowCommand(consoleWriter, featureFlags)},
            {
                "build", new UsagesBuildCommand(
                    consoleWriter, featureFlags, usagesProvider, getCommand, buildDepsCommand, buildCommand, gitRepositoryFactory)
            },
            {
                "grep", new UsagesGrepCommand(
                    consoleWriter, featureFlags, cycleDetector, depsValidatorFactory, gitRepositoryFactory,
                    new ShellRunner(LogManager.GetLogger<UsagesGrepCommand>()), usagesProvider)
            }
        };
    }

    public string HelpMessage => @"
    Performs operations with module usages

    usages show
        shows the modules linked to the given dependence

        Usage:
            cm usages show [-m=<module>] [-c=<configuration>] [-b=<branch>] [-a]
            -m/--module            - module name (current module name by default)
            -c/--configuration     - configuration name (* by default)
            -b/--branch            - branch name (* by default)
            -a/--all               - show every branch of each parent
            -e/--edges             - prints graph in proper format
                                     for graph visualizers(i.e. arborjs.org/halfviz/)

        Example:
            cm usages show -m=logging
                show the modules which linked to the logging/full-build master

    usages build
        tries get and build all modules (in masters) linked to the current

        Usage:
            cm usages build [-b=<branch>] [-p]
            -b/--branch            - checking parents which use this branch (current by default)
            -p/--pause             - pause on errors

    usages grep
        search for given pattern in modules (in masters)
        linked to the current (<branch>, master by default)

        Usage:
            cm usages grep [-b=<branch>] [-i/--ignore-case] [-s/--skip-get] <patterns>
                [-f <patternFile>] [-- <fileMask>]
            -i/--ignore-case
            -s/--skip-get           - skip cloning modules
            -f <patternFile>        - search for patterns from file (line delimited)
            <patterns>              - patterns for search
            <fileMasks>             - limit the search to paths matching at least one pattern
            patterns combined with --or by default, can be combined with --and (<p1> --and <p2>)
            for other options see help for `git grep` command

        Example:
            cm usages grep ""new Class"" ""Class.New"" -- *.cs
                show lines contains ""new Class"" or ""Class.New"" in modules linked to the current, only in *.cs files
";

    public string Name => "usages";
    public bool IsHiddenCommand => CementSettingsRepository.Get().CementServer == null;

    public int Run(string[] args)
    {
        if (args.Length < 2 || !commands.ContainsKey(args[1]))
        {
            consoleWriter.WriteError("Bad arguments");
            return -1;
        }

        return commands[args[1]].Run(args);
    }
}
