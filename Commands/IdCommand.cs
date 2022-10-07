﻿using System;
using System.IO;
using Commands.Attributes;
using Common;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
[HiddenCommand]
public sealed class IdCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IGitRepositoryFactory gitRepositoryFactory;

    public IdCommand(ConsoleWriter consoleWriter, IGitRepositoryFactory gitRepositoryFactory)
    {
        this.consoleWriter = consoleWriter;
        this.gitRepositoryFactory = gitRepositoryFactory;
    }

    public string HelpMessage => @"
    Prints id of current module or ids of modules

    Usage:
        cm id
";

    public string Name => "id";

    public int Run(string[] args)
    {
        var cwd = Directory.GetCurrentDirectory();
        if (Helper.IsCementTrackedDirectory(cwd))
        {
            PrintHashes(Directory.GetDirectories(cwd));
            return 0;
        }

        if (Helper.IsCurrentDirectoryModule(cwd))
        {
            PrintHashes(new[] {cwd});
            return 0;
        }

        consoleWriter.WriteError("Failed to get info in %s\nNot a module or module's parent folder");
        return -1;
    }

    private void PrintHashes(string[] modules)
    {
        foreach (var module in modules)
        {
            try
            {
                var moduleName = Path.GetFileName(module);
                var workspace = Directory.GetParent(module).FullName;
                var repo = gitRepositoryFactory.Create(moduleName, workspace);
                if (repo.IsGitRepo)
                {
                    var hash = repo.CurrentLocalCommitHash();
                    consoleWriter.WriteLine(moduleName + " " + hash);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
