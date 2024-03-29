using System;
using System.IO;
using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class StatusCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IGitRepositoryFactory gitRepositoryFactory;

    public StatusCommand(ConsoleWriter consoleWriter, IGitRepositoryFactory gitRepositoryFactory)
    {
        this.consoleWriter = consoleWriter;
        this.gitRepositoryFactory = gitRepositoryFactory;
    }

    public bool MeasureElapsedTime { get; }

    public bool RequireModuleYaml { get; }

    public CommandLocation Location { get; } = CommandLocation.Any;

    public string Name => "status";

    public string HelpMessage => @"
    Prints status of modifed git repos in the cement tracked dir
    It checks repo for push/pull and local state

    Usage:
        cm status

    Runs anywhere in the cement tracked tree
";

    public int Run(string[] args)
    {
        if (args.Length != 1)
        {
            consoleWriter.WriteError("Invalid command usage. User 'cm help init' for details");
            return -1;
        }

        var cwd = Directory.GetCurrentDirectory();
        cwd = Helper.GetWorkspaceDirectory(cwd);

        if (cwd == null)
        {
            consoleWriter.WriteError("Cement root was not found");
            return -1;
        }

        PrintStatus(cwd);
        return 0;
    }

    private void PrintStatus(string cwd)
    {
        var listDir = Directory.GetDirectories(cwd);
        var count = 0;
        foreach (var dir in listDir)
        {
            var repo = gitRepositoryFactory.Create(dir);
            PrintStatus(repo);
            consoleWriter.WriteProgress(++count + "/" + listDir.Length + " " + repo.ModuleName);
        }

        consoleWriter.ResetProgress();
    }

    private void PrintStatus(GitRepository repo)
    {
        try
        {
            if (!repo.HasLocalChanges() && repo.ShowUnpushedCommits().Length == 0)
                return;

            consoleWriter.WriteInfo(repo.ModuleName);
            if (repo.HasLocalChanges())
                consoleWriter.WriteLine(repo.ShowLocalChanges());
            if (repo.ShowUnpushedCommits().Length > 0)
                consoleWriter.WriteLine(repo.ShowUnpushedCommits());
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
